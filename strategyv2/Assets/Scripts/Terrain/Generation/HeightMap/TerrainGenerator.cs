using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ErosionOptions
{
    public bool enabled = true;
    public int numDropletsPerCell = 8;
}

public class TerrainGenerator : MonoBehaviour {

    [Header ("Erosion Settings")]
    public ComputeShader erosion;
    public int MaxNumThreads = 65535;

    public int erosionBrushRadius = 3;

    public int maxLifetime = 30;
    public float sedimentCapacityFactor = 3;
    public float minSedimentCapacity = .01f;
    public float depositSpeed = 0.3f;
    public float erodeSpeed = 0.3f;

    public float evaporateSpeed = .01f;
    public float gravity = 4;
    public float startSpeed = 1;
    public float startWater = 1;
    [Range (0, 1)]
    public float inertia = 0.3f;

    // Internal
    public float[] HeightMap { get; private set; }
    public float[] LakeMap { get; private set; }

    public int mapSizeWithBorder;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    public void GenerateHeightMap (int mapSize) {
        mapSizeWithBorder = mapSize + erosionBrushRadius * 2;
        HeightMap = FindObjectOfType<HeightMapGenerator> ().GenerateHeightMap (mapSizeWithBorder);
        LakeMap = new float[mapSizeWithBorder * mapSizeWithBorder];
    }

    public void Erode (int mapSize, ErosionOptions options) {
        // Create brush
        List<int> brushIndexOffsets = new List<int> ();
        List<float> brushWeights = new List<float> ();

        float weightSum = 0;
        for (int brushY = -erosionBrushRadius; brushY <= erosionBrushRadius; brushY++) {
            for (int brushX = -erosionBrushRadius; brushX <= erosionBrushRadius; brushX++) {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst < erosionBrushRadius * erosionBrushRadius) {
                    brushIndexOffsets.Add (brushY * mapSize + brushX);
                    float brushWeight = 1 - Mathf.Sqrt (sqrDst) / erosionBrushRadius;
                    weightSum += brushWeight;
                    brushWeights.Add (brushWeight);
                }
            }
        }
        for (int i = 0; i < brushWeights.Count; i++) {
            brushWeights[i] /= weightSum;
        }

        // Send brush data to compute shader
        ComputeBuffer brushIndexBuffer = new ComputeBuffer (brushIndexOffsets.Count, sizeof (int));
        ComputeBuffer brushWeightBuffer = new ComputeBuffer (brushWeights.Count, sizeof (int));
        brushIndexBuffer.SetData (brushIndexOffsets);
        brushWeightBuffer.SetData (brushWeights);
        erosion.SetBuffer (0, "brushIndices", brushIndexBuffer);
        erosion.SetBuffer (0, "brushWeights", brushWeightBuffer);

        int numThreads = System.Math.Min(HeightMap.Length, MaxNumThreads);
        int numElementsToProcess = Mathf.CeilToInt(HeightMap.Length / (float)numThreads);
        Debug.Log($"Erosion: num elements = {HeightMap.Length}, num GPU Threads = {numThreads}, each doing {numElementsToProcess} elements");

        // Heightmap buffer
        ComputeBuffer mapBuffer = new ComputeBuffer (HeightMap.Length, sizeof (float));
        mapBuffer.SetData (HeightMap);
        erosion.SetBuffer (0, "map", mapBuffer);

        // WaterMap buffer
        ComputeBuffer waterMapBuffer = new ComputeBuffer(LakeMap.Length, sizeof(float));
        waterMapBuffer.SetData(LakeMap);
        erosion.SetBuffer(0, "waterMap", waterMapBuffer);  

        // Settings
        erosion.SetInt ("borderSize", erosionBrushRadius);
        erosion.SetInt ("mapSize", mapSizeWithBorder);

        erosion.SetInt ("numThreads", numThreads);
        erosion.SetInt ("numElementsToProcess", numElementsToProcess);

        erosion.SetInt ("numDropletsPerCell", options.numDropletsPerCell);

        erosion.SetInt ("brushLength", brushIndexOffsets.Count);
        erosion.SetInt ("maxLifetime", maxLifetime);
        erosion.SetFloat ("inertia", inertia);
        erosion.SetFloat ("sedimentCapacityFactor", sedimentCapacityFactor);
        erosion.SetFloat ("minSedimentCapacity", minSedimentCapacity);
        erosion.SetFloat ("depositSpeed", depositSpeed);
        erosion.SetFloat ("erodeSpeed", erodeSpeed);
        erosion.SetFloat ("evaporateSpeed", evaporateSpeed);
        erosion.SetFloat ("gravity", gravity);
        erosion.SetFloat ("startSpeed", startSpeed);
        erosion.SetFloat ("startWater", startWater);

        // Run compute shader
        erosion.Dispatch (0, numThreads, 1, 1);
        mapBuffer.GetData (HeightMap);
        waterMapBuffer.GetData(LakeMap);

        // Release buffers
        mapBuffer.Release ();
        //randomIndexBuffer.Release ();
        brushIndexBuffer.Release ();
        brushWeightBuffer.Release ();
    }
}