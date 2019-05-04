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

    public Terrain[,] map;

    public void GenerateMap()
    {
        ClearMap();
        map = new Terrain[Width, Height];
        float seed;
        seed = Time.time;
        System.Random rand = new System.Random((int)seed);

        foreach (var layerSetting in LayerSettings)
        {
            if (layerSetting.randomSeed)
            {
                seed = Time.time;
            }
            else
            {
                seed = layerSetting.seed;
                rand = new System.Random((int)seed);
            }

            //Debug.Log(seed);
            
            for (int i = 0; i < layerSetting.iterations; i++)
            {
                switch (layerSetting.algorithm)
                {
                    case LayerFillAlgorithm.Solid:
                        //Debug.Log("solid");
                        map = LayerMapFunctions.GenerateArray(Width, Height, layerSetting.terrain);
                        break;
                    case LayerFillAlgorithm.RandomWalk:
                        //Debug.Log("walk");
                        map = LayerMapFunctions.RandomWalk2D(map, rand, layerSetting.terrain, layerSetting.radius);
                        break;
                    case LayerFillAlgorithm.Square:
                        //Debug.Log("walk");
                        map = LayerMapFunctions.RandomSquares(map, rand, layerSetting.terrain, layerSetting.radius);
                        break;
                }
            }
        }

        //LayerMapFunctions.RenderMapWithTiles(map, Tilemap, LayerSettings);
    }

    public void ClearMap()
    {
        Tilemap.ClearAllTiles();
        map = new Terrain[Width, Height];
    }
}
