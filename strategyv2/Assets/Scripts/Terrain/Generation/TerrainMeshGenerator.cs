﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class MeshGeneratorArgs
{
    /// <summary>
    /// size of the outside border of the 
    /// </summary>
    public int BorderSize = 3;
    /// <summary>
    /// scales the number of verts per index in the height map
    /// </summary>
    public int resolutionScale = 1;
    public float Scale = 20;
    public float ElevationScale = 10;
}


public class TerrainMeshGenerator : MonoBehaviour
{
    public static TerrainMeshGenerator Instance;

    #region Terrain Mesh
    private Mesh m_Mesh;
    [SerializeField]
    private MeshRenderer m_MeshRenderer;
    [SerializeField]
    private MeshFilter m_MeshFilter;
    [SerializeField]
    private Material m_Material;
    [SerializeField]
    private Texture2D texture;
    #endregion Terrain Mesh

    #region Water Meshes
    [SerializeField]
    private GameObject m_WaterMeshPrefab;
    [SerializeField]
    private List<GameObject> m_WaterMeshes = new List<GameObject>();
    public int[,] WaterComponentMap;
    #endregion Water Meshes

    void Awake()
    {
        Instance = this;
    }

    //TODO Block this into chuncks 
    public void ConstructMesh(float[,] rawMap, MeshGeneratorArgs otherArgs)
    {
        float[,] map = ScaleHeightMapResolution(rawMap, otherArgs.resolutionScale);
        int heightMapSize = map.GetUpperBound(0);
        int meshSize = map.GetUpperBound(0);
        Vector3[] verts = new Vector3[meshSize * meshSize];
        int[] triangles = new int[(meshSize - 1) * (meshSize - 1) * 6];
        Vector2[] uvs = new Vector2[meshSize * meshSize];
        int t = 0;
        int mapSizeWithBorder = meshSize + otherArgs.BorderSize * 2;

        for (int x = 0; x < meshSize; x++)
        {
            for (int y = 0; y < meshSize; y++)
            {
                //int borderedMapIndex = (y + otherArgs.BorderSize) * mapSizeWithBorder + x + otherArgs.BorderSize;
                int meshMapIndex = y * meshSize + x;

                Vector2 uv = new Vector2(x / (meshSize - 1f), y / (meshSize - 1f));
                uvs[meshMapIndex] = uv;

                Vector3 pos = new Vector3(uv.x, 0, uv.y) * otherArgs.Scale;
                Vector2Int heightMapIndex = new Vector2Int((int)(heightMapSize * uv.x), (int)(heightMapSize * uv.y));
                float normalizedHeight = map[heightMapIndex.x, heightMapIndex.y];
                pos += Vector3.up * normalizedHeight * otherArgs.ElevationScale;
                verts[meshMapIndex] = pos;

                // Construct triangles
                if (x != meshSize - 1 && y != meshSize - 1)
                {
                    t = (y * (meshSize - 1) + x) * 3 * 2;

                    triangles[t + 0] = meshMapIndex + meshSize;
                    triangles[t + 1] = meshMapIndex + meshSize + 1;
                    triangles[t + 2] = meshMapIndex;

                    triangles[t + 3] = meshMapIndex + meshSize + 1;
                    triangles[t + 4] = meshMapIndex + 1;
                    triangles[t + 5] = meshMapIndex;
                    t += 6;
                }
            }
        }

        if (m_Mesh == null)
        {
            m_Mesh = new Mesh();
        }
        else
        {
            m_Mesh.Clear();
        }

        m_Mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m_Mesh.vertices = verts;
        m_Mesh.triangles = triangles;
        m_Mesh.uv = uvs;
        m_Mesh.RecalculateNormals();
        m_MeshFilter.sharedMesh = m_Mesh;
        //m_MeshRenderer.sharedMaterial = m_Material;

        //m_MeshRenderer.material.SetFloat("_MaxHeight", otherArgs.ElevationScale);
    }

