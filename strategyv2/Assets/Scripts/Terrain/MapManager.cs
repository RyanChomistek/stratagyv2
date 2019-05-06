using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    public TerrainTile BaseTerrainTile;
    public MapGenerator MapGen;
    [Tooltip("The Tilemap to draw onto")]
    public Tilemap Tilemap;
    public TerrainTile[,] map;
    public TileBase BlankTile;

    public bool GenNewMapOnStart = false;
    void Start()
    {
        if(GenNewMapOnStart)
            GenerateMap();
    }

    public void GenerateMap()
    {
        MapGen.GenerateMap();
        ConvertMapGenerationToTerrainTiles();
        RenderMapWithTiles();
        //RenderMapWithKey(x => x.Population);
        CreateGraph();
    }

    public static bool InBounds(PointNode[,] map, int x, int y)
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
                    //Debug.Log($"{x} {y} {map[x,y].TerrainType} {map[x, y].MoveCost}");
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
                            // && map[i, j].TerrainType != Terrain.Water
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
        map = new TerrainTile[MapGen.map.GetUpperBound(0)+1, MapGen.map.GetUpperBound(1)+1];
        for (int i =0; i <= MapGen.map.GetUpperBound(0); i++)
        {
            for (int j = 0; j <= MapGen.map.GetUpperBound(1); j++)
            {
                map[i, j] = new TerrainTile(terrainTileLookup[MapGen.map[i, j]]);
            }
        }
    }

    public void RenderMapWithTiles()
    {
        Tilemap.ClearAllTiles(); //Clear the map (ensures we dont overlap)
        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                /*
                if (map[x, y] != Terrain.Empty)
                {
                    var settings = layerSettings.Find(terrain => terrain.terrain == map[x, y]);
                    Tilemap.SetTile(new Vector3Int(x, y, 0), settings.tile);
                }
                */
                Tilemap.SetTile(new Vector3Int(x, y, 0), map[x, y].tile);
            }
        }
    }

    public void RenderMapWithKey(Func<TerrainTile, float> key)
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
                //Debug.Log($"{map[x, y].TerrainType.ToString()} {key(map[x, y]) / max} {color}");
                //Tilemap.SetTile(position, map[x, y].tile);
                //Tilemap.SetTile(position, BlankTile);
                Tilemap.SetTileFlags(position, TileFlags.None);
                Tilemap.SetColor(position, color);
                
            }
        }
    }
}
