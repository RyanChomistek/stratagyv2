using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMapGenerationData
{
    public SquareArray<float> HeightMap { get; set; }
    public SquareArray<float> WaterMap { get; set; }

    public int MapSize;

    public HeightMapGenerationData(int mapSize)
    {
        this.MapSize = mapSize;
        HeightMap = new SquareArray<float>(mapSize);
        WaterMap = new SquareArray<float>(mapSize);
    }
}

public interface HeightMapLayerBase 
{
     void Apply(HeightMapGenerationData HMData);
}
