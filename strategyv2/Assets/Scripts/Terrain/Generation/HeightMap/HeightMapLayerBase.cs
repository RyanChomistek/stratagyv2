using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMapGenerationData
{
    public float[] HeightMap { get; private set; }
    public float[] WaterMap { get; private set; }
}

public abstract class HeightMapLayerBase 
{
    public abstract void Apply(HeightMapGenerationData HMData);
}
