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

    Terrain[,] map;

    public void GenerateMap()
    {
        ClearMap();
        map = new Terrain[Width, Height];
        
        foreach (var layerSetting in LayerSettings)
        {
            //Seed our random
            System.Random rand = new System.Random(layerSetting.seed.GetHashCode());
            for (int i = 0; i < layerSetting.iterations; i++)
            {
                switch (layerSetting.algorithm)
                {
                    case LayerFillAlgorithm.Solid:
                        //Debug.Log("solid");
                        //map = LayerMapFunctions.GenerateArray(Width, Height, layerSetting.terrain);
                        break;
                    case LayerFillAlgorithm.RandomWalk:
                        //Debug.Log("walk");
                        map = LayerMapFunctions.RandomWalk2D(map, rand, layerSetting.terrain);
                        break;
                }
            }
        }

        LayerMapFunctions.RenderMap(map, Tilemap, LayerSettings);
    }

    public void ClearMap()
    {
        Tilemap.ClearAllTiles();
        map = new Terrain[Width, Height];
    }
}
