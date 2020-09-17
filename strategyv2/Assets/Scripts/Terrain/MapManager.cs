using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum MapDisplays
{
    Tiles, TilesWithVision, Population, Supply, MovementSpeed, Simple, PlayerControlledAreas, HeightMap, HeightGradient
}

public class MapManager : MonoBehaviour
{
    private static MapManager _instance;
    public static MapManager Instance { get { return _instance; } }
    public TerrainMapTile BaseTerrainTile;

    public MapGenerator MapGen;
    // public TerrainMeshGenerator MeshGen;
    public LandMeshGenerator landMeshGen;

    [SerializeField]
    private List<MapTileSettings> MapTiles;

    [SerializeField]
    private MeshGeneratorArgs m_MeshArgs;

    public SquareArray<TerrainMapTile> map;

    private float _minHeight, _maxHeight;
    public int NumZLayers = 5;

    public MapDisplays CurrentlyDisplayingMapType;

    private Color FowGrey = new Color(.25f, .25f, .25f, 1);
    private Color NotDiscovered = new Color(0f, 0f, 0f, 1);
    private Color PlayerVision = new Color(1, 1, 1, 1);
    private Color OtherDivisionsVision = new Color(.5f, .5f, .5f, 1);

    [SerializeField]
    private float TileUpdateTickTime = 1;

    private Action OnMapRerender;

    private bool FinishedUpdatingTiles = false;

    public bool IsFinishedGeneratingMap { get; private set; }

    Dictionary<Terrain, TerrainMapTile> terrainTileLookup = new Dictionary<Terrain, TerrainMapTile>();
    Dictionary<Improvement, ImprovementMapTile> improvementTileLookup = new Dictionary<Improvement, ImprovementMapTile>();

    /// <summary>
    /// The higher this number the more fine the nave mesh compared to the terrain mesh
    /// </summary>
    int NavMeshScale = 1;

    public Action OnTerrrainGenerationFinished;

    private void Awake()
    {
        _instance = this;
        CurrentlyDisplayingMapType = MapDisplays.TilesWithVision;

        StartCoroutine(CreateTerrain());
    }

    IEnumerator CreateTerrain()
    {
        yield return new WaitForEndOfFrame();
        GenerateMap();
        IsFinishedGeneratingMap = true;

        //OnTerrrainGenerationFinished?.Invoke();
    }

    void Start()
    {
        OnTerrrainGenerationFinished += () =>
        {
            CreateGraph();
            StartCoroutine(UpdateTileValues());
            //InputController.Instance.RegisterOnClickCallBack(PrintTile);
            ButtonHandler Handler = new ButtonHandler(ButtonHandler.LeftClick, (x, y) => { },
                (handler, mousePos) =>
                {
                    PrintTile(mousePos);
                });

            InputController.Instance.RegisterHandler(Handler);

            LocalPlayerController.Instance.GeneralDivision.AttachedDivision.OnDiscoveredMapChanged += x => Rerender();
        };
    }

    private void Update()
    {
        if(CurrentlyDisplayingMapType == MapDisplays.TilesWithVision)
        {
            //show the players vision range
            //RenderMapWithTilesAndVision(LocalPlayerController.Instance.GeneralDivision, PlayerVision);
            //OnMapRerender?.Invoke();
        }
    }

    void OnDestroy()
    {
        //UpdateTileValuesThreads.ForEach(x => x.Abort());
    }

    public SquareArray<bool> GetMapMask()
    {
        return new SquareArray<bool>(map.SideLength, false);
    }

    public void RegisterOnMapRerenderCallback(Action callback)
    {
        OnMapRerender += callback;
    }

    private void PrintTile(Vector3 pos)
    {
        var tile = GetTileFromPosition(pos);
        Debug.Log(tile);
        //Debug.Log(tile.ToString());
    }

    #region updateTileValues
    private void UpdateTileValueThread(Vector2Int start, Vector2Int End)
    {
        for (int y = start.y; y < End.y; y++)
        {
            for (int x = start.x; x < End.x; x++)
            {
                map[x, y].Update(TileUpdateTickTime);
            }
        }
    }

