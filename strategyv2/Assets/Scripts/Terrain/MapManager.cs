using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum MapDisplays
{
    Tiles, TilesWithVision, Population, Supply, MovementSpeed, Simple
}

public class MapManager : MonoBehaviour
{
    private static MapManager _instance;
    public static MapManager Instance { get { return _instance; } }
    public MapTerrainTile BaseTerrainTile;
    public MapGenerator MapGen;
    [Tooltip("The Tilemap to draw onto")]
    public Tilemap Tilemap;
    public MapTerrainTile[,] map;
    public TileBase BlankTile;

    public bool GenNewMapOnStart = false;
    public MapDisplays CurrentlyDisplayingMapType;

    private Color _FowGrey = new Color(.25f, .25f, .25f, 1);
    private Color _notDiscovered = new Color(0f, 0f, 0f, 1);
    private Color _playerVision = new Color(1, 1, 1, 1);
    private Color _otherDivisionsVision = new Color(.5f, .5f, .5f, 1);

    [SerializeField]
    private float _tileUpdateTickTime = 1;

    private Action _onMapRerender;

    private void Awake()
    {
        _instance = this;
        if (GenNewMapOnStart)
            GenerateMap();

        CurrentlyDisplayingMapType = MapDisplays.TilesWithVision;
    }

    void Start()
    {
        CreateGraph();
        SetUpAjdacentTiles();
        StartCoroutine(UpdateTileValues());
        InputController.Instance.RegisterOnClickCallBack(PrintTile);
        LocalPlayerController.Instance.GeneralDivision.AttachedDivision.OnDiscoveredMapChanged += x => Rerender();
    }

    private void Update()
    {
        if(CurrentlyDisplayingMapType == MapDisplays.TilesWithVision)
        {
            //show the players vision range
            RenderMapWithTilesAndVision(LocalPlayerController.Instance.GeneralDivision, _playerVision);
            _onMapRerender?.Invoke();
        }
    }

    public bool[,] GetMapMask()
    {
        bool[,] mask = new bool[map.GetUpperBound(1)+1, map.GetUpperBound(0)+1];
        for (int i = 0; i <= mask.GetUpperBound(0); i++)
        {
            for (int j = 0; j <= mask.GetUpperBound(1); j++)
            {
                mask[i, j] = false;
            }
        }
        return mask;
    }

    public void RegisterOnMapRerenderCallback(Action callback)
    {
        _onMapRerender += callback;
    }

    private void PrintTile(Vector3 pos)
    {
        var tile = GetTileFromPosition(pos);
        Debug.Log(tile);
    }

    private void SetUpAjdacentTiles()
    {
        List<MapTerrainTile> adjacents = new List<MapTerrainTile>();
        for (int y = 0; y <= map.GetUpperBound(0); y++)
        {
            for (int x = 0; x <= map.GetUpperBound(1); x++)
            {
                adjacents.Clear();
                for (int i = x - 1; i <= x + 1; i++)
                {
                    for (int j = y - 1; j <= y + 1; j++)
                    {
                        if (InBounds(map, i, j))
                        {
                            adjacents.Add(map[i, j]);
                        }
                    }
                }

                map[x, y].SetAdjacentTiles(adjacents);
            }
        }
    }

    private IEnumerator UpdateTileValues()
    {
        float timeSinceLastTick = 0;
        float GameTime = 0;
        while(true)
        {
            while (timeSinceLastTick < _tileUpdateTickTime)
            {
                timeSinceLastTick += GameManager.DeltaTime;
                GameTime += GameManager.DeltaTime;
                yield return new WaitForEndOfFrame();
            }

            float max = 0;
            float min = 0;
            for (int y = 0; y <= map.GetUpperBound(0); y++)
            {
                for (int x = 0; x <= map.GetUpperBound(1); x++)
                {
                    map[x, y].Update(GameTime);
                    max = Mathf.Max(max, map[x, y].Supply);
                    min = Mathf.Min(min, map[x, y].Supply);

                }
            }

            //RenderMap(CurrentlyDisplayingMapType);

            timeSinceLastTick = 0;
        }
    }

