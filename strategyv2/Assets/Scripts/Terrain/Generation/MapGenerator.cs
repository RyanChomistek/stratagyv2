using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    public int mapSize;

    public List<MapLayerSettings> LayerSettings = new List<MapLayerSettings>();

    private MapData m_MapData;

    public Terrain[,] terrainMap { get  { return m_MapData.terrainMap; } }
    public Improvement[,] improvmentMap { get { return m_MapData.improvmentMap; } }
    public float[,] RawHeightMap { get { return m_MapData.RawHeightMap; } }
    public float[,] HeightMap { get { return m_MapData.HeightMap; } }
    public float[,] WaterMap { get { return m_MapData.WaterMap; } }
    public Vector2[,] GradientMap { get { return m_MapData.GradientMap; } }
    public Vector2[,] LayeredGradientMap { get { return m_MapData.LayeredGradientMap; } }

    public TerrainGenerator HeightmapGen;

    public bool LoadFromFile = false;

    

    /// <summary>
    /// Generates  terrain and improvment maps based on provided layers
    /// </summary>
    /// <param name="terrainTileLookup"></param>
    /// <param name="improvementTileLookup"></param>
    /// <param name="numZLayers"> number of z layers to produce, height map is flattened to z layers so more z layers will make the mape more hilly, also much slower to render</param>
    public void GenerateMap(Dictionary<Terrain, TerrainMapTile> terrainTileLookup,
        Dictionary<Improvement, ImprovementMapTile> improvementTileLookup, int numZLayers, ErosionOptions erosionOptions)
    {
        ClearMap();
        if(LoadFromFile)
        {
            LayerMapFunctions.LogAction(() => LoadMap(), "Load Map Time");
            return;
        }

        m_MapData.terrainMap = new Terrain[mapSize, mapSize];
        m_MapData.improvmentMap = new Improvement[mapSize, mapSize];
        m_MapData.HeightMap = new float[mapSize, mapSize];
        m_MapData.WaterMap = new float[mapSize, mapSize];

        float seed;
        seed = System.DateTime.Now.Millisecond;
        System.Random rand = new System.Random((int)seed);

        //generate heightmap
        LayerMapFunctions.LogAction(() => HeightmapGen.GenerateHeightMap(mapSize), "Base Height Map Time");
        if (erosionOptions.enabled)
        {
            LayerMapFunctions.LogAction(() => HeightmapGen.Erode(mapSize, erosionOptions), "Erosion time");
        }

        for (int i = 0; i < mapSize * mapSize; i++)
        {
            int x = i % mapSize;
            int y = i / mapSize;
            int index = y * mapSize + x;
            var erosionBrushRadius = HeightmapGen.erosionBrushRadius;
            var mapSizeWithBorder = HeightmapGen.mapSizeWithBorder;
            int borderedMapIndex = (y + erosionBrushRadius) * mapSizeWithBorder + x + erosionBrushRadius;
            m_MapData.HeightMap[x, y] = HeightmapGen.HeightMap[borderedMapIndex];
            m_MapData.terrainMap[x, y] = Terrain.Grass;
            m_MapData.WaterMap[x, y] = HeightmapGen.LakeMap[borderedMapIndex];
        }

        m_MapData.RawHeightMap = m_MapData.HeightMap.Clone() as float[,];
        m_MapData.GradientMap = null;
        LayerMapFunctions.LogAction(() =>
        {
            //LayerMapFunctions.Smooth(ref heightMap);
            m_MapData.GradientMap = (Vector2[,]) CalculateGradients(m_MapData.HeightMap).Clone();
            LayerHeightMap(numZLayers);
            m_MapData.LayeredGradientMap = CalculateGradients(m_MapData.HeightMap);
        }
        , "other gen times");
        
        foreach (MapLayerSettings layerSetting in LayerSettings)
        {
            if (!layerSetting.IsEnabled)
            {
                continue;
            }

            Vector2[,] gradientMapInUse;

            if (layerSetting.useLayeredGradients)
            {
                gradientMapInUse = m_MapData.LayeredGradientMap;
            }
            else
            {
                gradientMapInUse = m_MapData.GradientMap;
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
                RunAlgorithmGeneric(ref m_MapData.terrainMap, layerSetting.terrain, ref m_MapData.terrainMap, ref m_MapData.improvmentMap, 
                    ref gradientMapInUse, ref m_MapData.WaterMap, ref rand, terrainTileLookup, improvementTileLookup, layerSetting);
            }
            else
            {
                RunAlgorithmGeneric(ref m_MapData.improvmentMap, layerSetting.Improvement, ref m_MapData.terrainMap, ref m_MapData.improvmentMap,
                    ref gradientMapInUse, ref m_MapData.WaterMap, ref rand, terrainTileLookup, improvementTileLookup, layerSetting);
            }

            //recalculate gradients becasue they might have changed
            m_MapData.LayeredGradientMap = CalculateGradients(m_MapData.HeightMap);
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
        ref float[,] LakeMap,
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
                    currentMap = LayerMapFunctions.RandomWalk2D(ref currentMap, ref terrainMap, ref m_MapData.HeightMap, rand, currentTileValue,
                        layerSetting.radius, false, terrainTileLookup);
                    break;
                case LayerFillAlgorithm.Square:
                    currentMap = LayerMapFunctions.RandomSquares(ref currentMap, rand, currentTileValue, layerSetting.radius);
                    break;
                case LayerFillAlgorithm.PerlinNoise:
                    currentMap = LayerMapFunctions.PerlinNoise(ref currentMap, ref terrainMap, ref gradientMap, currentTileValue, rand, layerSetting.PerlinNoiseScale, layerSetting.PerlinNoiseThreshold, layerSetting.MaxGradient, terrainTileLookup);
                    break;
                case LayerFillAlgorithm.RandomWalkBlocking:
                    currentMap = LayerMapFunctions.RandomWalk2D(ref currentMap, ref terrainMap, ref m_MapData.HeightMap, rand,
                        currentTileValue, layerSetting.radius, true, terrainTileLookup);
                    break;
                case LayerFillAlgorithm.HeightRange:
                    LayerMapFunctions.FillHeightRange(ref currentMap, ref m_MapData.HeightMap, currentTileValue,
                        layerSetting.MinHeight, layerSetting.MaxHeight);
                    break;
                case LayerFillAlgorithm.FollowGradient:
                    LayerMapFunctions.GadientDescent(ref currentMap, ref m_MapData.HeightMap, ref gradientMap, rand, currentTileValue,
                        layerSetting.MinStartHeight, layerSetting.MinStopHeight, layerSetting.MaxWidth,
                        layerSetting.WidthChangeThrotle);
                    break;
                case LayerFillAlgorithm.FollowAlongGradient:
                    LayerMapFunctions.FollowAlongGradient(ref currentMap, ref m_MapData.HeightMap, ref gradientMap, rand,
                        currentTileValue, layerSetting.Width);
                    break;
                case LayerFillAlgorithm.AdjacentTiles:
                    LayerMapFunctions.AjdacentTiles(ref currentMap, ref m_MapData.HeightMap, ref gradientMap, ref terrainMap, ref improvementMap,
                        rand, currentTileValue, layerSetting.MinThreshold, layerSetting.MaxGradient,
                        layerSetting.radius, layerSetting.SpawnChance, terrainTileLookup, improvementTileLookup);
                    break;
                case LayerFillAlgorithm.Lake:
                case LayerFillAlgorithm.River:
                    LakeGenerator.Lake(ref currentMap, ref m_MapData.HeightMap, ref LakeMap, ref gradientMap, ref terrainMap, currentTileValue, layerSetting);
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
        for (int x = 0; x <= m_MapData.improvmentMap.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= m_MapData.improvmentMap.GetUpperBound(1); y++)
            {
                if (m_MapData.terrainMap[x, y] == Terrain.Water && m_MapData.improvmentMap[x,y] != Improvement.Empty)
                {
                    m_MapData.terrainMap[x, y] = Terrain.Grass;
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
        ThreadPool.QueueUserWorkItem(delegate (object state)
        {
            try
            {
                var MapDataJson = JsonConvert.SerializeObject(m_MapData);
                var sr = File.CreateText("Assets/Saves/MapDataJson.MapData");
                sr.WriteLine(MapDataJson);
                sr.Close();
            }
            finally
            {
                Debug.Log("Done saving");
            }
        },
        null);
    }

    private void LoadMap()
    {
        var sr = File.ReadAllText("Assets/Saves/MapDataJson.MapData");
        m_MapData = JsonConvert.DeserializeObject<MapData>(sr);
    }

    private void LayerHeightMap(int numZLayers)
    {
        for (int x = 0; x <= m_MapData.HeightMap.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= m_MapData.HeightMap.GetUpperBound(1); y++)
            {
                m_MapData.HeightMap[x, y] = GetLayerIndexByHeight(m_MapData.HeightMap[x, y], numZLayers) / (float) numZLayers;
            }
        }
    }

    private Vector2[,] CalculateGradients(float[,] arr)
    {
        var gradientMap = new Vector2[arr.GetUpperBound(0) + 1, arr.GetUpperBound(1) + 1];
        //loop through every tile
        LayerMapFunctions.ParallelForFast(arr, (x, y) => {
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
                        var delta = arr[i, j] - arr[x, y];

                        Vector2 localGradient = dir * delta;
                        gradientMap[x, y] += localGradient;
                    }
                }
            }
        });

        return gradientMap;
    }

    public void ClearMap()
    {
        m_MapData.terrainMap = new Terrain[mapSize, mapSize];
    }
}