    /// <summary>
    /// async update the tiles, will set FinishedUpdatingTiles when its done
    /// </summary>
    /// <param name="numThreads"></param>
    private void AsyncUpdateTileValues(int numThreads)
    {
        int size = (map.SideLength) / numThreads;

        Thread waitThread = new Thread(() => {
            // Use the thread pool to parrellize update
            using (CountdownEvent e = new CountdownEvent(1))
            {
                // TODO make these blocks instead of rows so that we get better lock perf
                for (int i = 0; i < numThreads; i++)
                {
                    Vector2Int start = new Vector2Int(0, i * size);
                    Vector2Int End = new Vector2Int(map.SideLength, ((i + 1) * size) - 1);
                    e.AddCount();
                    ThreadPool.QueueUserWorkItem(delegate (object state)
                    {
                        try
                        {
                            UpdateTileValueThread(start, End);
                        }
                        finally
                        {
                            e.Signal();
                        }
                    },
                    null);
                }

                e.Signal();
                e.Wait();

                FinishedUpdatingTiles = true;
            }
        });

        waitThread.Start();
    }
    
    /// <summary>
    /// start a new update cycle every second, also make sure that the previous one is finished before we start a new one
    /// </summary>
    /// <returns></returns>
    private IEnumerator UpdateTileValues()
    {
        float timeSinceLastTick = 0;
        float GameTime = 0;
        int numThreads = 4;
        while (true)
        {
            while (timeSinceLastTick < TileUpdateTickTime)
            {
                timeSinceLastTick += GameManager.DeltaTime;
                GameTime += GameManager.DeltaTime;
                yield return new WaitForEndOfFrame();
            }

            FinishedUpdatingTiles = false;
            AsyncUpdateTileValues(numThreads);

            while(!FinishedUpdatingTiles)
            {
                yield return new WaitForEndOfFrame();
            }

            timeSinceLastTick = 0;
        }
    }
    #endregion updateTileValues

    public void GenerateMap()
    {
        terrainTileLookup = new Dictionary<Terrain, TerrainMapTile>();
        improvementTileLookup = new Dictionary<Improvement, ImprovementMapTile>();

        ProfilingUtilities.LogAction(() =>
        {
            foreach (MapTileSettings tile in MapTiles)
            {
                if (tile.Layer == MapLayer.Terrain)
                {
                    if (!terrainTileLookup.ContainsKey(tile.TerrainMapTileSettings.TerrainType))
                    {
                        terrainTileLookup[tile.TerrainMapTileSettings.TerrainType] = tile.TerrainMapTileSettings;
                    }
                }
                else
                {
                    if (!improvementTileLookup.ContainsKey(tile.ImprovementMapTileSettings.Improvement))
                    {
                        improvementTileLookup[tile.ImprovementMapTileSettings.Improvement] = tile.ImprovementMapTileSettings;
                    }
                }
            }
        }, "MAP GEN: finished setup");

        ProfilingUtilities.LogAction(() =>
        {
            MapGen.GenerateMaps();
            ConvertMapGenerationToMapTiles(terrainTileLookup, improvementTileLookup);
            SetUpAjdacentTiles();
        }, "MAP GEN: done generating map tiles");

        ReconstructMesh();
    }

    public void ReconstructMesh()
    {
        ProfilingUtilities.LogAction(() =>
        {
            //MeshGen.ConstructLakeMeshes(MapGen.m_MapData, m_MeshArgs, MapGen.HeightMap, MapGen.WaterMap, MapGen.terrainMap, m_MeshArgs);

            // MeshGen.ConstructWaterPlaneMesh(MapGen.m_MapData, m_MeshArgs);
            // ProfilingUtilities.LogAction(() => MeshGen.ConstructMesh(MapGen.m_MapData, m_MeshArgs, terrainTileLookup), "mesh time");
            landMeshGen.ConstructMesh(MapGen.m_MapData, terrainTileLookup, improvementTileLookup);

            //MeshGen.ConstructRoadMeshes(MapGen.m_MapData);
            //MeshGen.ConstructGridMesh(MapGen.m_MapData);
        }, "MAP GEN: done constructing meshes");
    }

