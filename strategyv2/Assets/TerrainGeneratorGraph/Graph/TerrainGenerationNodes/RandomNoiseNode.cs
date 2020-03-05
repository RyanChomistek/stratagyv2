using HeightMapGeneration;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class RandomNoiseNode : TerrainNode
{
    [Input] public int MaxNumThreads = 65535;
    [Input] public int numOctaves = 7;
    [Input] public float persistence = .5f;
    [Input] public float lacunarity = 2;
    [Input] public float initialScale = 2;
    [Input] public ComputeShader heightMapComputeShader;
    [Input] public bool GenerationEnabled = true;

    [Input] public float[] BaseHeightMap = new float[128 * 128];
    [Output] public float[] HeightMap;
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
                seed = this.seed,
                randomizeSeed = this.randomizeSeed,
                MaxNumThreads = MaxNumThreads,
                numOctaves = numOctaves,
                persistence = persistence,
                lacunarity = lacunarity,
                initialScale = initialScale,
                heightMapComputeShader = heightMapComputeShader,
                GenerationEnabled = GenerationEnabled,
            };

            HeightMap = RandomNoiseHeightMapLayer.Run(rns, BaseHeightMap);
            HeightMapSize = HeightMap.Length;

            GenerateVisualization(HeightMap, (val) => {
                return Color.Lerp(Color.green, Color.red, val);
            });

            // Only recalc if we are successful
            base.Recalculate();
        }
    }
}