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
    void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        MapGen.GenerateMap();
        ConvertMapGenerationToTerrainTiles();
        RenderMapWithTiles();
        //RenderMapWithKey(x => x.Population);
    }

    private void ConvertMapGenerationToTerrainTiles()
    {
        var terrainTileLookup = MapGen.LayerSettings.ToDictionary(x => x.terrain, x => x.terrainTile);
        map = new TerrainTile[MapGen.map.GetUpperBound(0), MapGen.map.GetUpperBound(1)];
        for (int i =0; i < MapGen.map.GetUpperBound(0); i++)
        {
            for (int j = 0; j < MapGen.map.GetUpperBound(1); j++)
            {
                map[i, j] = new TerrainTile(terrainTileLookup[MapGen.map[i, j]]);
            }
        }
    }

    public void RenderMapWithTiles()
    {
        Tilemap.ClearAllTiles(); //Clear the map (ensures we dont overlap)
        for (int x = 0; x < map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y < map.GetUpperBound(1); y++) //Loop through the height of the map
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

        for (int x = 0; x < map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y < map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                float val = key(map[x, y]);
                min = Mathf.Min(min, val);
                max = Mathf.Max(max, val);
            }
        }

        Tilemap.ClearAllTiles(); //Clear the map (ensures we dont overlap)
        for (int x = 0; x < map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y < map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                /*
                if (map[x, y] != Terrain.Empty)
                {
                    var settings = layerSettings.Find(terrain => terrain.terrain == map[x, y]);
                    Tilemap.SetTile(new Vector3Int(x, y, 0), settings.tile);
                }
                */
                var position = new Vector3Int(x, y, 0);
                var color = Color.Lerp(Color.red, Color.green, key(map[x, y]) / max);
                Tilemap.SetTile(position, BlankTile);
                Tilemap.SetTileFlags(position, TileFlags.None);
                Tilemap.SetColor(position, color);
                
            }
        }
    }
}