    private void SetUpAjdacentTiles()
    {
        List<TerrainMapTile> adjacents = new List<TerrainMapTile>();
        for (int y = 0; y < map.SideLength; y++)
        {
            for (int x = 0; x < map.SideLength; x++)
            {
                adjacents.Clear();
                for (int i = x - 1; i <= x + 1; i++)
                {
                    for (int j = y - 1; j <= y + 1; j++)
                    {
                        if (map.InBounds(i, j))
                        {
                            adjacents.Add(map[i, j]);
                        }
                    }
                }

                map[x, y].SetAdjacentTiles(adjacents);
            }
        }
    }

    #region position conversion and helpers

    public float getHeightAtWorldPosition(Vector3 position)
    {
        // return MeshGen.GetHeightAtWorldPosition(position);
        return landMeshGen.GetHeightAtWorldPosition(position);
    }

    public TerrainMapTile GetTileFromPosition(Vector3 position)
    {
        //var tilePos = MeshGen.ConvertWorldPositionToTilePosition(position, MapGen.m_MapData);
        var tilePos = landMeshGen.ConvertWorldPositionToTilePosition(position);
        return map[tilePos.x, tilePos.y];
    }

    public Vector2Int GetTilePositionFromPosition(Vector3 position)
    {
        var tilePos = landMeshGen.ConvertWorldPositionToTilePosition(position);
        return tilePos;
    }

    public Vector3 GetWorldPositionFromTilePosition(Vector2 position)
    {
        Vector3 tilePos = new Vector3(position.x, 0, position.y);
        Vector3 worldPos = landMeshGen.ConvertTilePositionToWorldPosition(tilePos, MapGen.m_MapData.TileMapSize);
        float height = landMeshGen.GetHeightAtWorldPosition(worldPos);
        worldPos.y = height;

        return worldPos;
    }

    public Vector2Int ClampTilePositionToInBounds(Vector2Int position)
    {
        int x = (int) Mathf.Clamp(position.x, 0, map.SideLength - 1);
        int y = (int) Mathf.Clamp(position.y, 0, map.SideLength - 1);
        return new Vector2Int(x, y);
    }

    public Vector3 ClampPositionToInBounds(Vector3 position)
    {
        var gridStart = Vector3.zero;
        float x = Mathf.Clamp(position.x, 0, landMeshGen.GetMeshSize().x);
        float z = Mathf.Clamp(position.z, 0, landMeshGen.GetMeshSize().z);
        return new Vector3(x, 0, z);
    }

    public int GetLayerIndexByHeight(float height)
    {
        int z = (int)(height * NumZLayers);
        //if the z is exactly numzlayers it will cause at out of bound on out bounds on the layers list
        if(z == NumZLayers)
        {
            z--;
        }
        return z;
    }
    #endregion

