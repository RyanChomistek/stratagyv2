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
    public List<MapLayerSettings> LayerSettings = new List<MapLayerSettings>();

    [SerializeField]
    public MapData m_MapData;

    public int MapSize { get { return m_MapData.mapSize; } set { m_MapData.mapSize = value; } }
    public int HeightMapSize { get { return m_MapData.MeshHeightMapSize; } }
    public Terrain[,] terrainMap { get  { return m_MapData.TerrainMap; } }
    public Improvement[,] improvmentMap { get { return m_MapData.ImprovmentMap; } }
    public float[,] HeightMap { get { return m_MapData.HeightMap; } }
    public float[,] WaterMap { get { return m_MapData.WaterMap; } }
    public Vector2[,] GradientMap { get { return m_MapData.GradientMap; } }
    public Vector2[,] LayeredGradientMap { get { return m_MapData.LayeredGradientMap; } }

    public TerrainGenerator HeightmapGen;

    public bool LoadFromFile = false;

    public void InitializeMaps()
    {
        ClearMap();
        if (LoadFromFile)
        {
            LayerMapFunctions.LogAction(() => LoadMap(), "Load Map Time");
            return;
        }

        m_MapData.Clear();

        m_MapData.TerrainMap = new Terrain[MapSize, MapSize];
        m_MapData.ImprovmentMap = new Improvement[MapSize, MapSize];
        m_MapData.HeightMap = new float[MapSize, MapSize];

        // Mesh height is bigger so that we have data for the outside edge
        m_MapData.VertexHeightMap = new float[m_MapData.MeshHeightMapSize, m_MapData.MeshHeightMapSize];
        m_MapData.WaterMap = new float[MapSize, MapSize];
        m_MapData.LandComponents = null;
    }

    public void GenerateHeightMaps(ErosionOptions erosionOptions)
    {
        if (LoadFromFile)
        {
            return;
        }

        LayerMapFunctions.LogAction(() => HeightmapGen.GenerateHeightMap(m_MapData.MeshHeightMapSize), "Base Height Map Time");
        if (erosionOptions.enabled)
        {
            LayerMapFunctions.LogAction(() => HeightmapGen.Erode(m_MapData.MeshHeightMapSize, erosionOptions), "Erosion time");
        }
    }

    /// <summary>
    /// Generates  terrain and improvment maps based on provided layers
    /// </summary>
    /// <param name="terrainTileLookup"></param>
    /// <param name="improvementTileLookup"></param>
    /// <param name="numZLayers"> number of z layers to produce, height map is flattened to z layers so more z layers will make the mape more hilly, also much slower to render</param>
    public void GenerateTerrainAndImprovements(Dictionary<Terrain, TerrainMapTile> terrainTileLookup,
        Dictionary<Improvement, ImprovementMapTile> improvementTileLookup, int numZLayers)
    {
        if (LoadFromFile)
        {
            return;
        }

        float seed;
        seed = System.DateTime.Now.Millisecond;
        System.Random rand = new System.Random((int)seed);

        HeightmapGen.ConvertMapsTo2D(m_MapData);

        for (int x = 0; x < MapSize; x++)
        {
            for (int y = 0; y < MapSize; y++)
            {
                m_MapData.TerrainMap[x, y] = Terrain.Grass;
            }
        }

        m_MapData.GradientMap = null;
        LayerMapFunctions.LogAction(() =>
        {
            //LayerMapFunctions.Smooth(ref heightMap);
            m_MapData.GradientMap = (Vector2[,]) CalculateGradients(m_MapData.HeightMap).Clone();
            LayerHeightMap(numZLayers);
            m_MapData.LayeredGradientMap = CalculateGradients(m_MapData.HeightMap);
        }, "other gen times");

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
                RunAlgorithmGeneric(ref m_MapData.TerrainMap, layerSetting.terrain, ref m_MapData.TerrainMap, ref m_MapData.ImprovmentMap, 
                    ref gradientMapInUse, ref m_MapData.WaterMap, ref rand, terrainTileLookup, improvementTileLookup, layerSetting);
            }
            else
            {
                RunAlgorithmGeneric(ref m_MapData.ImprovmentMap, layerSetting.Improvement, ref m_MapData.TerrainMap, ref m_MapData.ImprovmentMap,
                    ref gradientMapInUse, ref m_MapData.WaterMap, ref rand, terrainTileLookup, improvementTileLookup, layerSetting);
            }

            //recalculate gradients becasue they might have changed
            m_MapData.LayeredGradientMap = CalculateGradients(m_MapData.HeightMap);
        }
        
        FixImprovmentsOnWater();
        LayerMapFunctions.LogAction(() => SaveMap(), "Save time");
        
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
                    currentMap = LayerMapFunctions.GenerateArray(MapSize, MapSize, currentTileValue);
                    break;
                case LayerFillAlgorithm.RandomWalk:
                    currentMap = RoadGenerator.GenerateRoad(ref currentMap, m_MapData, rand, currentTileValue,
                        layerSetting.radius, false, terrainTileLookup);
                    break;
                case LayerFillAlgorithm.Square:
                    currentMap = LayerMapFunctions.RandomSquares(ref currentMap, rand, currentTileValue, layerSetting.radius);
                    break;
                case LayerFillAlgorithm.PerlinNoise:
                    currentMap = LayerMapFunctions.PerlinNoise(ref currentMap, ref terrainMap, ref gradientMap, currentTileValue, rand, layerSetting.PerlinNoiseScale, layerSetting.PerlinNoiseThreshold, layerSetting.MaxGradient, terrainTileLookup);
                    break;
                case LayerFillAlgorithm.RandomWalkBlocking:
                    //currentMap = RoadGenerator.RandomWalk2D(ref currentMap, ref terrainMap, ref m_MapData.HeightMap, rand,
                    //    currentTileValue, layerSetting.radius, true, terrainTileLookup);
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
                    LakeGenerator.Lake(ref currentMap, m_MapData, ref m_MapData.HeightMap, ref LakeMap, ref gradientMap, ref terrainMap, currentTileValue, layerSetting);
                    break;
                case LayerFillAlgorithm.Mountain:
                    MountainGenerator.Generate(m_MapData, layerSetting);
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
        for (int x = 0; x <= m_MapData.ImprovmentMap.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= m_MapData.ImprovmentMap.GetUpperBound(1); y++)
            {
                if (m_MapData.TerrainMap[x, y] == Terrain.Water && m_MapData.ImprovmentMap[x,y] != Improvement.Empty)
                {
                    m_MapData.TerrainMap[x, y] = Terrain.Grass;
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
        MapData clone = m_MapData.Clone();
        ThreadPool.QueueUserWorkItem(delegate (object state)
        {
            try
            {
                clone.SaveMapData("MapDataJson.MapData");
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
        m_MapData = MapData.LoadMapData("MapDataJson.MapData");
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

    public static Vector2[,] CalculateGradients(float[,] arr)
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
        m_MapData.TerrainMap = new Terrain[MapSize, MapSize];
    }
}
