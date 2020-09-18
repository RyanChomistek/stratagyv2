using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class RandomNoiseSettings
{
    public System.Random Rand;
    public int MaxNumThreads = 65535;
    public int numOctaves = 7;
    public float persistence = .5f;
    public float lacunarity = 2;
    public float initialScale = 2;
    public ComputeShader heightMapComputeShader;
    public bool GenerationEnabled = true;
    public int MapSize = 128;
}

[CreateNodeMenu("TerrainNodes/RandomNoiseNode")]
public class RandomNoiseNode : TerrainNode
{
    [Input] public int MaxNumThreads = 65535;
    [Input] public int numOctaves = 7;
    [Input] public float persistence = .5f;
    [Input] public float lacunarity = 2;
    [Input] public float initialScale = 2;
    [Input] public ComputeShader heightMapComputeShader;
    [Input] public bool GenerationEnabled = true;

    [Input] public float[] BaseHeightMap = null;
    [Output] public float[] HeightMap = null;
    public int HeightMapSize = 128 * 128;
    public override object GetValue(XNode.NodePort port)
    {
        return HeightMap;
    }

    public override void Recalculate()
    {
        int MaxNumThreads = GetInputValue<int>("MaxNumThreads", this.MaxNumThreads);
        int numOctaves = GetInputValue<int>("numOctaves", this.numOctaves);
        float persistence = GetInputValue<float>("persistence", this.persistence);
        float lacunarity = GetInputValue<float>("lacunarity", this.lacunarity);
        float initialScale = GetInputValue<float>("initialScale", this.initialScale);
        ComputeShader heightMapComputeShader = GetInputValue("heightMapComputeShader", this.heightMapComputeShader);
        bool GenerationEnabled = GetInputValue<bool>("GenerationEnabled", this.GenerationEnabled);
        float[] BaseHeightMap = GetInputValue<float[]>("BaseHeightMap", this.BaseHeightMap);

        // Check if this is a valid input
        if (BaseHeightMap != null && BaseHeightMap.Length > 0)
        {
            //Recalculate the graph
            RandomNoiseSettings rns = new RandomNoiseSettings()
            {
                MaxNumThreads = MaxNumThreads,
                numOctaves = numOctaves,
                persistence = persistence,
                lacunarity = lacunarity,
                initialScale = initialScale,
                heightMapComputeShader = heightMapComputeShader,
                GenerationEnabled = GenerationEnabled,
                Rand = (graph as TerrainGeneratorGraph).Rand
            };

            HeightMap = Run(rns, BaseHeightMap);
            HeightMapSize = HeightMap.Length;
        }
    }

    public static float[] Run(RandomNoiseSettings Settings, float[] BaseHeightMap)
    {
        float[] HeightMap = (float[])BaseHeightMap.Clone();
        GenerateHeightMapGPU(Settings, (int)Mathf.Sqrt(BaseHeightMap.Length), HeightMap);
        return HeightMap;
    }

    private static void GenerateHeightMapGPU(RandomNoiseSettings Settings, int mapSize, float[] heightMap)
    {
        if (!Settings.GenerationEnabled)
        {
            return;
        }

        Vector2[] offsets = new Vector2[Settings.numOctaves];
        for (int i = 0; i < Settings.numOctaves; i++)
        {
            offsets[i] = new Vector2(Settings.Rand.Next(-10000, 10000), Settings.Rand.Next(-10000, 10000));
        }
        ComputeBuffer offsetsBuffer = new ComputeBuffer(offsets.Length, sizeof(float) * 2);
        offsetsBuffer.SetData(offsets);
        Settings.heightMapComputeShader.SetBuffer(0, "offsets", offsetsBuffer);

        int floatToIntMultiplier = 1000;

        ComputeBuffer mapBuffer = new ComputeBuffer(heightMap.Length, sizeof(int));
        mapBuffer.SetData(heightMap);
        Settings.heightMapComputeShader.SetBuffer(0, "heightMap", mapBuffer);

        int[] minMaxHeight = { floatToIntMultiplier * Settings.numOctaves, 0 };
        ComputeBuffer minMaxBuffer = new ComputeBuffer(minMaxHeight.Length, sizeof(int));
        minMaxBuffer.SetData(minMaxHeight);
        Settings.heightMapComputeShader.SetBuffer(0, "minMax", minMaxBuffer);

        int numThreads = System.Math.Min(heightMap.Length, Settings.MaxNumThreads);
        int numElementsToProcess = Mathf.CeilToInt(heightMap.Length / (float)numThreads);
        //Debug.Log($"HeightMapGen: num elements = {heightMap.Length}, num GPU Threads = {numThreads}, each doing {numElementsToProcess} elements");

        Settings.heightMapComputeShader.SetInt("mapSize", mapSize);
        Settings.heightMapComputeShader.SetInt("numThreads", numThreads);
        Settings.heightMapComputeShader.SetInt("numElementsToProcess", numElementsToProcess);

        Settings.heightMapComputeShader.SetInt("octaves", Settings.numOctaves);
        Settings.heightMapComputeShader.SetFloat("lacunarity", Settings.lacunarity);
        Settings.heightMapComputeShader.SetFloat("persistence", Settings.persistence);
        Settings.heightMapComputeShader.SetFloat("scaleFactor", Settings.initialScale);
        Settings.heightMapComputeShader.SetInt("floatToIntMultiplier", floatToIntMultiplier);
        Settings.heightMapComputeShader.Dispatch(0, numThreads, 1, 1);

        mapBuffer.GetData(heightMap);
        minMaxBuffer.GetData(minMaxHeight);

        mapBuffer.Release();
        minMaxBuffer.Release();
        offsetsBuffer.Release();

        float minValue = (float)minMaxHeight[0] / (float)floatToIntMultiplier;
        float maxValue = (float)minMaxHeight[1] / (float)floatToIntMultiplier;

        for (int i = 0; i < heightMap.Length; i++)
        {
            heightMap[i] = Mathf.InverseLerp(minValue, maxValue, heightMap[i]);
        }
    }

    public override void Flush()
    {
        BaseHeightMap = null;
        HeightMap = null;
    }
}