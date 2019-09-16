﻿using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public MapTerrainTile BaseTerrainTile;
    public MapGenerator MapGen;
    [Tooltip("The Tilemap to draw onto")]
    public List<Tilemap> TerrainTileLayers;
    public Tilemap ImprovementTilemap;
    public MapTerrainTile[,] map;
    public TileBase BlankTile;
    public GameObject ZLayerPrefab;

    private float _minHeight, _maxHeight;
    public int NumZLayers = 5;

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
        StartCoroutine(UpdateTileValues());
        //InputController.Instance.RegisterOnClickCallBack(PrintTile);
        ButtonHandler Handler = new ButtonHandler(ButtonHandler.LeftClick, (x, y) => { },
            (handler, mousePos) => {
                PrintTile(mousePos);
            });

        InputController.Instance.RegisterButtonHandler(Handler);

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
        Debug.Log(tile.ToString());
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
        Dictionary<Terrain, TerrainTileSettings> terrainTileLookup = new Dictionary<Terrain, TerrainTileSettings>();
        foreach (MapLayerSettings layer in MapGen.LayerSettings)
        {
            if (!terrainTileLookup.ContainsKey(layer.terrain))
            {
                terrainTileLookup[layer.terrain] = layer.terrainTile;
            }
        }

        MapGen.GenerateMap(terrainTileLookup, NumZLayers);
        ConvertMapGenerationToTerrainTiles(terrainTileLookup);
        SetUpAjdacentTiles();
        CreateTileMapLayers();
        RenderMap(MapDisplays.Tiles);
    }

    #region position conversion and helpers
    public MapTerrainTile GetTileFromPosition(Vector3 position)
    {
        var gridStart = TerrainTileLayers[0].transform.position;
        var deltaFromStart = position - gridStart;
        var rounded = LayerMapFunctions.FloorVector(deltaFromStart);
        return map[rounded.x, rounded.y];
    }

    public Vector2Int GetTilePositionFromPosition(Vector3 position)
    {
        var gridStart = TerrainTileLayers[0].transform.position;
        var deltaFromStart = position - gridStart;
        var rounded = LayerMapFunctions.FloorVector(deltaFromStart);
        return rounded;
    }

    public Vector2Int ClampTilePositionToInBounds(Vector2Int position)
    {
        int x = (int) Mathf.Clamp(position.x, 0, map.GetUpperBound(0));
        int y = (int) Mathf.Clamp(position.y, 0, map.GetUpperBound(1));
        return new Vector2Int(x, y);
    }

    public Vector3 ClampPositionToInBounds(Vector3 position)
    {
        var gridStart = TerrainTileLayers[0].transform.position;
        float x = Mathf.Clamp(position.x, - gridStart.x, map.GetUpperBound(0) - gridStart.x - 1);
        float y = Mathf.Clamp(position.y, - gridStart.y, map.GetUpperBound(1) - gridStart.y - 1);
        return new Vector3(x, y);
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
                                nodeArray[x,y].AddConnection(nodeArray[i,j], (uint) map[x,y].MoveCost);
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

    private void ConvertMapGenerationToTerrainTiles(Dictionary<Terrain, TerrainTileSettings> terrainTileLookup)
    {
        _minHeight = 100;
        _maxHeight = -100;
        map = new MapTerrainTile[MapGen.terrainMap.GetUpperBound(0)+1, MapGen.terrainMap.GetUpperBound(1)+1];
        for (int i =0; i <= MapGen.terrainMap.GetUpperBound(0); i++)
        {
            for (int j = 0; j <= MapGen.terrainMap.GetUpperBound(1); j++)
            {
                map[i, j] = new MapTerrainTile(terrainTileLookup[MapGen.terrainMap[i, j]].tile, MapGen.heightMap[i,j], MapGen.LayeredGradientMap[i, j]);
                map[i, j].Improvement = new MapTerrainTile(terrainTileLookup[MapGen.improvmentMap[i, j]].tile, MapGen.heightMap[i, j], MapGen.LayeredGradientMap[i, j]);
                map[i, j].ModifyBaseWithImprovement();
                _minHeight = Mathf.Min(_minHeight, MapGen.heightMap[i, j]);
                _maxHeight = Mathf.Max(_maxHeight, MapGen.heightMap[i, j]);
            }
        }

        //Debug.Log($"min {_minHeight}, max {_maxHeight}");
    }

    private void CreateTileMapLayers()
    {
        TerrainTileLayers.ForEach(x => DestroyImmediate(x.gameObject));
        TerrainTileLayers.Clear();

        for (int i = 0; i < NumZLayers; i++)
        {
            var layer = Instantiate(ZLayerPrefab);
            Tilemap tileMap = layer.GetComponent<Tilemap>();
            TerrainTileLayers.Add(tileMap);
            tileMap.GetComponent<TilemapRenderer>().sortingOrder = i;
            layer.transform.parent = transform;
        }
    }

    public void Rerender()
    {
        RenderMap(CurrentlyDisplayingMapType);
    }

    public void RenderMap(MapDisplays mapDisplay)
    {
        CurrentlyDisplayingMapType = mapDisplay;
        switch (mapDisplay)
        {
            case MapDisplays.Population:
                //this.RenderMapWithKeyAndRange(x => x.Population, 1000);
                this.RenderMapWithKey(x => x.Population);
                break;
            case MapDisplays.Supply:
                //this.RenderMapWithKeyAndRange(x => x.Supply, 1000);
                this.RenderMapWithKey(x => x.Supply);
                break;
            case MapDisplays.HeightMap:
                this.RenderMapWithKey(x => x.Height);
                break;
            case MapDisplays.HeightGradient:
                this.RenderMapWithKey(x => x.HeightGradient.magnitude);
                break;
            case MapDisplays.Tiles:
                this.RenderMapWithTiles();
                break;
            case MapDisplays.TilesWithVision:
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
            case MapDisplays.PlayerControlledAreas:
                this.RenderMapWithZoneOfControl();
                break;
        }
        
        _onMapRerender?.Invoke();
    }

    public void RenderMapWithTiles()
    {
        TerrainTileLayers.ForEach(x => x.ClearAllTiles());
        ImprovementTilemap.ClearAllTiles();
        int min = 1000;
        int cnt = 0;
        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                var tile = map[x, y];
                int highestLayer = GetLayerIndexByHeight(tile.Height);
                min = Mathf.Min(min, highestLayer);
                for (int z = 0; z <= highestLayer; z ++)
                {
                    TerrainTileLayers[z].SetTile(new Vector3Int(x, y, 0), map[x, y].tile);
                }

                ImprovementTilemap.SetTile(new Vector3Int(x, y, 0), map[x, y].Improvement.tile);
                cnt += (int) map[x, y].Improvement.TerrainType;
            }
        }
    }

    public void SetTileColor(Vector2Int pos, Color color)
    {
        int x = pos.x, y = pos.y;
        var position = new Vector3Int(pos.x, pos.y, 0);
        var tile = map[x, y];
        int highestLayer = GetLayerIndexByHeight(tile.Height);

        TerrainTileLayers[highestLayer].SetTileFlags(position, TileFlags.None);
        TerrainTileLayers[highestLayer].SetColor(position, color);
        ImprovementTilemap.SetTileFlags(position, TileFlags.None);
        ImprovementTilemap.SetColor(position, color);


    }

    public Color GetTileColor(Vector2Int pos)
    {
        int x = pos.x, y = pos.y;
        var tile = map[x, y];
        int highestLayer = GetLayerIndexByHeight(tile.Height);
        var position = new Vector3Int(pos.x, pos.y, 0);
        return TerrainTileLayers[highestLayer].GetColor(position);
    }

    public void RenderSimple()
    {
        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                //Tilemap.SetTile(position, BlankTile);
                var position = new Vector2Int(x, y);
                SetTileColor(position, map[x, y].SimpleDisplayColor);
            }
        }
    }

    public void RenderAllTilesGray()
    {
        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                var position = new Vector2Int(x, y);
                SetTileColor(position, _FowGrey);
            }
        }
    }

    public void RenderDiscoveredTiles(ControlledDivision division)
    {
        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                var position = new Vector2Int(x, y);
                
                if(division.discoveredMapLocations[x, y])
                {
                    //TerrainTilemap.SetColor(position, _FowGrey);
                    SetTileColor(position, _FowGrey);
                }
                else
                {
                    SetTileColor(position, _notDiscovered);
                    //TerrainTilemap.SetColor(position, _notDiscovered);
                }
            }
        }
    }

    public static Vector2Int RoundVector(Vector2 vec)
    {
        return new Vector2Int(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y));
    }

    public void RenderMapWithZoneOfControl()
    {
        var controlColors = new List<Color>[map.GetUpperBound(0) + 1, map.GetUpperBound(1) + 1];
        
        foreach (var controller in DivisionControllerManager.Instance.Divisions)
        {
            int sightDistance = Mathf.RoundToInt(controller.AttachedDivision.MaxSightDistance);
            Vector2 controllerPosition = controller.transform.position;
            Vector3Int controllerPositionRounded = new Vector3Int(RoundVector(controller.transform.position).x, RoundVector(controller.transform.position).y, 0);
            //for the x and ys we go one over so that we erase the old sight tiles as we walk past them
            for (int x = -sightDistance - 1; x <= sightDistance + 1; x++)
            {
                for (int y = -sightDistance - 1; y <= sightDistance + 1; y++)
                {

                    var position = new Vector3Int(x, y, 0) + controllerPositionRounded;
                    var inVision = (new Vector2(position.x, position.y) - controllerPosition).magnitude < controller.AttachedDivision.MaxSightDistance;
                    var color = _notDiscovered;
                    float percentDistance = 1 - (new Vector3(x, y, 0).magnitude / 1.5f / sightDistance);
                    if (InBounds(map, position.x, position.y) && inVision)
                    {
                        if (controlColors[position.x, position.y] == null)
                            controlColors[position.x, position.y] = new List<Color>();
                        
                        controlColors[position.x, position.y].Add(Color.Lerp(_FowGrey, controller.Controller.PlayerColor, percentDistance));
                    }
                }
            }
        }

        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                var position = new Vector2Int(x, y);
                List<Color> colors = controlColors[x, y];
                if (colors == null)
                {
                    //TerrainTilemap.SetColor(position, _FowGrey);
                    SetTileColor(position, _FowGrey);
                }
                else
                {
                    Color blend = Color.black;
                    foreach(var color in colors)
                    {
                        blend += color / colors.Count;
                    }

                    SetTileColor(position, blend);
                    //TerrainTilemap.SetColor(position, blend);
                }
            }
        }
    }

    public void RenderMapWithTilesAndVision(DivisionController controller, Color visionColor)
    {
        int sightDistance = Mathf.RoundToInt(controller.AttachedDivision.MaxSightDistance);
        Vector2 controllerPosition = controller.transform.position;
        Vector2Int controllerPositionRounded = new Vector2Int(RoundVector(controller.transform.position).x, RoundVector(controller.transform.position).y);

        //for the x and ys we go one over so that we erase the old sight tiles as we walk past them
        for (int x = -sightDistance-1; x <= sightDistance+1; x++)
        {
            for (int y = -sightDistance-1; y <= sightDistance+1; y++)
            {
               
                var position = new Vector2Int(x, y) + controllerPositionRounded;
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
                
                //TerrainTilemap.SetTileFlags(position, TileFlags.None);
                //TerrainTilemap.SetColor(position, color);

                SetTileColor(position, color);
            }
        }
    }

    public void RenderMapWithKey(Func<MapTerrainTile, float> key)
    {
        double min = float.MaxValue;
        double max = float.MinValue;
        double sum = 0;
        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                double val = key(map[x, y]);
                min = Math.Min(min, val);
                max = Math.Max(max, val);
                sum += val;
            }
        }

        double average = sum / map.Length;
        double rse = 0;

        for (int x = 0; x <= map.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++)
            {
                float val = key(map[x, y]);
                rse += Math.Pow(val - average, 2);
            }
        }

        rse /= (map.Length - 1);
        double std = Math.Sqrt(rse);

        Debug.Log($"min {min}, max {max}, average {average}, std {std} {average - 2 * std} {average + 2 * std}");
        min = average - 2 * std;
        max = average + 2 * std;
        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                var position = new Vector2Int(x, y);
                float val = key(map[x, y]);
                //clamp the threshold to be within the middle 95% of values
                val = Mathf.Clamp(val, (float) min, (float) max);
                float t = Mathf.InverseLerp((float) min, (float) max, val);
                var color = Color.Lerp(Color.red, Color.green, t);
                //TerrainTilemap.SetTileFlags(position, TileFlags.None);
                //TerrainTilemap.SetColor(position, color);
                SetTileColor(position, color);
            }
        }
    }

    public void RenderMapWithKeyAndRange(Func<MapTerrainTile, float> key, float range)
    {
        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                var position = new Vector2Int(x, y);
                var color = Color.Lerp(Color.red, Color.green, key(map[x, y]) / range);
                //TerrainTilemap.SetTileFlags(position, TileFlags.None);
                //TerrainTilemap.SetColor(position, color);

                SetTileColor(position, color);
            }
        }
    }
}
