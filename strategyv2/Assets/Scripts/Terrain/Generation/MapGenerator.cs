using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    public int mapSize;

    public bool UseErosion = true;
    public List<MapLayerSettings> LayerSettings = new List<MapLayerSettings>();

    [SerializeField]
    public Terrain[,] terrainMap;
    [SerializeField]
    public Improvement[,] improvmentMap;
    public float[,] heightMap;

    public Vector2[,] LayeredGradientMap;
    public TerrainGenerator HeightmapGen;

    public bool LoadFromFile = false;

    /// <summary>
    /// Generates  terrain and improvment maps based on provided layers
    /// </summary>
    /// <param name="terrainTileLookup"></param>
    /// <param name="improvementTileLookup"></param>
    /// <param name="numZLayers"> number of z layers to produce, height map is flattened to z layers so more z layers will make the mape more hilly, also much slower to render</param>
    public void GenerateMap(Dictionary<Terrain, TerrainMapTile> terrainTileLookup,
        Dictionary<Improvement, ImprovementMapTile> improvementTileLookup, int numZLayers)
    {
        ClearMap();
        if(LoadFromFile)
        {
            LoadMap();
            return;
        }

        terrainMap = new Terrain[mapSize, mapSize];
        improvmentMap = new Improvement[mapSize, mapSize];
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
            terrainMap[x, y] = Terrain.Grass;
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

            if (layerSetting.MapTile.Layer == MapLayer.Terrain)
            {
                RunAlgorithmGeneric(ref terrainMap, layerSetting.terrain, ref terrainMap, ref improvmentMap, 
                    ref gradientMapInUse, ref rand, terrainTileLookup, improvementTileLookup, layerSetting);
            }
            else
            {
                RunAlgorithmGeneric(ref improvmentMap, layerSetting.Improvement, ref terrainMap, ref improvmentMap,
                    ref gradientMapInUse, ref rand, terrainTileLookup, improvementTileLookup, layerSetting);
            }

            //recalculate gradients becasue they might have changed
            LayeredGradientMap = CalculateGradients(heightMap);
        }

        FixImprovmentsOnWater();

        //SmoothHeightMap();
        //gradientMap = CalculateGradients(heightMap);
        SaveMap();
    }

    public void RunAlgorithmGeneric<T>(
        ref T[,] currentMap,
        T currentTileValue,
        ref Terrain[,] terrainMap,
        ref Improvement[,] improvementMap,
        ref Vector2[,] gradientMap,
        ref System.Random rand,
        Dictionary<Terrain, TerrainMapTile> terrainTileLookup,
        Dictionary<Improvement, ImprovementMapTile> improvementTileLookup,
        MapLayerSettings layerSetting)
    {
        for (int i = 0; i < layerSetting.iterations; i++)
        {
            switch (layerSetting.algorithm)
            {
                case LayerFillAlgorithm.Solid:
                    currentMap = LayerMapFunctions.GenerateArray(mapSize, mapSize, currentTileValue);
                    break;
                case LayerFillAlgorithm.RandomWalk:
                    currentMap = LayerMapFunctions.RandomWalk2D(ref currentMap, ref terrainMap, ref heightMap, rand, currentTileValue,
                        layerSetting.radius, false, terrainTileLookup);
                    break;
                case LayerFillAlgorithm.Square:
                    currentMap = LayerMapFunctions.RandomSquares(ref currentMap, rand, currentTileValue, layerSetting.radius);
                    break;
                case LayerFillAlgorithm.PerlinNoise:
                    currentMap = LayerMapFunctions.PerlinNoise(ref currentMap, ref terrainMap, ref gradientMap, currentTileValue, rand, layerSetting.PerlinNoiseScale, layerSetting.PerlinNoiseThreshold, layerSetting.MaxGradient, terrainTileLookup);
                    break;
                case LayerFillAlgorithm.RandomWalkBlocking:
                    currentMap = LayerMapFunctions.RandomWalk2D(ref currentMap, ref terrainMap, ref heightMap, rand,
                        currentTileValue, layerSetting.radius, true, terrainTileLookup);
                    break;
                case LayerFillAlgorithm.HeightRange:
                    LayerMapFunctions.FillHeightRange(ref currentMap, ref heightMap, currentTileValue,
                        layerSetting.MinHeight, layerSetting.MaxHeight);
                    break;
                case LayerFillAlgorithm.FollowGradient:
                    LayerMapFunctions.GadientDescent(ref currentMap, ref heightMap, ref gradientMap, rand, currentTileValue,
                        layerSetting.MinStartHeight, layerSetting.MinStopHeight, layerSetting.MaxWidth,
                        layerSetting.WidthChangeThrotle);
                    break;
                case LayerFillAlgorithm.FollowAlongGradient:
                    LayerMapFunctions.FollowAlongGradient(ref currentMap, ref heightMap, ref gradientMap, rand,
                        currentTileValue, layerSetting.Width);
                    break;
                case LayerFillAlgorithm.AdjacentTiles:
                    LayerMapFunctions.AjdacentTiles(ref currentMap, ref heightMap, ref gradientMap, ref terrainMap, ref improvementMap,
                        rand, currentTileValue, layerSetting.MinThreshold, layerSetting.MaxGradient,
                        layerSetting.radius, layerSetting.SpawnChance, terrainTileLookup, improvementTileLookup);
                    break;
                case LayerFillAlgorithm.Droplets:
                    LayerMapFunctions.Droplets(ref currentMap, ref heightMap, ref gradientMap, rand, currentTileValue, layerSetting.PercentCovered);
                    break;
            }

        }
    }

    /// <summary>
    /// Check if we made an improvment on top of water, if so fix it to be land
    /// Might want to later change this for roads so that they look like briges instead, but idk
    /// </summary>
    private void FixImprovmentsOnWater()
    {
        for (int x = 0; x <= improvmentMap.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= improvmentMap.GetUpperBound(1); y++)
            {
                if (terrainMap[x, y] == Terrain.Water && improvmentMap[x,y] != Improvement.Empty)
                {
                    terrainMap[x, y] = Terrain.Grass;
                }
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

    private void SaveMap()
    {
        var terrainMapJson = JsonConvert.SerializeObject(terrainMap);
        var sr = File.CreateText("Assets/Saves/TerrainMap.txt");
        sr.WriteLine(terrainMapJson);
        sr.Close();

        var improvementMapJson = JsonConvert.SerializeObject(improvmentMap);
        sr = File.CreateText("Assets/Saves/ImprovementMap.txt");
        sr.WriteLine(improvementMapJson);
        sr.Close();

        var heightMapJson = JsonConvert.SerializeObject(heightMap);
        sr = File.CreateText("Assets/Saves/HeightMap.txt");
        sr.WriteLine(heightMapJson);
        sr.Close();
    }

    private void LoadMap()
    {
        var terrainMapJson = JsonConvert.SerializeObject(terrainMap);
        var sr = File.ReadAllText("Assets/Saves/TerrainMap.txt");
        terrainMap = JsonConvert.DeserializeObject<Terrain[,]>(sr);

        sr = File.ReadAllText("Assets/Saves/ImprovementMap.txt");
        improvmentMap = JsonConvert.DeserializeObject<Improvement[,]>(sr);

        sr = File.ReadAllText("Assets/Saves/HeightMap.txt");
        heightMap = JsonConvert.DeserializeObject<float[,]>(sr);

        LayeredGradientMap = CalculateGradients(heightMap);
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
