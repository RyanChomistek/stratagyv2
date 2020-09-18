using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using XNode;

public class ErosionSettings
{
    [Header("Erosion Settings")]
    public bool enabled = true;
    public ComputeShader erosion;
    public int MaxNumThreads = 65535;
    public float numDropletsPerCell = 8;
    public int maxLifetime = 30;
    public float sedimentCapacityFactor = 3;
    public float minSedimentCapacity = .01f;
    public float depositSpeed = 0.3f;
    public float erodeSpeed = 0.3f;
    public float evaporateSpeed = .01f;
    public float gravity = 4;
    public float startSpeed = 1;
    public float startWater = 1;
    [Range(0, 1)]
    public float inertia = 0.3f;
    public int ErosionBrushRadius = 3;
}

public class ErosionOutput
{
    public float[] HeightMap;
}

[CreateNodeMenu("Operators/Array/Erosion")]
public class ErosionNode : TerrainNode
{
    [Input] public ComputeShader erosion;
    [Input] public int MaxNumThreads = 65535;
    [Input] public float numDropletsPerCell = 8;
    [Input] public int maxLifetime = 30;
    [Input] public float sedimentCapacityFactor = 3;
    [Input] public float minSedimentCapacity = .01f;
    [Input] public float depositSpeed = 0.3f;
    [Input] public float erodeSpeed = 0.3f;
    [Input] public float evaporateSpeed = .01f;
    [Input] public float gravity = 4;
    [Input] public float startSpeed = 1;
    [Input] public float startWater = 1;
    [Input] public float inertia = 0.3f;

    [Input] public int ErosionBrushRadius = 3;

    [Input] public float[] InputHeightMap = null;

    [Output] public float[] HeightMap = null;
    public int HeightMapSize = 128 * 128;

    public override object GetValue(XNode.NodePort port)
    {
        if(port.fieldName == "HeightMap")
            return HeightMap;

        return null;
    }

    public override void Recalculate()
    {
        ComputeShader erosion = GetInputValue("erosion", this.erosion);
        int MaxNumThreads = GetInputValue("MaxNumThreads", this.MaxNumThreads);
        float numDropletsPerCell = GetInputValue("numDropletsPerCell", this.numDropletsPerCell);
        int maxLifetime = GetInputValue("maxLifetime", this.maxLifetime);
        float sedimentCapacityFactor = GetInputValue("sedimentCapacityFactor", this.sedimentCapacityFactor);
        float minSedimentCapacity = GetInputValue("minSedimentCapacity", this.minSedimentCapacity);
        float depositSpeed = GetInputValue("depositSpeed", this.depositSpeed);
        float erodeSpeed = GetInputValue("erodeSpeed", this.erodeSpeed);
        float evaporateSpeed = GetInputValue("evaporateSpeed", this.evaporateSpeed);
        float gravity = GetInputValue("gravity", this.gravity);
        float startSpeed = GetInputValue("startSpeed", this.startSpeed);
        float startWater = GetInputValue("startWater", this.startWater);
        float inertia = GetInputValue("inertia", this.inertia);

        float[] InputHeightMap = GetInputValue("InputHeightMap", this.InputHeightMap);

        if (InputHeightMap != null && InputHeightMap.Length > 0)
        {
            ErosionSettings ES = new ErosionSettings()
            {
                erosion = erosion,
                MaxNumThreads = MaxNumThreads,
                numDropletsPerCell = numDropletsPerCell,
                maxLifetime = maxLifetime,
                sedimentCapacityFactor = sedimentCapacityFactor,
                minSedimentCapacity = minSedimentCapacity,
                depositSpeed = depositSpeed,
                erodeSpeed = erodeSpeed,
                evaporateSpeed = evaporateSpeed,
                gravity = gravity,
                startSpeed = startSpeed,
                startWater = startWater,
                inertia = inertia,
                ErosionBrushRadius = ErosionBrushRadius,
            };

            ErosionOutput EO = Run(ES, InputHeightMap);
            HeightMap = EO.HeightMap;

            HeightMapSize = InputHeightMap.Length;
        }
    }

    public ErosionOutput Run(ErosionSettings settings, float[] InputHeightMap)
    {
        float[] HeightMap = (float[])InputHeightMap.Clone();

        ErodeGPU(settings, (int)Mathf.Sqrt(HeightMap.Length), HeightMap);

        return new ErosionOutput()
        {
            HeightMap = HeightMap
        };
    }

