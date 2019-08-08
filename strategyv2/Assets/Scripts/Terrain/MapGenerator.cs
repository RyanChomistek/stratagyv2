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
    public Vector2[,] LayeredGradientMap;

    public TerrainGenerator HeightmapGen;

    public void GenerateMap(Dictionary<Terrain, TerrainTileSettings> terrainTileLookup, int numZLayers)
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
        if (UseErosion)
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

        LayerMapFunctions.Smooth(ref heightMap);
        Vector2[,] unflattendGradientMap = CalculateGradients(heightMap);

        LayerHeightMap(numZLayers);
        LayeredGradientMap = CalculateGradients(heightMap);

        foreach (MapLayerSettings layerSetting in LayerSettings)
        {
            if (!layerSetting.IsEnabled)
            {
                continue;
            }

            Vector2[,] gradientMapInUse;

            if (layerSetting.useLayeredGradients)
            {
                gradientMapInUse = LayeredGradientMap;
            }
            else
            {
                gradientMapInUse = unflattendGradientMap;
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
                RunAlgorithm(ref terrainMap, ref terrainMap, ref improvmentMap, ref gradientMapInUse, ref rand, terrainTileLookup, layerSetting);
            }
            else
            {
                RunAlgorithm(ref improvmentMap, ref terrainMap, ref improvmentMap, ref gradientMapInUse, ref rand, terrainTileLookup, layerSetting);
            }

            //recalculate gradients becasue they might have changed
            LayeredGradientMap = CalculateGradients(heightMap);
        }

        //SmoothHeightMap();
        //gradientMap = CalculateGradients(heightMap);
    }

    public void RunAlgorithm(ref Terrain[,] map, ref Terrain[,] baseTerrainMap, ref Terrain[,] baseImprovementMap, ref Vector2[,] gradientMap, ref System.Random rand, Dictionary<Terrain, TerrainTileSettings> terrainTileLookup, MapLayerSettings layerSetting)
    {
        for (int i = 0; i < layerSetting.iterations; i++)
        {
            switch (layerSetting.algorithm)
            {
                case LayerFillAlgorithm.Solid:
                    map = LayerMapFunctions.GenerateArray(mapSize, mapSize, layerSetting.terrain);
                    break;
                case LayerFillAlgorithm.RandomWalk:
                    map = LayerMapFunctions.RandomWalk2D(ref map, ref baseTerrainMap, ref heightMap, rand, layerSetting.terrain, layerSetting.radius, false, terrainTileLookup);
                    break;
                case LayerFillAlgorithm.Square:
                    map = LayerMapFunctions.RandomSquares(map, rand, layerSetting.terrain, layerSetting.radius);
                    break;
                case LayerFillAlgorithm.PerlinNoise:
                    map = LayerMapFunctions.PerlinNoise(ref map, ref baseTerrainMap, ref gradientMap, layerSetting.terrain, rand, layerSetting.PerlinNoiseScale, layerSetting.PerlinNoiseThreshold, layerSetting.MaxGradient, terrainTileLookup);
                    break;
                case LayerFillAlgorithm.RandomWalkBlocking:
                    map = LayerMapFunctions.RandomWalk2D(ref map, ref baseTerrainMap, ref heightMap, rand, layerSetting.terrain, layerSetting.radius, true, terrainTileLookup);
                    break;
                case LayerFillAlgorithm.HeightRange:
                    LayerMapFunctions.FillHeightRange(ref map, ref heightMap, layerSetting.terrain, layerSetting.MinHeight, layerSetting.MaxHeight);
                    break;
                case LayerFillAlgorithm.FollowGradient:
                    LayerMapFunctions.GadientDescent(ref map, ref heightMap, ref gradientMap, rand, layerSetting.terrain, layerSetting.MinStartHeight, layerSetting.MinStopHeight, layerSetting.MaxWidth, layerSetting.WidthChangeThrotle);
                    break;
                case LayerFillAlgorithm.FollowAlongGradient:
                    LayerMapFunctions.FollowAlongGradient(ref map, ref heightMap, ref gradientMap, rand, layerSetting.terrain, layerSetting.Width);
                    break;
                case LayerFillAlgorithm.AdjacentTiles:
                    LayerMapFunctions.AjdacentTiles(ref map, ref heightMap, ref gradientMap, ref baseTerrainMap, ref baseImprovementMap,
                        rand,
                        layerSetting.terrain, layerSetting.MinThreshold, layerSetting.MaxGradient, layerSetting.radius, layerSetting.SpawnChance, terrainTileLookup);
                    break;
                case LayerFillAlgorithm.Droplets:
                    LayerMapFunctions.Droplets(ref map, ref heightMap, ref gradientMap, rand, layerSetting.terrain, layerSetting.PercentCovered);
                    break;
            }
        }
    }

    private int GetLayerIndexByHeight(float height, int numZLayers)
    {
        int z = (int)(height * numZLayers);
        //if the z is exactly numzlayers it will cause at out of bound on out bounds on the layers list
        if (z == numZLayers)
        {
            z--;
        }
        return z;
    }

    private void LayerHeightMap(int numZLayers)
    {
        for (int x = 0; x <= heightMap.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= heightMap.GetUpperBound(1); y++)
            {
                heightMap[x, y] = GetLayerIndexByHeight(heightMap[x, y], numZLayers) / (float) numZLayers;
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

    public void ClearMap()
    {
        terrainMap = new Terrain[mapSize, mapSize];
    }
}
