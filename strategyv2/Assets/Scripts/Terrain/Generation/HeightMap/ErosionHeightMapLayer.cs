using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HeightMapGeneration
{
    public class ErosionSettings
    {
        [Header("Erosion Settings")]
        public bool enabled = true;
        public ComputeShader erosion;
        public int MaxNumThreads = 65535;
        public int numDropletsPerCell = 8;
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
        public float[] WaterMap;
    }

    public class ErosionHeightMapLayer
    {
        public static ErosionOutput Run(ErosionSettings settings, float[] InputHeightMap, float[] InputWaterMap)
        {
            float[] HeightMap = (float[])InputHeightMap.Clone();
            float[] WaterMap = (float[])InputWaterMap.Clone();
            
            Erode(settings, (int)Mathf.Sqrt(HeightMap.Length), HeightMap, WaterMap);

            return new ErosionOutput() {
                HeightMap = HeightMap,
                WaterMap = WaterMap
            };
        }

        private static void Erode(ErosionSettings Settings, int mapSize, float[] HeightMap, float[] WaterMap)
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
            int numElementsToProcess = Mathf.CeilToInt(HeightMap.Length / (float)numThreads);
            //Debug.Log($"Erosion: num elements = {HeightMap.Length}, num GPU Threads = {numThreads}, each doing {numElementsToProcess} elements");

            // HeightMap buffer
            ComputeBuffer mapBuffer = new ComputeBuffer(HeightMap.Length, sizeof(float));
            mapBuffer.SetData(HeightMap);
            Settings.erosion.SetBuffer(0, "map", mapBuffer);

            // WaterMap buffer
            ComputeBuffer waterMapBuffer = new ComputeBuffer(WaterMap.Length, sizeof(float));
            waterMapBuffer.SetData(WaterMap);
            Settings.erosion.SetBuffer(0, "waterMap", waterMapBuffer);

            // Settings
            Settings.erosion.SetInt("borderSize", Settings.ErosionBrushRadius);
            Settings.erosion.SetInt("mapSize", mapSize);

            Settings.erosion.SetInt("numThreads", numThreads);
            Settings.erosion.SetInt("numElementsToProcess", numElementsToProcess);

            Settings.erosion.SetInt("numDropletsPerCell", Settings.numDropletsPerCell);

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

            // Run compute shader
            Settings.erosion.Dispatch(0, numThreads, 1, 1);
            mapBuffer.GetData(HeightMap);
            waterMapBuffer.GetData(WaterMap);

            // Release buffers
            mapBuffer.Release();
            waterMapBuffer.Release();
            brushIndexBuffer.Release();
            brushWeightBuffer.Release();
        }
    }
}