    private void ErodeGPU(ErosionSettings Settings, int mapSize, float[] HeightMap)
    {
        // Create brush
        List<int> brushIndexOffsets = new List<int>();
        List<float> brushWeights = new List<float>();

        float weightSum = 0;
        for (int brushY = -Settings.ErosionBrushRadius; brushY <= Settings.ErosionBrushRadius; brushY++)
        {
            for (int brushX = -Settings.ErosionBrushRadius; brushX <= Settings.ErosionBrushRadius; brushX++)
            {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst < Settings.ErosionBrushRadius * Settings.ErosionBrushRadius)
                {
                    brushIndexOffsets.Add(brushY * mapSize + brushX);
                    float brushWeight = 1 - Mathf.Sqrt(sqrDst) / Settings.ErosionBrushRadius;
                    weightSum += brushWeight;
                    brushWeights.Add(brushWeight);
                }
            }
        }
        for (int i = 0; i < brushWeights.Count; i++)
        {
            brushWeights[i] /= weightSum;
        }

        // Send brush data to compute shader
        ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int));
        ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(int));
        brushIndexBuffer.SetData(brushIndexOffsets);
        brushWeightBuffer.SetData(brushWeights);
        Settings.erosion.SetBuffer(0, "brushIndices", brushIndexBuffer);
        Settings.erosion.SetBuffer(0, "brushWeights", brushWeightBuffer);

        int numThreads = System.Math.Min(HeightMap.Length, Settings.MaxNumThreads);

        Debug.Log(numThreads);

        int numElementsToProcess = Mathf.CeilToInt(HeightMap.Length / (float)numThreads);
        int numDropletsPerThread = (int) (Settings.numDropletsPerCell * numElementsToProcess);
        //Debug.Log($"Erosion: num elements = {HeightMap.Length}, num GPU Threads = {numThreads}, each doing {numElementsToProcess} elements");

        // HeightMap buffer
        ComputeBuffer mapBuffer = new ComputeBuffer(HeightMap.Length, sizeof(float));
        mapBuffer.SetData(HeightMap);
        Settings.erosion.SetBuffer(0, "map", mapBuffer);

        // Random Seeds
        int[] seeds = new int[] 
        { 
            Graph.Rand.Next(0, 42949672), 
            Graph.Rand.Next(0, 4008679), 
            Graph.Rand.Next(0, 64035029), 
            Graph.Rand.Next(0, 24038167) 
        };

        // Settings
        Settings.erosion.SetInt("borderSize", Settings.ErosionBrushRadius);
        Settings.erosion.SetInt("mapSize", mapSize);
        Settings.erosion.SetInt("numDropletsPerThread", numDropletsPerThread);
        Settings.erosion.SetInt("brushLength", brushIndexOffsets.Count);
        Settings.erosion.SetInt("maxLifetime", Settings.maxLifetime);
        Settings.erosion.SetFloat("inertia", Settings.inertia);
        Settings.erosion.SetFloat("sedimentCapacityFactor", Settings.sedimentCapacityFactor);
        Settings.erosion.SetFloat("minSedimentCapacity", Settings.minSedimentCapacity);
        Settings.erosion.SetFloat("depositSpeed", Settings.depositSpeed);
        Settings.erosion.SetFloat("erodeSpeed", Settings.erodeSpeed);
        Settings.erosion.SetFloat("evaporateSpeed", Settings.evaporateSpeed);
        Settings.erosion.SetFloat("gravity", Settings.gravity);
        Settings.erosion.SetFloat("startSpeed", Settings.startSpeed);
        Settings.erosion.SetFloat("startWater", Settings.startWater);
        Settings.erosion.SetInts("seed", seeds);

        // Run compute shader
        ProfilingUtilities.LogAction(() =>
        {
            Settings.erosion.Dispatch(0, numThreads, 1, 1);
            mapBuffer.GetData(HeightMap);
        }, "run erosion");

        // Release buffers
        mapBuffer.Release();
        brushIndexBuffer.Release();
        brushWeightBuffer.Release();
    }

    public override void Flush()
    {
        InputHeightMap = null;

        HeightMap = null;
    }
}