using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    void Awake()
    {
        Instance = this;
    }

    //TODO Block this into chuncks 
    public void ContructMesh(float[,] rawMap)
    {
        float[,] map = ScaleHeightMapResolution(rawMap, resolutionScale);
        int heightMapSize = map.GetUpperBound(0);
        int meshSize = map.GetUpperBound(0);
        Vector3[] verts = new Vector3[meshSize * meshSize];
        int[] triangles = new int[(meshSize - 1) * (meshSize - 1) * 6];
        int t = 0;
        int mapSizeWithBorder = meshSize + BorderSize * 2;

        for (int x = 0; x < meshSize; x++)
        {
            for (int y = 0; y < meshSize; y++)
            {
                int borderedMapIndex = (y + BorderSize) * mapSizeWithBorder + x + BorderSize;
                int meshMapIndex = y * meshSize + x;

                Vector2 percent = new Vector2(x / (meshSize - 1f), y / (meshSize - 1f));
                Vector3 pos = new Vector3(percent.x, 0, percent.y) * Scale;
                Vector2Int heightMapIndex = new Vector2Int((int)(heightMapSize * percent.x), (int)(heightMapSize * percent.y));
                float normalizedHeight = map[heightMapIndex.x, heightMapIndex.y];
                pos += Vector3.up * normalizedHeight * ElevationScale;
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
        m_Mesh.RecalculateNormals();

        //AssignMeshComponents();
        m_MeshFilter.sharedMesh = m_Mesh;
        m_MeshRenderer.sharedMaterial = m_Material;

        m_Material.SetFloat("_MaxHeight", ElevationScale);
    }

    private float[,] ScaleHeightMapResolution(float[,] rawMap, int resolutionScale)
    {
        int rawMapSize = rawMap.GetUpperBound(0);
        int scaledMapSize = rawMapSize * resolutionScale;
        float[,] scaledMap = new float[scaledMapSize, scaledMapSize];
        for (int x = 0; x < scaledMapSize; x++)
        {
            for (int y = 0; y < scaledMapSize; y++)
            {
                Vector2 percent = new Vector2(x / (scaledMapSize - 1f), y / (scaledMapSize - 1f));
                Vector2Int rawIndex = new Vector2Int((int)(rawMapSize * percent.x), (int)(rawMapSize * percent.y));

                scaledMap[x, y] = rawMap[rawIndex.x, rawIndex.y];
            }
        }

        LayerMapFunctions.Smooth(ref scaledMap, resolutionScale * 2);
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
