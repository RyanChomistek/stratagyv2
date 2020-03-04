using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HeightMapGeneration
{
    public class ErosionHeightMapLayer : HeightMapLayerBase
    {
        ErosionLayerSettings Settings;

        // these are 1d arrays for use in the shader
        private float[] HeightMap;
        private float[] WaterMap;

        public int ErosionBrushRadius = 3;

        public ErosionHeightMapLayer(ErosionLayerSettings settings, int erosionBrushRadius)
        {
            this.Settings = settings;
            this.ErosionBrushRadius = erosionBrushRadius;
        }

        public void Apply(HeightMapGenerationData HMData)
        {
            HeightMap = TerrainGenerator.Convert2DMapTo1D(HMData.MapSize, HMData.HeightMap);
            WaterMap = TerrainGenerator.Convert2DMapTo1D(HMData.MapSize, HMData.WaterMap);

            Erode(HMData);

            HMData.HeightMap = TerrainGenerator.Convert1DMapTo2D(HMData.MapSize, HeightMap);
            HMData.WaterMap = TerrainGenerator.Convert1DMapTo2D(HMData.MapSize, WaterMap);
        }

        public void Erode(HeightMapGenerationData HMData)
        {
            // Create brush
            List<int> brushIndexOffsets = new List<int>();
            List<float> brushWeights = new List<float>();

            float weightSum = 0;
            for (int brushY = -ErosionBrushRadius; brushY <= ErosionBrushRadius; brushY++)
            {
                for (int brushX = -ErosionBrushRadius; brushX <= ErosionBrushRadius; brushX++)
                {
                    float sqrDst = brushX * brushX + brushY * brushY;
                    if (sqrDst < ErosionBrushRadius * ErosionBrushRadius)
                    {
                        brushIndexOffsets.Add(brushY * HMData.MapSize + brushX);
                        float brushWeight = 1 - Mathf.Sqrt(sqrDst) / ErosionBrushRadius;
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
            this.Settings.erosion.SetBuffer(0, "brushIndices", brushIndexBuffer);
            this.Settings.erosion.SetBuffer(0, "brushWeights", brushWeightBuffer);

            int numThreads = System.Math.Min(HeightMap.Length, this.Settings.MaxNumThreads);
            int numElementsToProcess = Mathf.CeilToInt(HeightMap.Length / (float)numThreads);
            Debug.Log($"Erosion: num elements = {HeightMap.Length}, num GPU Threads = {numThreads}, each doing {numElementsToProcess} elements");

            // Heightmap buffer
            ComputeBuffer mapBuffer = new ComputeBuffer(HeightMap.Length, sizeof(float));
            mapBuffer.SetData(HeightMap);
            this.Settings.erosion.SetBuffer(0, "map", mapBuffer);

            // WaterMap buffer
            ComputeBuffer waterMapBuffer = new ComputeBuffer(WaterMap.Length, sizeof(float));
            waterMapBuffer.SetData(WaterMap);
            this.Settings.erosion.SetBuffer(0, "waterMap", waterMapBuffer);

            // Settings
            this.Settings.erosion.SetInt("borderSize", ErosionBrushRadius);
            this.Settings.erosion.SetInt("mapSize", HMData.MapSize);

            this.Settings.erosion.SetInt("numThreads", numThreads);
            this.Settings.erosion.SetInt("numElementsToProcess", numElementsToProcess);

            this.Settings.erosion.SetInt("numDropletsPerCell", Settings.numDropletsPerCell);

            this.Settings.erosion.SetInt("brushLength", brushIndexOffsets.Count);
            this.Settings.erosion.SetInt("maxLifetime", this.Settings.maxLifetime);
            this.Settings.erosion.SetFloat("inertia", this.Settings.inertia);
            this.Settings.erosion.SetFloat("sedimentCapacityFactor", this.Settings.sedimentCapacityFactor);
            this.Settings.erosion.SetFloat("minSedimentCapacity", this.Settings.minSedimentCapacity);
            this.Settings.erosion.SetFloat("depositSpeed", this.Settings.depositSpeed);
            this.Settings.erosion.SetFloat("erodeSpeed", this.Settings.erodeSpeed);
            this.Settings.erosion.SetFloat("evaporateSpeed", this.Settings.evaporateSpeed);
            this.Settings.erosion.SetFloat("gravity", this.Settings.gravity);
            this.Settings.erosion.SetFloat("startSpeed", this.Settings.startSpeed);
            this.Settings.erosion.SetFloat("startWater", this.Settings.startWater);

            // Run compute shader
            this.Settings.erosion.Dispatch(0, numThreads, 1, 1);
            mapBuffer.GetData(HeightMap);
            waterMapBuffer.GetData(WaterMap);

            // Release buffers
            mapBuffer.Release();
            //randomIndexBuffer.Release ();
            brushIndexBuffer.Release();
            brushWeightBuffer.Release();
        }
    }
}