    /**
     * creates the navmesh for our grid
     */
    private void CreateGraph()
    {
        AstarData data = AstarPath.active.data;
        var graphs = data.graphs.ToList();
        graphs.ForEach(x => data.RemoveGraph(x));
        PointGraph graph = data.AddGraph(typeof(PointGraph)) as PointGraph;
        AstarPath.active.Scan(graph);

        // Make sure we only modify the graph when all pathfinding threads are paused
        AstarPath.active.AddWorkItem(new AstarWorkItem(ctx => {
            //first make node array
            int size = (map.SideLength) * NavMeshScale + 1;

            SquareArray<PointNode> nodeArray = new SquareArray<PointNode>(size);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector3 tilePos = GetWorldPositionFromTilePosition(new Vector2(x / (float)NavMeshScale, y / (float) NavMeshScale));
                    nodeArray[x,y] = graph.AddNode((Int3) tilePos);
                }
            }

            int connections = 0;

            //now connect nodes
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // loop over all adjacent nodes
                    for (int i = x-1; i <= x + 1; i++)
                    {
                        for (int j = y - 1; j <= y + 1; j++)
                        {
                            if (nodeArray.InBounds(i, j))
                            {
                                int scaledX = x / NavMeshScale;
                                int scaledY = y / NavMeshScale;

                                // if we are in the last edge grab
                                if(scaledX == map.SideLength)
                                {
                                    scaledX--;
                                }

                                if (scaledY == map.SideLength)
                                {
                                    scaledY--;
                                }

                                nodeArray[x, y].AddConnection(nodeArray[i, j], (uint)map[scaledX, scaledY].MoveCost);
                                connections++;
                            }
                        }
                    }
                }
            }
        }));

        // Run the above work item immediately
        AstarPath.active.FlushWorkItems();
    }

    private void ConvertMapGenerationToMapTiles(Dictionary<Terrain, TerrainMapTile> terrainTileLookup,
        Dictionary<Improvement, ImprovementMapTile> improvementTileLookup)
    {
        _minHeight = 100;
        _maxHeight = -100;
        map = new SquareArray<TerrainMapTile>(MapGen.terrainMap.SideLength);
        for (int i = 0; i < MapGen.terrainMap.SideLength; i++)
        {
            for (int j = 0; j < MapGen.terrainMap.SideLength; j++)
            {
                map[i, j] = new TerrainMapTile(terrainTileLookup[MapGen.terrainMap[i, j]], MapGen.HeightMap[i,j], MapGen.LayeredGradientMap[i, j]);
                map[i, j].Improvement = new ImprovementMapTile(improvementTileLookup[MapGen.improvmentMap[i, j]]);
                map[i, j].ModifyBaseWithImprovement();
                _minHeight = Mathf.Min(_minHeight, MapGen.HeightMap[i, j]);
                _maxHeight = Mathf.Max(_maxHeight, MapGen.HeightMap[i, j]);
            }
        }

        //Debug.Log($"min {_minHeight}, max {_maxHeight}");
    }

    public void Rerender()
    {
        RenderMap(CurrentlyDisplayingMapType);
    }

    public void RenderMap(MapDisplays mapDisplay)
    {
        DateTime start = DateTime.Now;
        CurrentlyDisplayingMapType = mapDisplay;
        //switch (mapDisplay)
        //{
        //    case MapDisplays.Population:
        //        //this.RenderMapWithKeyAndRange(x => x.Population, 1000);
        //        this.RenderMapWithKey(x => x.Population);
        //        break;
        //    case MapDisplays.Supply:
        //        //this.RenderMapWithKeyAndRange(x => x.Supply, 1000);
        //        this.RenderMapWithKey(x => x.Supply);
        //        break;
        //    case MapDisplays.HeightMap:
        //        this.RenderMapWithKey(x => x.Height);
        //        break;
        //    case MapDisplays.HeightGradient:
        //        this.RenderMapWithKey(x => x.HeightGradient.magnitude);
        //        break;
        //    case MapDisplays.Tiles:
        //        //this.RenderMapWithTiles();
        //        break;
        //    case MapDisplays.TilesWithVision:
        //        //RenderAllTilesGray();
        //        RenderDiscoveredTiles(LocalPlayerController.Instance.GeneralDivision.AttachedDivision);
        //        RenderMapWithTilesAndVision(LocalPlayerController.Instance.GeneralDivision, PlayerVision);
        //        break;
        //    case MapDisplays.MovementSpeed:
        //        this.RenderMapWithKey(x => 1 / (float)x.MoveCost);
        //        break;
        //    case MapDisplays.Simple:
        //        this.RenderSimple();
        //        break;
        //    case MapDisplays.PlayerControlledAreas:
        //        this.RenderMapWithZoneOfControl();
        //        break;
        //}

        DateTime end = DateTime.Now;
        Debug.Log($"render time {end - start}");

        OnMapRerender?.Invoke();
    }

    //public void RenderSimple()
    //{
    //    for (int x = 0; x < map.SideLength; x++) //Loop through the width of the map
    //    {
    //        for (int y = 0; y < map.SideLength; y++) //Loop through the height of the map
    //        {
    //            //Tilemap.SetTile(position, BlankTile);
    //            var position = new Vector2Int(x, y);
    //            //SetTileColor(position, map[x, y].SimpleDisplayColor);
    //        }
    //    }
    //}

    //public void RenderAllTilesGray()
    //{
    //    for (int x = 0; x < map.SideLength; x++) //Loop through the width of the map
    //    {
    //        for (int y = 0; y < map.SideLength; y++) //Loop through the height of the map
    //        {
    //            var position = new Vector2Int(x, y);
    //            //SetTileColor(position, FowGrey);
    //        }
    //    }
    //}

    //public void RenderDiscoveredTiles(ControlledDivision division)
    //{
    //    for (int x = 0; x < map.SideLength; x++) //Loop through the width of the map
    //    {
    //        for (int y = 0; y < map.SideLength; y++) //Loop through the height of the map
    //        {
    //            var position = new Vector2Int(x, y);
                
    //            if(division.discoveredMapLocations[x, y])
    //            {
    //                //TerrainTilemap.SetColor(position, _FowGrey);
    //                //SetTileColor(position, FowGrey);
    //            }
    //            else
    //            {
    //                //SetTileColor(position, NotDiscovered);
    //                //TerrainTilemap.SetColor(position, _notDiscovered);
    //            }
    //        }
    //    }
    //}

    //public void RenderMapWithZoneOfControl()
    //{
    //    var controlColors = new List<Color>[map.GetUpperBound(0) + 1, map.GetUpperBound(1) + 1];
        
    //    foreach (var controller in DivisionControllerManager.Instance.Divisions)
    //    {
    //        int sightDistance = Mathf.RoundToInt(controller.AttachedDivision.MaxSightDistance);
    //        Vector2 controllerPosition = controller.transform.position;
    //        Vector3Int controllerPositionRounded = new Vector3Int(VectorUtilityFunctions.RoundVector(controller.transform.position).x, VectorUtilityFunctions.RoundVector(controller.transform.position).y, 0);
    //        //for the x and ys we go one over so that we erase the old sight tiles as we walk past them
    //        for (int x = -sightDistance - 1; x <= sightDistance + 1; x++)
    //        {
    //            for (int y = -sightDistance - 1; y <= sightDistance + 1; y++)
    //            {

    //                var position = new Vector3Int(x, y, 0) + controllerPositionRounded;
    //                var inVision = (new Vector2(position.x, position.y) - controllerPosition).magnitude < controller.AttachedDivision.MaxSightDistance;
    //                var color = NotDiscovered;
    //                float percentDistance = 1 - (new Vector3(x, y, 0).magnitude / 1.5f / sightDistance);
    //                if (InBounds(map, position.x, position.y) && inVision)
    //                {
    //                    if (controlColors[position.x, position.y] == null)
    //                        controlColors[position.x, position.y] = new List<Color>();
                        
    //                    controlColors[position.x, position.y].Add(Color.Lerp(FowGrey, controller.Controller.PlayerColor, percentDistance));
    //                }
    //            }
    //        }
    //    }

    //    for (int x = 0; x < map.SideLength; x++) //Loop through the width of the map
    //    {
    //        for (int y = 0; y < map.SideLength; y++) //Loop through the height of the map
    //        {
    //            var position = new Vector2Int(x, y);
    //            List<Color> colors = controlColors[x, y];
    //            if (colors == null)
    //            {
    //                //TerrainTilemap.SetColor(position, _FowGrey);
    //                //SetTileColor(position, FowGrey);
    //            }
    //            else
    //            {
    //                Color blend = Color.black;
    //                foreach(var color in colors)
    //                {
    //                    blend += color / colors.Count;
    //                }

    //                //SetTileColor(position, blend);
    //                //TerrainTilemap.SetColor(position, blend);
    //            }
    //        }
    //    }
    //}

    //public void RenderMapWithTilesAndVision(DivisionController controller, Color visionColor)
    //{
    //    int sightDistance = Mathf.RoundToInt(controller.AttachedDivision.MaxSightDistance);
    //    if(controller == null)
    //    {
    //        return;
    //    }

    //    Vector2 controllerPosition = controller.transform.position;
    //    Vector2Int controllerPositionRounded = new Vector2Int(VectorUtilityFunctions.RoundVector(controller.transform.position).x, VectorUtilityFunctions.RoundVector(controller.transform.position).y);

    //    //for the x and ys we go one over so that we erase the old sight tiles as we walk past them
    //    for (int x = -sightDistance-1; x <= sightDistance+1; x++)
    //    {
    //        for (int y = -sightDistance-1; y <= sightDistance+1; y++)
    //        {
               
    //            var position = new Vector2Int(x, y) + controllerPositionRounded;
    //            var inVision = (new Vector2(position.x, position.y) - controllerPosition).magnitude < controller.AttachedDivision.MaxSightDistance;
    //            var color = NotDiscovered;
                
    //            if (inVision)
    //            {
    //                color = visionColor;
    //            }
    //            else if(InBounds(controller.AttachedDivision.discoveredMapLocations, position.x, position.y) && controller.AttachedDivision.discoveredMapLocations[position.x, position.y])
    //            {
    //                color = FowGrey;
    //            }
                
    //            //TerrainTilemap.SetTileFlags(position, TileFlags.None);
    //            //TerrainTilemap.SetColor(position, color);

    //            //SetTileColor(position, color);
    //        }
    //    }
    //}

    //public void RenderMapWithKey(Func<TerrainMapTile, float> key)
    //{
    //    double min = float.MaxValue;
    //    double max = float.MinValue;
    //    double sum = 0;

        
    //    for (int x = 0; x < map.SideLength; x++)
    //    {
    //        for (int y = 0; y < map.SideLength; y++)
    //        {
    //            double val = key(map[x, y]);
    //            min = Math.Min(min, val);
    //            max = Math.Max(max, val);
    //            sum += val;
    //        }
    //    }

    //    double average = sum / map.Length;
    //    double rse = 0;

    //    for (int x = 0; x < map.SideLength; x++)
    //    {
    //        for (int y = 0; y < map.SideLength; y++)
    //        {
    //            float val = key(map[x, y]);
    //            rse += Math.Pow(val - average, 2);
    //        }
    //    }

    //    rse /= (map.Length - 1);
    //    double std = Math.Sqrt(rse);

    //    Debug.Log($"min {min}, max {max}, average {average}, std {std} {average - 2 * std} {average + 2 * std}");
    //    min = average - 2 * std;
    //    max = average + 2 * std;

    //    for (int x = 0; x < map.SideLength; x++)
    //    {
    //        for (int y = 0; y < map.SideLength; y++)
    //        {
    //            var position = new Vector2Int(x, y);
    //            float val = key(map[x, y]);
    //            //clamp the threshold to be within the middle 95% of values
    //            val = Mathf.Clamp(val, (float) min, (float) max);
    //            float t = Mathf.InverseLerp((float) min, (float) max, val);
    //            var color = Color.Lerp(Color.red, Color.green, t);
    //            //SetTileColor(position, color);
    //        }
    //    }
    //}

    //public void RenderMapWithKeyAndRange(Func<TerrainMapTile, float> key, float range)
    //{
    //    for (int x = 0; x < map.SideLength; x++) //Loop through the width of the map
    //    {
    //        for (int y = 0; y < map.SideLength; y++) //Loop through the height of the map
    //        {
    //            var position = new Vector2Int(x, y);
    //            var color = Color.Lerp(Color.red, Color.green, key(map[x, y]) / range);
    //            //TerrainTilemap.SetTileFlags(position, TileFlags.None);
    //            //TerrainTilemap.SetColor(position, color);

    //            //SetTileColor(position, color);
    //        }
    //    }
    //}
}
