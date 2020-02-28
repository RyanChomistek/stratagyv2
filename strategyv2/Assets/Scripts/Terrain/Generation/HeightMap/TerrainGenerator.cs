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
        HeightMap = FindObjectOfType<HeightMapGenerator>().GenerateHeightMap(mapSizeWithBorder);
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

    public void ConvertMapsTo2D(MapData mapdata)
    {
        mapdata.VertexHeightMap = new float[mapdata.MeshHeightMapSize, mapdata.MeshHeightMapSize];
        mapdata.RawWaterLevelMap = new float[mapdata.MeshHeightMapSize, mapdata.MeshHeightMapSize];

        // Convert the height maps to be in 2d form
        for (int i = 0; i < mapdata.MeshHeightMapSize * mapdata.MeshHeightMapSize; i++)
        {
            int x = i % mapdata.MeshHeightMapSize;
            int y = i / mapdata.MeshHeightMapSize;
            int borderedMapIndex = (y + erosionBrushRadius) * mapSizeWithBorder + x + erosionBrushRadius;
            mapdata.VertexHeightMap[x, y] = HeightMap[borderedMapIndex];
            mapdata.RawWaterLevelMap[x, y] = LakeMap[borderedMapIndex];
        }

        LayerMapFunctions.SmoothMT(ref mapdata.VertexHeightMap, 2);

        // Create the tile height map
        // take every 4 height map points and find the middle value and use that
        for (int x = 0; x < mapdata.MeshHeightMapSize - 1; x++)
        {
            for (int y = 0; y < mapdata.MeshHeightMapSize - 1; y++)
            {
                Vector2Int rawIndex = new Vector2Int(x,y);
                Vector2Int[] indexes = new Vector2Int[]
                {
                    new Vector2Int(rawIndex.x,     rawIndex.y + 1), // topLeft
                    new Vector2Int(rawIndex.x + 1, rawIndex.y + 1), // topRight
                    new Vector2Int(rawIndex.x + 1, rawIndex.y),     // bottomRight
                    new Vector2Int(rawIndex.x,     rawIndex.y),     // bottomLeft
                };

                float[] heights = new float[4];
                for (int i = 0; i < indexes.Length; i++)
                {
                    heights[i] = mapdata.VertexHeightMap[indexes[i].x, indexes[i].y];
                }

                float[] waterLevels = new float[4];
                for (int i = 0; i < indexes.Length; i++)
                {
                    waterLevels[i] = mapdata.RawWaterLevelMap[indexes[i].x, indexes[i].y];
                }

                Vector2 uv = new Vector2(.5f, .5f);

                float waterLevel = TerrainMeshGenerator.QuadLerp(waterLevels[0], waterLevels[1], waterLevels[2], waterLevels[3], uv.x, uv.y);
                mapdata.WaterMap[x, y] = waterLevel;

                float height = TerrainMeshGenerator.QuadLerp(heights[0], heights[1], heights[2], heights[3], uv.x, uv.y);
                mapdata.HeightMap[x, y] = height;
            }
        }
    }
}