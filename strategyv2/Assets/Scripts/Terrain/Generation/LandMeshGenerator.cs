using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandMeshGenerator : MonoBehaviour
{
    [SerializeField] private Mesh m_Mesh;
    [SerializeField] private MeshRenderer m_MeshRenderer;
    [SerializeField] private MeshFilter m_MeshFilter;

    private MapData m_Mapdata = null;
    private MeshGeneratorArgs m_MeshArgs;

    public void ConstructMesh(
        MapData mapData, 
        Dictionary<Terrain, TerrainMapTile> terrainTileLookup, 
        Dictionary<Improvement, ImprovementMapTile> improvementTileLookup,
        MeshGeneratorArgs meshArgs)
    {
        m_MeshArgs = meshArgs;
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
                verts[vertIndex] = new Vector3(uv.x * meshArgs.MeshSize.x, heightMap[x, y] * meshArgs.MeshSize.y, uv.y * meshArgs.MeshSize.z);

                if (x != mapSize - 1 && y != mapSize - 1)
                {
                    t = (y * (mapSize - 1) + x) * 3 * 2;

                    triangles[t + 0] = vertIndex + mapSize;
                    triangles[t + 1] = vertIndex + mapSize + 1;
                    triangles[t + 2] = vertIndex;

                    triangles[t + 3] = vertIndex + mapSize + 1;
                    triangles[t + 4] = vertIndex + 1;
                    triangles[t + 5] = vertIndex;
                }
            }
        }

        m_Mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m_Mesh.vertices = verts;
        m_Mesh.triangles = triangles;
        m_Mesh.uv = uvs;
        m_Mesh.RecalculateNormals();
        
        m_MeshFilter.sharedMesh = m_Mesh;

        // Set texture for terrain
        GenerateTerrainTexure(mapData, terrainTileLookup);
        GenerateSedimentTexture(mapData, terrainTileLookup);

        m_MeshRenderer.sharedMaterial.SetFloat("_MaxHeight", meshArgs.MeshSize.y);
    }

    public void GenerateTerrainTexure(MapData mapData,
        Dictionary<Terrain, TerrainMapTile> terrainTileLookup)
    {
        int terrainTextureSize = 256;
        int numThreads = 16;
        SquareArray<TerrainChannels> alphaMap = new SquareArray<TerrainChannels>(terrainTextureSize);
        TerrainChannels[] neighborChannels = new TerrainChannels[numThreads];

        float texCordToTileCordScale = (float)mapData.TerrainMap.SideLength / (float)terrainTextureSize;
        float TileCordToTexCordScale = (float)terrainTextureSize / (float)mapData.TerrainMap.SideLength;

        for (int x = 0; x < terrainTextureSize; x++)
        {
            for (int y = 0; y < terrainTextureSize; y++)
            {
                alphaMap[x,y] = new TerrainChannels();
            }
        }

        for (int i = 0; i < numThreads; i++)
        {
            neighborChannels[i] = new TerrainChannels();
        }

        int tileLookDistance = 2;
        float maxDistance = TileCordToTexCordScale * tileLookDistance;
        SquareArray<Color32> terrainColors = new SquareArray<Color32>(terrainTextureSize);

        ArrayUtilityFunctions.ForMT(alphaMap.SideLength, (threadId, x, y) =>
        {
            Vector2Int tilePos = VectorUtilityFunctions.FloorVector(new Vector2(x, y) * texCordToTileCordScale);
            Vector2Int texPos = new Vector2Int(x, y);
            Vector2 texPosReal = VectorUtilityFunctions.Vec2IntToVec2(texPos);
            TerrainChannels ac = alphaMap[texPos];
            TerrainChannels neighborAc = neighborChannels[threadId];

            // loop through all of the neightbor tiles
            for (int dx = -tileLookDistance; dx <= tileLookDistance; dx++)
            {
                for (int dy = -tileLookDistance; dy <= tileLookDistance; dy++)
                {
                    Vector2Int neighborTilePos = tilePos + new Vector2Int(dx, dy);
                    if (mapData.TerrainMap.InBounds(neighborTilePos))
                    {
                        Vector2 neighborTileAphaMapPosCenter =
                            VectorUtilityFunctions.Vec2IntToVec2(neighborTilePos) * TileCordToTexCordScale +
                            new Vector2(TileCordToTexCordScale, TileCordToTexCordScale) / 2;
                        float distance = (neighborTileAphaMapPosCenter - texPosReal).magnitude;
                        float weight = 1 - (distance / maxDistance);
                        if (weight > 0)
                        {
                            neighborAc.Set(mapData.TerrainMap[neighborTilePos]);

                            neighborAc.Scale(weight);
                            ac.Add(neighborAc);
                        }
                    }
                }
            }

            ac.Normalize();
            terrainColors[x, y] = MixColors(ac, terrainTileLookup);
        }, numThreads);



        Texture2D terrainTexture = new Texture2D(terrainTextureSize, terrainTextureSize, TextureFormat.ARGB32, false);
        terrainTexture.SetPixels32(terrainColors.Array);
        terrainTexture.Apply(true);
        m_MeshRenderer.sharedMaterial.mainTexture = terrainTexture;
    }

    public void GenerateSedimentTexture(MapData mapData,
        Dictionary<Terrain, TerrainMapTile> terrainTileLookup)
    {
        // Set texture for sediment
        int sedimentTextureSize = 1024;
        SquareArray<Color32> sedimentColors = new SquareArray<Color32>(sedimentTextureSize);
        Texture2D SedimentTexture = new Texture2D(sedimentTextureSize, sedimentTextureSize);
        float sedimentTextureCordToSedimentScale = (float)mapData.SedimentMap.SideLength / (float)sedimentTextureSize;
        float sedimentCordToTerrainCordScale = (float)mapData.TerrainMap.SideLength / (float)mapData.SedimentMap.SideLength;

        for (int x = 0; x < sedimentTextureSize; x++)
        {
            for (int y = 0; y < sedimentTextureSize; y++)
            {
                Vector2 sedimentUV = new Vector2((x * sedimentTextureCordToSedimentScale), (y * sedimentTextureCordToSedimentScale));
                Vector2Int sedimentPos = VectorUtilityFunctions.FloorVector(sedimentUV);
                Vector2 uv = sedimentUV * sedimentCordToTerrainCordScale;

                Vector2Int tilePos = VectorUtilityFunctions.FloorVector(uv);

                Color sedimentColor = terrainTileLookup[mapData.TerrainMap[tilePos]].SedimentColor;

                sedimentColors[x, y] =
                    new Color(sedimentColor.r, sedimentColor.g, sedimentColor.b, mapData.SedimentMap[sedimentPos]);
            }
        }

        SedimentTexture.SetPixels32(sedimentColors.Array);
        SedimentTexture.Apply(true);
        m_MeshRenderer.sharedMaterial.SetTexture("_SedimentTex", SedimentTexture);
    }

    private Color32 MixColors(TerrainChannels channels, Dictionary<Terrain, TerrainMapTile> terrainTileLookup)
    {
        Color sum = new Color();
        for(int i = 0; i < (int)Terrain.Max; i++)
        {
            float weight = channels.channels[i];
            if(terrainTileLookup.ContainsKey((Terrain)i))
                sum += terrainTileLookup[(Terrain) i].SimpleDisplayColor * weight;
        }

        return sum;
    }

    public float GetHeightAtWorldPosition(Vector3 position)
    {
        ref SquareArray<float> heightMap = ref m_Mapdata.VertexHeightMap;

        Vector2Int coord = VectorUtilityFunctions.RoundVector(position);

        return heightMap[coord] * m_MeshArgs.MeshSize.y;
    }

    public Vector2Int ConvertWorldPositionToTilePosition(Vector3 worldPosition)
    {
        Vector3 scale = VectorUtilityFunctions.DivScaler(m_MeshArgs.MeshSize, m_Mapdata.TileMapSize);

        return new Vector2Int(Mathf.FloorToInt(worldPosition.x / scale.x), Mathf.FloorToInt(worldPosition.z / scale.z));
    }

    public Vector2 ConvertTilePositionToWorldPosition(Vector2 pos, int mapSize)
    {
        Vector3 scale = VectorUtilityFunctions.DivScaler(m_MeshArgs.MeshSize, m_Mapdata.TileMapSize);
        return new Vector2(pos.x * scale.x, pos.y * scale.z);
    }

    public Vector3 GetMeshSize()
    {
        return m_MeshArgs.MeshSize;
    }
}
