using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HeightMapGeneration
{
    public class RandomNoiseSettings
    {
        [SerializeField]
        public int seed;
        public bool randomizeSeed;
        public int MaxNumThreads = 65535;
        public int numOctaves = 7;
        public float persistence = .5f;
        public float lacunarity = 2;
        public float initialScale = 2;
        public ComputeShader heightMapComputeShader;
        public bool GenerationEnabled = true;
        public int MapSize = 128;
    }

    public class RandomNoiseHeightMapLayer : HeightMapLayerBase
    {
        private RandomNoiseSettings Settings;

        public RandomNoiseHeightMapLayer(RandomNoiseSettings settings)
        {
            this.Settings = settings;
        }

        public void Apply(HeightMapGenerationData HMData)
        {
            float[] heightMap = TerrainGenerator.Convert2DMapTo1D(HMData.MapSize, HMData.HeightMap);
            GenerateHeightMapGPU(HMData.MapSize, heightMap);

            HMData.HeightMap = TerrainGenerator.Convert1DMapTo2D(HMData.MapSize, heightMap);
        }

        public float[] Run(float[] BaseHeightMap)
        {
            float[] HeightMap = (float[]) BaseHeightMap.Clone();
            GenerateHeightMapGPU((int) Mathf.Sqrt(BaseHeightMap.Length), HeightMap);
            return HeightMap;
        }

        void GenerateHeightMapGPU(int mapSize, float[] heightMap)
        {
            if (!Settings.GenerationEnabled)
            {
                return;
            }

            Settings.seed = (Settings.randomizeSeed) ? Random.Range(-10000, 10000) : Settings.seed;
            var prng = new System.Random(Settings.seed);

            Vector2[] offsets = new Vector2[Settings.numOctaves];
            for (int i = 0; i < Settings.numOctaves; i++)
            {
                offsets[i] = new Vector2(prng.Next(-10000, 10000), prng.Next(-10000, 10000));
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
            Debug.Log($"HeightMapGen: num elements = {heightMap.Length}, num GPU Threads = {numThreads}, each doing {numElementsToProcess} elements");

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
    }
}