    public void GenerateAndSetTextures(Terrain[,] map, Dictionary<Terrain, TerrainMapTile> terrainTileLookup)
    {
        int width = map.GetUpperBound(0);
        int height = map.GetUpperBound(1);
        texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Point;

        Dictionary<int, Color> waterComponentColorMap = new Dictionary<int, Color>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int x = j;
                int y = height - 1 - i;

                if(map[x, y] == Terrain.Water)
                {
                    if(waterComponentColorMap.TryGetValue(WaterComponentMap[x,y], out Color color))
                    {
                        texture.SetPixel(x, y, color);
                    }
                    else
                    {
                        Color randColor = UnityEngine.Random.ColorHSV();
                        waterComponentColorMap.Add(WaterComponentMap[x, y], randColor);
                        texture.SetPixel(x, y, randColor);

                    }
                }
                else
                {
                    texture.SetPixel(x, y, terrainTileLookup[map[x, y]].SimpleDisplayColor);
                }
                
            }
        }

        texture.Apply();
        //byte[] bytes = texture.EncodeToPNG();
        //File.WriteAllBytes(Application.dataPath + "/Textures/SavedScreen.png", bytes);
        m_MeshRenderer.material.SetTexture("_MainTex", texture);
        //m_MeshRenderer.sharedMaterial = m_Material;
        
    }

    public void ConstructWaterMeshes(MeshGeneratorArgs meshArgs, ref float[,] heightMap, ref float[,] waterMap, ref Terrain[,] terrainTileMap)
    {
        // Clear any old water meshes
        m_WaterMeshes.ForEach(x => DestroyImmediate(x));
        m_WaterMeshes.Clear();

        // Flood fill water components
        WaterComponentMap = new int[heightMap.GetUpperBound(0), heightMap.GetUpperBound(1)];
        int componentCounter = 1;

        // MUST NOT USE DIAGONALS will mess up mesh generation
        Vector2Int[] floodFillDirections = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
        };

        List<HashSet<Vector2Int>> components = new List<HashSet<Vector2Int>>();

        // 0 represents unmarked, -1 is land, and any other number is a water component
        for (int y = 0; y <= WaterComponentMap.GetUpperBound(0); y++)
        {
            for (int x = 0; x <= WaterComponentMap.GetUpperBound(1); x++)
            {
                // Make sure that the tile is water, and that we havent set a component number on it yet
                if(terrainTileMap[x,y] == Terrain.Water && WaterComponentMap[x,y] == 0)
                {
                    HashSet<Vector2Int> componet = FloodFill(new Vector2Int(x,y), componentCounter, floodFillDirections, ref WaterComponentMap, ref heightMap, ref waterMap, ref terrainTileMap);
                    components.Add(componet);
                    ConstructWaterMeshFromComponent(componet, meshArgs, ref heightMap);
                    //Debug.Log($"componenet :{componentCounter}, size: {componet.Count}");
                    componentCounter++;
                }
                else if(WaterComponentMap[x, y] == 0)
                {
                    // If its land mark as -1
                    WaterComponentMap[x, y] = -1;
                }
            }
        }

        Debug.Log($"{componentCounter} unique water zones");

        // Construct mesh for each component
    }

    public void ConstructWaterMeshFromComponent(HashSet<Vector2Int> componet, MeshGeneratorArgs meshArgs, ref float[,] heightMap)
    {
        List<Vector3> verts = new List<Vector3>();
        Dictionary<Vector2Int, int> vertToIndexMap = new Dictionary<Vector2Int, int>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        int meshSize = heightMap.GetUpperBound(0);

        Vector2Int[] adjacentDirections = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
            //Vector2Int.up + Vector2Int.left,
            //Vector2Int.up + Vector2Int.right,
            //Vector2Int.down + Vector2Int.left,
            //Vector2Int.down + Vector2Int.right,
        };

        foreach (var index in componet)
        {
            List<Vector2Int> adjacentIndexes = new List<Vector2Int>(); 
            // Check all potential adjacent verts, and see if they are in the componet
            foreach(var dir in adjacentDirections)
            {
                Vector2Int otherIndex = index + dir;
                if(componet.Contains(otherIndex))
                {
                    // Need to look for point orthoganal to dir and this new point, in either direction
                    // We can only do this because we know that dir will be like (1,0) or (0,-1)
                    Vector2Int ortho = new Vector2Int(dir.y, dir.x);

                    // now we split because we have to handle positive and negative cases

                    Vector2Int pos = otherIndex + ortho;
                    if (componet.Contains(pos))
                    {
                        List<Vector2Int> points = new List<Vector2Int> { index, otherIndex, pos};
                        points = OrderVertsInClockWise(points);
                        AddVerts(points, meshSize, meshArgs, verts, triangles, uvs, vertToIndexMap, heightMap);
                    }

                    Vector2Int neg = otherIndex - ortho;
                    if (componet.Contains(neg))
                    {
                        List<Vector2Int> points = new List<Vector2Int> { index, otherIndex, neg };
                        points = OrderVertsInClockWise(points);
                        AddVerts(points, meshSize, meshArgs, verts, triangles, uvs, vertToIndexMap, heightMap);
                    }
                    
                }
            }
        }

        Mesh mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            vertices = verts.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray()
        };

        mesh.RecalculateNormals();

        GameObject waterMesh = Instantiate(m_WaterMeshPrefab);
        m_WaterMeshes.Add(waterMesh);
        waterMesh.GetComponent<MeshFilter>().sharedMesh = mesh;
        waterMesh.transform.SetParent(transform);
    }

    private void AddVerts(List<Vector2Int> vertsToAdd, int meshSize, MeshGeneratorArgs meshArgs, List<Vector3> verts, List<int> triangles,
        List<Vector2> uvs, Dictionary<Vector2Int, int> vertToIndexMap, float[,] heightMap)
    {
        foreach(var vert in vertsToAdd)
        {
            int index;
            
            // check if we have already added the vert, if not add it
            if (!vertToIndexMap.TryGetValue(vert, out index))
            {
                index = verts.Count;
                Vector2 uv = new Vector2(vert.x / (meshSize - 1f), vert.y / (meshSize - 1f));
                Vector3 worldPos = new Vector3(uv.x, 0, uv.y) * meshArgs.Scale;
                worldPos += Vector3.up * heightMap[vert.x, vert.y] * meshArgs.ElevationScale;

                verts.Add(worldPos);
                uvs.Add(uv);
                vertToIndexMap.Add(vert, index);
            }

            triangles.Add(index);
        }
    }

    private List<Vector2Int> OrderVertsInClockWise(List<Vector2Int> verts)
    {
        // Find minx, miny
        Vector2Int min = verts[0];
        foreach (Vector2Int v in verts)
        {
            min.x = Math.Min(v.x, min.x);
            min.y = Math.Min(v.y, min.y);
        }

        List<Vector2Int> offsets = new List<Vector2Int>();

        // Calculate offsets from min
        for(int i = 0; i < verts.Count; i++)
        {
            offsets.Add(verts[i] - min);
        }

        // calculate center of tri
        float x = 0;
        float y = 0;
        foreach (Vector2 v in offsets)
        {
            x += v.x;
            y += v.y;
        }
        Vector2 center = new Vector2(0, 0);
        center.x = x / offsets.Count;
        center.y = y / offsets.Count;

        // Sort with custom compare
        List<int> ordering = new List<int> {0,1,2};

        ordering.Sort((aI,bI) => {
            Vector2Int a = offsets[aI];
            Vector2Int b = offsets[bI];
            // get the angle t for each of the points
            // ^ 
            // | t
            // - ->
            float a1 = ((Mathf.Rad2Deg * Mathf.Atan2(a.x - center.x, a.y - center.y)) + 360) % 360;
            float a2 = ((Mathf.Rad2Deg * Mathf.Atan2(b.x - center.x, b.y - center.y)) + 360) % 360;
            return (int)(a1 - a2);
        });

        List<Vector2Int> orderedVerts = new List<Vector2Int>() {
            verts[ordering[0]],
            verts[ordering[1]],
            verts[ordering[2]],
        };

        return orderedVerts;
    }

    private HashSet<Vector2Int> FloodFill(Vector2Int startPos, int componentNumber, Vector2Int[] floodFillDirections, ref int[,] waterComponentMap, ref float[,] heightMap, ref float[,] waterMap, ref Terrain[,] terrainTileMap)
    {
        WaterComponentMap[startPos.x, startPos.y] = componentNumber;
        HashSet<Vector2Int> componet = new HashSet<Vector2Int>();
        componet.Add(startPos);
        foreach (var dir in floodFillDirections)
        {
            Vector2Int newPos = startPos + dir;
            if (LayerMapFunctions.InBounds(terrainTileMap, new Vector2Int(newPos.x, newPos.y)) && 
                terrainTileMap[newPos.x, newPos.y] == Terrain.Water && 
                waterComponentMap[newPos.x, newPos.y] == 0)
            {
                componet.UnionWith(FloodFill(newPos, componentNumber, floodFillDirections, ref waterComponentMap, ref heightMap, ref waterMap, ref terrainTileMap));
            }
        }

        return componet;
    }

    public float QuadLerp(float a, float b, float c, float d, float u, float v)
    {
        v = 1 - v;
        float abu = Mathf.Lerp(a, b, u);
        float dcu = Mathf.Lerp(d, c, u);
        return Mathf.Lerp(abu, dcu, v);
    }

    private float[,] ScaleHeightMapResolution(float[,] rawMap, int resolutionScale)
    {
        int rawMapSize = rawMap.GetUpperBound(0);
        int scaledMapSize = rawMapSize * resolutionScale;
        float[,] scaledMap = new float[scaledMapSize, scaledMapSize];
        float maxDistance = resolutionScale * Mathf.Sqrt(2);

        List<Vector2> interpolationDirections = new List<Vector2>()
        {
            Vector2.left, Vector2.right, Vector2.up, Vector2.down
        };


        LayerMapFunctions.ParallelForFast(scaledMap, (x, y) => {
            Vector2 rawPosition = new Vector2(x, y) / resolutionScale;
            Vector2Int rawIndex = new Vector2Int((int)(x / resolutionScale), (int)(y / resolutionScale));

            Vector2Int[] rawIndexes = new Vector2Int[]
            {
                    new Vector2Int(rawIndex.x,     rawIndex.y + 1), // topLeft
                    new Vector2Int(rawIndex.x + 1, rawIndex.y + 1), // topRight
                    new Vector2Int(rawIndex.x + 1, rawIndex.y),     // bottomRight
                    new Vector2Int(rawIndex.x,     rawIndex.y),     // bottomLeft
            };

            float[] heights = new float[4];

            for (int i = 0; i < rawIndexes.Length; i++)
            {
                heights[i] = rawMap[rawIndexes[i].x, rawIndexes[i].y];
            }

            Vector2 uv = rawPosition - rawIndex;
            float height = QuadLerp(heights[0], heights[1], heights[2], heights[3], uv.x, uv.y);
            scaledMap[x, y] = height;
        });

        LayerMapFunctions.LogAction(() => LayerMapFunctions.SmoothMT(ref scaledMap, resolutionScale * resolutionScale), "smooth time");

        return scaledMap;
    }

    //void AssignMeshComponents()
    //{
    //    Transform meshHolder = new GameObject().transform;
    //    meshHolder.transform.parent = transform;
    //    meshHolder.transform.localPosition = Vector3.zero;
    //    meshHolder.transform.localRotation = Quaternion.identity;
        

    //    // Ensure mesh renderer and filter components are assigned
    //    if (!meshHolder.gameObject.GetComponent<MeshFilter>())
    //    {
    //        meshHolder.gameObject.AddComponent<MeshFilter>();
    //    }
    //    if (!meshHolder.GetComponent<MeshRenderer>())
    //    {
    //        meshHolder.gameObject.AddComponent<MeshRenderer>();
    //    }

    //    m_MeshRenderer = meshHolder.GetComponent<MeshRenderer>();
    //    m_MeshFilter = meshHolder.GetComponent<MeshFilter>();
    //}
}