    public void GenerateMap()
    {
        MapGen.GenerateMap();
        ConvertMapGenerationToTerrainTiles();
        RenderMap(MapDisplays.Tiles);
    }

    public MapTerrainTile GetTileFromPosition(Vector3 position)
    {
        var gridStart = Tilemap.transform.position;
        var deltaFromStart = position - gridStart;
        var rounded = LayerMapFunctions.FloorVector(deltaFromStart);
        return map[rounded.x, rounded.y];
    }

    public Vector2Int GetTilePositionFromPosition(Vector3 position)
    {
        var gridStart = Tilemap.transform.position;
        var deltaFromStart = position - gridStart;
        var rounded = LayerMapFunctions.FloorVector(deltaFromStart);
        return rounded;
    }

    public static bool InBounds<T>(T[,] map, int x, int y)
    {
        //Debug.Log($"{position}{map.GetUpperBound(0)} {position.x <= map.GetUpperBound(0)} { position.y <= map.GetUpperBound(1)} { position.x > 0} { position.y > 0} ");
        if (x <= map.GetUpperBound(0) &&
            y <= map.GetUpperBound(1) &&
            x >= 0 &&
            y >= 0)
        {
            return true;
        }

        return false;
    }

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
            //create the graph
            //first make node array
            PointNode[,] nodeArray = new PointNode[map.GetUpperBound(0) + 1, map.GetUpperBound(1) + 1];

