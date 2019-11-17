﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class MapData
{
    [SerializeField]
    public int mapSize;
    [SerializeField]
    public Terrain[,] TerrainMap;
    [SerializeField]
    public Improvement[,] ImprovmentMap;
    [SerializeField]
    public float[,] HeightMap;
    [SerializeField]
    public float[,] WaterMap;

    [SerializeField]
    public Vector2[,] GradientMap;
    [SerializeField]
    public Vector2[,] LayeredGradientMap;

    public MapData Clone()
    {
        MapData other = new MapData
        {
            mapSize = mapSize,
            TerrainMap = TerrainMap.Clone() as Terrain[,],
            ImprovmentMap = ImprovmentMap.Clone() as Improvement[,],
            //RawHeightMap = RawHeightMap.Clone() as float[,],
            HeightMap = HeightMap.Clone() as float[,],
            WaterMap = WaterMap.Clone() as float[,],
            GradientMap = GradientMap.Clone() as Vector2[,],
            LayeredGradientMap = LayeredGradientMap.Clone() as Vector2[,]
        };

        return other;
    }

    public static MapData LoadMapData(string fileName)
    {
        var sr = File.ReadAllText($"Assets/Saves/{fileName}");
        return JsonConvert.DeserializeObject<MapData>(sr);
    }

    public void SaveMapData(string fileName)
    {
        var MapDataJson = JsonConvert.SerializeObject(this);
        var sr = File.CreateText($"Assets/Saves/{fileName}");
        sr.WriteLine(MapDataJson);
        sr.Close();
    }
}
