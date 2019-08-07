using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    public int mapSize;

    public bool UseErosion = true;
    public List<MapLayerSettings> LayerSettings = new List<MapLayerSettings>();

    public Terrain[,] terrainMap;
    public Terrain[,] improvmentMap;
    public float[,] heightMap;
    public Vector2[,] gradientMap;

    public TerrainGenerator HeightmapGen;

    public void GenerateMap(Dictionary<Terrain, TerrainTileSettings> terrainTileLookup)
    {
        ClearMap();
        terrainMap = new Terrain[mapSize, mapSize];
        improvmentMap = new Terrain[mapSize, mapSize];
        heightMap = new float[mapSize, mapSize];

        float seed;
        seed = System.DateTime.Now.Millisecond;
        System.Random rand = new System.Random((int)seed);

        //generate heightmap
        HeightmapGen.GenerateHeightMap(mapSize);
        if(UseErosion)
            HeightmapGen.Erode();

        for (int i = 0; i < mapSize * mapSize; i++)
        {
            int x = i % mapSize;
            int y = i / mapSize;
            int index = y * mapSize + x;
            var erosionBrushRadius = HeightmapGen.erosionBrushRadius;
            var mapSizeWithBorder = HeightmapGen.mapSizeWithBorder;
            int borderedMapIndex = (y + erosionBrushRadius) * mapSizeWithBorder + x + erosionBrushRadius;
            heightMap[x, y] = HeightmapGen.Map[borderedMapIndex];
            terrainMap[x, y] = Terrain.Hill;
        }

        SmoothHeightMap();

        gradientMap = CalculateGradients(heightMap);

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

            if (layerSetting.Layer == MapLayer.Terrain)
            {
                RunAlgorithm(ref terrainMap, ref terrainMap, ref rand, terrainTileLookup, layerSetting);
            }
            else
            {
                RunAlgorithm(ref improvmentMap, ref terrainMap, ref rand, terrainTileLookup, layerSetting);
            }

        }
    }

    public void RunAlgorithm(ref Terrain[,] map, ref Terrain[,] baseTerrainMap, ref System.Random rand, Dictionary<Terrain, TerrainTileSettings> terrainTileLookup, MapLayerSettings layerSetting)
    {
        for (int i = 0; i < layerSetting.iterations; i++)
        {
            switch (layerSetting.algorithm)
            {
                case LayerFillAlgorithm.Solid:
                    map = LayerMapFunctions.GenerateArray(mapSize, mapSize, layerSetting.terrain);
                    break;
                case LayerFillAlgorithm.RandomWalk:
                    map = LayerMapFunctions.RandomWalk2D(ref map, ref baseTerrainMap, rand, layerSetting.terrain, layerSetting.radius, false, terrainTileLookup);
                    break;
                case LayerFillAlgorithm.Square:
                    map = LayerMapFunctions.RandomSquares(map, rand, layerSetting.terrain, layerSetting.radius);
                    break;
                case LayerFillAlgorithm.PerlinNoise:
                    map = LayerMapFunctions.PerlinNoise(ref map, layerSetting.terrain, rand, layerSetting.PerlinNoiseScale, layerSetting.PerlinNoiseThreshold);
                    break;
                case LayerFillAlgorithm.RandomWalkBlocking:
                    map = LayerMapFunctions.RandomWalk2D(ref map, ref baseTerrainMap, rand, layerSetting.terrain, layerSetting.radius, true, terrainTileLookup);
                    break;
                case LayerFillAlgorithm.HeightRange:
                    LayerMapFunctions.FillHeightRange(ref map, ref heightMap, layerSetting.terrain, layerSetting.MinHeight, layerSetting.MaxHeight);
                    break;
                case LayerFillAlgorithm.FollowGradient:
                    LayerMapFunctions.FollowGradient(ref map, ref heightMap, ref gradientMap, rand, layerSetting.terrain, layerSetting.MinStartHeight, layerSetting.MinStopHeight, layerSetting.MaxWidth, layerSetting.WidthChangeThrotle);
                    break;

            }
        }
    }

    private Vector2[,] CalculateGradients(float[,] arr)
    {
        var gradientMap = new Vector2[arr.GetUpperBound(0) + 1, arr.GetUpperBound(1) + 1];
        //loop through every tile
        for (int x = 0; x <= arr.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= arr.GetUpperBound(1); y++)
            {
                //loop through every tiles neighbors
                for (int i = x - 1; i <= x + 1; i++)
                {
                    for (int j = y - 1; j <= y + 1; j++)
                    {
                        if (MapManager.InBounds(arr, i, j))
                        {
                            //adjacents.Add(map[i, j]);
                            int d_x = i - x, d_y = j - y;
                            var dir = new Vector2(d_x, d_y);
                            var delta =  arr[i, j] - arr[x, y];
                            
                            Vector2 localGradient = dir * delta;
                            gradientMap[x, y] += localGradient;
                        }
                    }
                }

            }
        }

        return gradientMap;
    }

    /// <summary>
    /// remove jagged edges
    /// </summary>
    private void SmoothHeightMap()
    {
        //gausian smoothing filter
        var kernel = new List<List<float>>()
        {
            new List<float>{1,4,7,4,1 },
            new List<float>{4,16,26,16,4},
            new List<float>{7,26,41,26,7},
            new List<float>{4,16,26,16,4},
            new List<float>{1,4,7,4,1 },
        };

        int k_h = kernel.Count;
        int k_w = kernel[0].Count;

        for (int k_x = 0; k_x < k_w; k_x++)
        {
            for (int k_y = 0; k_y < k_h; k_y++)
            {
                kernel[k_y][k_x] /= 273f;
            }
        }

        heightMap = Convolution2D(heightMap, kernel);
    }

    public float[,] Convolution2D(float[,] arr, List<List<float>> kernel)
    {
        var convolutedArr = new float[arr.GetUpperBound(0)+1, arr.GetUpperBound(1)+1];
        int k_h = kernel.Count;
        int k_w = kernel[0].Count;

        float min = 1000, max = -1000;

        //do a 2d convolution
        for (int x = 0; x <= arr.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= arr.GetUpperBound(1); y++)
            {
                for (int k_x = 0; k_x < k_w; k_x++)
                {
                    for (int k_y = 0; k_y < k_h; k_y++)
                    {
                        float k = kernel[k_y][k_x];
                        int heightMapOffset_x = x + k_x - (k_w / 2);
                        int heightMapOffset_y = y + k_y - (k_h / 2);

                        if (MapManager.InBounds(arr, heightMapOffset_x, heightMapOffset_y))
                            convolutedArr[x, y] += arr[heightMapOffset_x, heightMapOffset_y] * k;
                    }
                }
                min = Mathf.Min(min, convolutedArr[x, y]);
                max = Mathf.Max(max, convolutedArr[x, y]);
            }
        }

        //fix ranges
        for (int x = 0; x <= convolutedArr.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= convolutedArr.GetUpperBound(1); y++)
            {
                convolutedArr[x, y] = Mathf.InverseLerp(min, max, convolutedArr[x, y]);
            }
        }

        return convolutedArr;
    }

    public void ClearMap()
    {
        terrainMap = new Terrain[mapSize, mapSize];
    }
}
