using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandMeshGenerator : MonoBehaviour
{
    [SerializeField] private Mesh m_Mesh;
    [SerializeField] private MeshRenderer m_MeshRenderer;
    [SerializeField] private MeshFilter m_MeshFilter;

    [SerializeField] private Vector3 m_MeshSize;

    private MapData m_Mapdata = null;

    public void ConstructMesh(MapData mapData, Dictionary<Terrain, TerrainMapTile> terrainTileLookup, Dictionary<Improvement, ImprovementMapTile> improvementTileLookup)
    {
        m_Mapdata = mapData;
        ref SquareArray<float> heightMap = ref mapData.VertexHeightMap;

        int mapSize = heightMap.SideLength;

        Vector3[] verts = new Vector3[heightMap.Length];
        Vector2[] uvs = new Vector2[heightMap.Length];

        int[] triangles = new int[(mapSize - 1) * (mapSize - 1) * 6];

        int t = 0;

        for(int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                int vertIndex = y * mapSize + x;
                Vector2 uv = new Vector2((x / (float)mapSize), (y / (float)mapSize));
                uvs[vertIndex] = uv;
                verts[vertIndex] = new Vector3(uv.x * m_MeshSize.x, heightMap[x, y] * m_MeshSize.y, uv.y * m_MeshSize.z);

                if (x != mapSize - 1 && y != mapSize - 1)
                {
                    t = (y * (mapSize - 1) + x) * 3 * 2;

                    triangles[t + 0] = vertIndex + mapSize;
                    triangles[t + 1] = vertIndex + mapSize + 1;
                    triangles[t + 2] = vertIndex;

                    triangles[t + 3] = vertIndex + mapSize + 1;
                    triangles[t + 4] = vertIndex + 1;
                    triangles[t + 5] = vertIndex;
                    t += 6;
                }
            }
        }

        m_Mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m_Mesh.vertices = verts;
        m_Mesh.triangles = triangles;
        m_Mesh.uv = uvs;
        m_Mesh.RecalculateNormals();
        m_MeshFilter.sharedMesh = m_Mesh;

        // Set texture for sediment
        SquareArray<Color32> sedimentColors = new SquareArray<Color32>(mapData.SedimentMap.SideLength);
        Texture2D SedimentTexture = new Texture2D(mapData.SedimentMap.SideLength, mapData.SedimentMap.SideLength);
        for (int x = 0; x < mapData.SedimentMap.SideLength; x++)
        {
            for (int y = 0; y < mapData.SedimentMap.SideLength; y++)
            {
                sedimentColors[x, y] = Color32.Lerp(Color.black, Color.white, mapData.SedimentMap[x, y]);
            }
        }
        SedimentTexture.SetPixels32(sedimentColors.Array);
        SedimentTexture.Apply(true);

        // Set texture for terrain
        SquareArray<Color32> terrainColors = new SquareArray<Color32>(mapData.TerrainMap.SideLength);
        Texture2D terrainTexture = new Texture2D(mapData.TerrainMap.SideLength, mapData.TerrainMap.SideLength);
        
        for (int x = 0; x < mapData.TerrainMap.SideLength; x++)
        {
            for (int y = 0; y < mapData.TerrainMap.SideLength; y++)
            {
                terrainColors[x, y] = terrainTileLookup[mapData.TerrainMap[x, y]].SimpleDisplayColor;
            }
        }
        
        terrainTexture.SetPixels32(terrainColors.Array);
        terrainTexture.Apply(true);

        m_MeshRenderer.sharedMaterial.mainTexture = terrainTexture;
        m_MeshRenderer.sharedMaterial.SetTexture("_SedimentTex", SedimentTexture);
        m_MeshRenderer.sharedMaterial.SetFloat("_MaxHeight", m_MeshSize.y);
        // m_MeshRenderer.sharedMaterial = material;
    }

    public float GetHeightAtWorldPosition(Vector3 position)
    {
        ref SquareArray<float> heightMap = ref m_Mapdata.VertexHeightMap;

        Vector2Int coord = VectorUtilityFunctions.RoundVector(position);

        return heightMap[coord] * m_MeshSize.y;
    }

    public Vector2Int ConvertWorldPositionToTilePosition(Vector3 worldPosition)
    {
        Vector3 scale = VectorUtilityFunctions.DivScaler(m_MeshSize, m_Mapdata.TileMapSize);

        return new Vector2Int(Mathf.FloorToInt(worldPosition.x / scale.x), Mathf.FloorToInt(worldPosition.z / scale.z));
    }

    public Vector2 ConvertTilePositionToWorldPosition(Vector2 pos, int mapSize)
    {
        Vector3 scale = VectorUtilityFunctions.DivScaler(m_MeshSize, m_Mapdata.TileMapSize);
        return new Vector2(pos.x * scale.x, pos.y * scale.z);
    }

    public Vector3 GetMeshSize()
    {
        return m_MeshSize;
    }
}
