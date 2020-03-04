using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HeightMapGeneration
{
    public enum Algorithm
    {
        RandomNoise, Erosion, Terrace
    }

    public class NodeUtils
    {
        public static Dictionary<Algorithm, Type> AlgorithmTypeMap = new Dictionary<Algorithm, Type>()
        {
            {Algorithm.Erosion, typeof(ErosionLayerSettings) },
            {Algorithm.RandomNoise, typeof(RandomNoiseLayerSettings) },
            {Algorithm.Terrace, typeof(TerraceLayerSettings) },
        };
    }

    public class RandomNoiseLayerSettings : ScriptableObject
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
    }

    public class ErosionLayerSettings : ScriptableObject
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
    }

    public class TerraceLayerSettings : ScriptableObject
    {
        public int NumLayers = 10;
    }
}



