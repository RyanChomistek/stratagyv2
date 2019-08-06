using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [Tooltip("The Tilemap to draw onto")]
    public Tilemap Tilemap;

    [Tooltip("Width of our map")]
    public int Width;
    [Tooltip("Height of our map")]
    public int Height;

    public List<MapLayerSettings> LayerSettings = new List<MapLayerSettings>();

    public Terrain[,] terrainMap;
    public Terrain[,] improvmentMap;

    public void GenerateMap()
    {
        ClearMap();
        terrainMap = new Terrain[Width+1, Height+1];
        improvmentMap = new Terrain[Width+1, Height+1];
        float seed;
        seed = System.DateTime.Now.Millisecond;
        System.Random rand = new System.Random((int)seed);

        foreach (MapLayerSettings layerSetting in LayerSettings)
        {
            if (!layerSetting.IsEnabled)
            {
                continue;
            }

            if (layerSetting.randomSeed)
            {
                seed = System.DateTime.Now.Millisecond + seed;
                rand = new System.Random((int)seed);
            }
            else
            {
                seed = layerSetting.seed;
                rand = new System.Random((int)seed);
            }

            if(layerSetting.Layer == MapLayer.Terrain)
            {
                RunAlgorithm(ref terrainMap, ref rand, layerSetting);
            }
            else
            {
                RunAlgorithm(ref improvmentMap, ref rand, layerSetting);
            }

        }

        //LayerMapFunctions.RenderMapWithTiles(map, Tilemap, LayerSettings);
    }

    public void RunAlgorithm(ref Terrain[,] map, ref System.Random rand, MapLayerSettings layerSetting)
    {
        for (int i = 0; i < layerSetting.iterations; i++)
        {
            switch (layerSetting.algorithm)
            {
                case LayerFillAlgorithm.Solid:
                    map = LayerMapFunctions.GenerateArray(Width, Height, layerSetting.terrain);
                    break;
                case LayerFillAlgorithm.RandomWalk:
                    map = LayerMapFunctions.RandomWalk2D(map, rand, layerSetting.terrain, layerSetting.radius);
                    break;
                case LayerFillAlgorithm.Square:
                    map = LayerMapFunctions.RandomSquares(map, rand, layerSetting.terrain, layerSetting.radius);
                    break;
                case LayerFillAlgorithm.PerlinNoise:
                    map = LayerMapFunctions.PerlinNoise(ref map, layerSetting.terrain, rand, layerSetting.PerlinNoiseScale, layerSetting.PerlinNoiseThreshold);
                    break;
            }
        }
    }

    public void ClearMap()
    {
        Tilemap.ClearAllTiles();
        terrainMap = new Terrain[Width, Height];
    }
}
