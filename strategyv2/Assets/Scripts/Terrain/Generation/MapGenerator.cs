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
    [SerializeField]
    public MapData m_MapData;
    public bool LoadFromFile = false;
    public TerrainGeneratorGraph TerrainGraph;

    public SquareArray<Terrain> terrainMap { get { return m_MapData.TerrainMap; } }
    public SquareArray<Improvement> improvmentMap { get { return m_MapData.ImprovmentMap; } }
    public SquareArray<float> HeightMap { get { return m_MapData.HeightMap; } }
    public SquareArray<float> WaterMap { get { return m_MapData.WaterMap; } }
    public SquareArray<Vector2> GradientMap { get { return m_MapData.GradientMap; } }
    public SquareArray<Vector2> LayeredGradientMap { get { return m_MapData.LayeredGradientMap; } }

    public void GenerateMaps()
    {
        if (LoadFromFile)
        {
            ProfilingUtilities.LogAction(() => LoadMap(), "Load Map Time");
            return;
        }

        m_MapData.Clear();


        TerrainGraph.RandomizeSeed = true;
        m_MapData = TerrainGraph.RecalculateFullGraphAndGetMapData();

        //SaveMap();
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
}