            for (int y = 0; y <= map.GetUpperBound(0); y++)
            {
                for (int x = 0; x <= map.GetUpperBound(1); x++)
                {
                    nodeArray[x,y] = graph.AddNode((Int3)new Vector3(x, y, 0));
                }
            }
            int connections = 0;
            //now connect nodes
            for (int y = 0; y <= map.GetUpperBound(0); y++)
            {
                for (int x = 0; x <= map.GetUpperBound(1); x++)
                {
                    for (int i = x-1; i <= x+1; i++)
                    {
                        for (int j = y-1; j <= y+1; j++)
                        {
                            if (InBounds(nodeArray, i, j))
                            {
                                nodeArray[x,y].AddConnection(nodeArray[i,j], map[x,y].MoveCost);
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

    private void ConvertMapGenerationToTerrainTiles()
    {
        var terrainTileLookup = MapGen.LayerSettings.ToDictionary(x => x.terrain, x => x.terrainTile);
        map = new MapTerrainTile[MapGen.map.GetUpperBound(0)+1, MapGen.map.GetUpperBound(1)+1];
        for (int i =0; i <= MapGen.map.GetUpperBound(0); i++)
        {
            for (int j = 0; j <= MapGen.map.GetUpperBound(1); j++)
            {
                map[i, j] = new MapTerrainTile(terrainTileLookup[MapGen.map[i, j]]);
            }
        }
    }

    public void Rerender()
    {
        Debug.Log("rerender");
        RenderMap(CurrentlyDisplayingMapType);
    }

    public void RenderMap(MapDisplays mapDisplay)
    {
        CurrentlyDisplayingMapType = mapDisplay;
        switch (mapDisplay)
        {
            case MapDisplays.Population:
                //this.RenderMapWithKeyAndRange(x => x.Population, 1000);
                this.RenderMapWithKey(x => x.Population/x.MaxPopulation);
                break;
            case MapDisplays.Supply:
                //this.RenderMapWithKeyAndRange(x => x.Supply, 1000);
                this.RenderMapWithKey(x => x.Supply / x.MaxSupply);
                break;
            case MapDisplays.Tiles:
                this.RenderMapWithTiles();
                break;
            case MapDisplays.TilesWithVision:
                Debug.Log("visibility");
                //RenderAllTilesGray();
                RenderDiscoveredTiles(LocalPlayerController.Instance.GeneralDivision.AttachedDivision);
                RenderMapWithTilesAndVision(LocalPlayerController.Instance.GeneralDivision, _playerVision);
                break;
            case MapDisplays.MovementSpeed:
                this.RenderMapWithKey(x => 1 / (float)x.MoveCost);
                break;
            case MapDisplays.Simple:
                this.RenderSimple();
                break;
        }
        
        _onMapRerender?.Invoke();
    }

    public void RenderMapWithTiles()
    {
        Tilemap.ClearAllTiles(); //Clear the map (ensures we dont overlap)
        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                Tilemap.SetTile(new Vector3Int(x, y, 0), map[x, y].tile);
            }
        }
    }

    public void SetTileColor(Vector2Int pos, Color color)
    {
        var position = new Vector3Int(pos.x, pos.y, 0);
        Tilemap.SetTileFlags(position, TileFlags.None);
        Tilemap.SetColor(position, color);
    }

    public Color GetTileColor(Vector2Int pos)
    {
        var position = new Vector3Int(pos.x, pos.y, 0);
        return Tilemap.GetColor(position);
    }

    public void RenderSimple()
    {
        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                //Tilemap.SetTile(position, BlankTile);
                var position = new Vector3Int(x, y, 0);
                Tilemap.SetTileFlags(position, TileFlags.None);
                Tilemap.SetColor(position, map[x, y].SimpleDisplayColor);
                //Debug.Log(map[x, y].SimpleDisplayColor);
            }
        }
    }

    public void RenderAllTilesGray()
    {
        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                var position = new Vector3Int(x, y, 0);
                Tilemap.SetTileFlags(position, TileFlags.None);
                Tilemap.SetColor(position, _FowGrey);
            }
        }
    }

    public void RenderDiscoveredTiles(ControlledDivision division)
    {
        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                var position = new Vector3Int(x, y, 0);
                Tilemap.SetTileFlags(position, TileFlags.None);
                
                if(division.discoveredMapLocations[x, y])
                {
                    Tilemap.SetColor(position, _FowGrey);
                }
                else
                {
                    Tilemap.SetColor(position, _notDiscovered);
                }
            }
        }
    }

    public static Vector2Int RoundVector(Vector2 vec)
    {
        return new Vector2Int(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y));
    }

    public void RenderMapWithTilesAndVision(DivisionController controller, Color visionColor)
    {
        int sightDistance = Mathf.RoundToInt(controller.AttachedDivision.MaxSightDistance);
        Vector2 controllerPosition = controller.transform.position;
        Vector3Int controllerPositionRounded = new Vector3Int(RoundVector(controller.transform.position).x, RoundVector(controller.transform.position).y,0);

        //for the x and ys we go one over so that we erase the old sight tiles as we walk past them
        for (int x = -sightDistance-1; x <= sightDistance+1; x++)
        {
            for (int y = -sightDistance-1; y <= sightDistance+1; y++)
            {
               
                var position = new Vector3Int(x, y, 0) + controllerPositionRounded;
                var inVision = (new Vector2(position.x, position.y) - controllerPosition).magnitude < controller.AttachedDivision.MaxSightDistance;
                var color = _notDiscovered;
                
                if (inVision)
                {
                    color = visionColor;
                }
                else if(InBounds(controller.AttachedDivision.discoveredMapLocations, position.x, position.y) && controller.AttachedDivision.discoveredMapLocations[position.x, position.y])
                {
                    color = _FowGrey;
                }
                
                Tilemap.SetTileFlags(position, TileFlags.None);
                Tilemap.SetColor(position, color);
            }
        }
    }

    public void RenderMapWithKey(Func<MapTerrainTile, float> key)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                float val = key(map[x, y]);
                min = Mathf.Min(min, val);
                max = Mathf.Max(max, val);
            }
        }

        //Tilemap.ClearAllTiles(); //Clear the map (ensures we dont overlap)
        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                var position = new Vector3Int(x, y, 0);
                var color = Color.Lerp(Color.red, Color.green, key(map[x, y]) / max);
                Tilemap.SetTileFlags(position, TileFlags.None);
                Tilemap.SetColor(position, color);
                
            }
        }
    }

    public void RenderMapWithKeyAndRange(Func<MapTerrainTile, float> key, float range)
    {
        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                var position = new Vector3Int(x, y, 0);
                var color = Color.Lerp(Color.red, Color.green, key(map[x, y]) / range);
                Tilemap.SetTileFlags(position, TileFlags.None);
                Tilemap.SetColor(position, color);
            }
        }
    }
}
