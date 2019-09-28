using System;
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

    private Mesh m_Mesh;
    [SerializeField]
    private MeshRenderer m_MeshRenderer;
    [SerializeField]
    private MeshFilter m_MeshFilter;
    [SerializeField]
    private Material m_Material;
    [SerializeField]
    private Texture2D texture;
    void Awake()
    {
        Instance = this;
    }

    //TODO Block this into chuncks 
    public void ContructMesh(float[,] rawMap, MeshGeneratorArgs otherArgs)
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
                int borderedMapIndex = (y + otherArgs.BorderSize) * mapSizeWithBorder + x + otherArgs.BorderSize;
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

        //AssignMeshComponents();
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

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int x = j;
                int y = height - 1 - i;
                texture.SetPixel(x, y, terrainTileLookup[map[x,y]].SimpleDisplayColor);
            }
        }

        texture.Apply();
        //byte[] bytes = texture.EncodeToPNG();
        //File.WriteAllBytes(Application.dataPath + "/Textures/SavedScreen.png", bytes);
        m_MeshRenderer.material.SetTexture("_MainTex", texture);
        //m_MeshRenderer.sharedMaterial = m_Material;
        
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
