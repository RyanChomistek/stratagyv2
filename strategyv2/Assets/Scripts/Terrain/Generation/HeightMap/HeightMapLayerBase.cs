using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMapGenerationData
{
    public float[,] HeightMap { get; set; }
    public float[,] WaterMap { get; set; }

    public int MapSize;

    public HeightMapGenerationData(int mapSize)
    {
        this.MapSize = mapSize;
        HeightMap = new float[mapSize, mapSize];
        WaterMap = new float[mapSize, mapSize];
    }
}

public interface HeightMapLayerBase 
{
     void Apply(HeightMapGenerationData HMData);
}
