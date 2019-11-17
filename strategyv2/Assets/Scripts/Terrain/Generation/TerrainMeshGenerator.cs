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
    public float WaterScale = .1f;
}


public class TerrainMeshGenerator : MonoBehaviour
{
    public static TerrainMeshGenerator Instance;

    #region Terrain Mesh
    [SerializeField]
    private UnityEngine.Terrain m_Terrain;

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
    #endregion Water Meshes

    void Awake()
    {
        Instance = this;
    }

    //TODO Block this into chuncks 
    public void ConstructMesh(float[,] heightMap, Vector2[,] gradientMap, MeshGeneratorArgs otherArgs, Terrain[,] map,
        Improvement[,] improvementMap, Dictionary<Terrain, TerrainMapTile> terrainTileLookup)
    {
        Debug.Log(m_Terrain.terrainData.alphamapWidth + " " + m_Terrain.terrainData.alphamapHeight);

        TerrainData tData = m_Terrain.terrainData;

        float resolutionScale = tData.heightmapResolution / (float)heightMap.GetUpperBound(0);
        float alphaMapScale = tData.alphamapWidth / (float)map.GetUpperBound(0);
        float[,] scaledMap = ScaleHeightMapResolution(heightMap, resolutionScale);
        Debug.Log($"scales: resolution {resolutionScale}, alphaMap {alphaMapScale}");

        m_Terrain.terrainData.SetHeights(0, 0, scaledMap);

        float[,,] alphaData = tData.GetAlphamaps(0, 0, tData.alphamapWidth, tData.alphamapHeight);

        int GRASS = 0;
        int WATER = 1;
        int ROCK = 2;
        int ROAD = 3;

        for (int y = 0; y < tData.alphamapHeight; y++)
        {
            for (int x = 0; x < tData.alphamapWidth; x++)
            {
                Vector2Int terrainMapPos = new Vector2Int((int)(x / alphaMapScale), (int)(y / alphaMapScale));
                Terrain terrain = map[terrainMapPos.x, terrainMapPos.y];
                Improvement improvement = improvementMap[terrainMapPos.x, terrainMapPos.y];
                Vector2 gradient = gradientMap[terrainMapPos.x, terrainMapPos.y];

                if (terrain == Terrain.Water)
                {
                    alphaData[y, x, GRASS] = 0;
                    alphaData[y, x, WATER] = 1;
                    alphaData[y, x, ROCK] = 0;
                    alphaData[y, x, ROAD] = 0;
                }
                else
                {
                    float rockyness = gradient.magnitude * 10;

                    alphaData[y, x, GRASS] = 1 - rockyness;
                    alphaData[y, x, WATER] = 0;
                    alphaData[y, x, ROCK] = rockyness;
                    alphaData[y, x, ROAD] = 0;
                }
                
                if(improvement == Improvement.Road)
                {
                    alphaData[y, x, GRASS] = 0;
                    alphaData[y, x, WATER] = 0;
                    alphaData[y, x, ROCK] = 0;
                    alphaData[y, x, ROAD] = 1;
                }
            }
        }

        ConstructTrees(tData, improvementMap, heightMap, gradientMap, otherArgs);

        tData.SetAlphamaps(0, 0, alphaData);
    }

    public void ConstructTrees(TerrainData tData, Improvement[,] improvementMap, float[,] heightMap, Vector2[,] gradientMap, MeshGeneratorArgs otherArgs)
    {
        List<TreeInstance> trees = new List<TreeInstance>();
        Vector3 tileSize = new Vector3(1 / (float)improvementMap.GetUpperBound(0), 0, 1 / (float)improvementMap.GetUpperBound(1));
        float nudgeRadius = .25f;
        float scaleNudgeBase = .2f;
        for (int x = 0; x <= improvementMap.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= improvementMap.GetUpperBound(1); y++)
            {
                if(improvementMap[x,y] == Improvement.Forest && UnityEngine.Random.Range(0,4) == 1)
                {
                    // Move the tree a little so that we dont get a grid
                    Vector3 nudge = 
                        new Vector3( 
                            UnityEngine.Random.Range(-nudgeRadius, nudgeRadius) * tileSize.x,
                            0, 
                            UnityEngine.Random.Range(-nudgeRadius, nudgeRadius) * tileSize.z);

                    Vector3 treePos = 
                        new Vector3(
                            x / (float) improvementMap.GetUpperBound(0),
                            heightMap[x,y],
                            y / (float)improvementMap.GetUpperBound(1)) + nudge;

                    TreeInstance tree = new TreeInstance();
                    tree.color = Color.white;

                    Vector3 scaleNudge =
                        new Vector3(
                            UnityEngine.Random.Range(-scaleNudgeBase, scaleNudgeBase),
                            0,
                            UnityEngine.Random.Range(-scaleNudgeBase, scaleNudgeBase));

                    tree.heightScale = .5f + scaleNudge.x;
                    tree.widthScale = .25f + scaleNudge.z;

                    tree.lightmapColor = Color.white;
                    tree.position = treePos;
                    tree.prototypeIndex = 0;
                    tree.widthScale = 1;
                    trees.Add(tree);
                }
            }
        }

        Debug.Log("trees placed: " + trees.Count);
        tData.treeInstances = trees.ToArray();
    }

    public void ConstructWaterMeshes(MeshGeneratorArgs meshArgs, float[,] heightMap, float[,] waterMap, Terrain[,] terrainTileMap)
    {
        Debug.Log($"terrain y size {(m_Terrain.terrainData.size.y)} {m_Terrain.terrainData.bounds.max.y }");
        // Clear any old water meshes
        m_WaterMeshes.ForEach(x => DestroyImmediate(x));
        m_WaterMeshes.Clear();

        List<HashSet<Vector2Int>> components = null;
        LayerMapFunctions.LogAction(() => {
            components = LayerMapFunctions.FindComponents(Terrain.Water, heightMap.GetUpperBound(0), 2, ref terrainTileMap);
        }, "create components time");

        float[,] waterClone = waterMap.Clone() as float[,];

        LayerMapFunctions.Normalize(ref waterClone);

        foreach(var componet in components)
        {
            ConstructWaterMeshFromComponent(componet, meshArgs, ref heightMap, waterClone, ref terrainTileMap);
        }

        Debug.Log($"{components.Count} unique water zones");
    }

    public void ConstructWaterMeshFromComponent(HashSet<Vector2Int> componet, MeshGeneratorArgs meshArgs, ref float[,] heightMap, float[,] waterMap, ref Terrain[,] terrainTileMap)
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
        };

        // Find minimum height
        float minHeight = 100;
        float sumHeight = 0;
        foreach (var index in componet)
        {
            minHeight = Mathf.Min(minHeight, heightMap[index.x, index.y]);
            sumHeight += heightMap[index.x, index.y];
        }

        Debug.Log($"average componenet water height {sumHeight / componet.Count}");

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
                        AddVerts(points, meshSize, meshArgs, verts, triangles, uvs, vertToIndexMap, heightMap, waterMap, minHeight, ref terrainTileMap);
                    }

                    Vector2Int neg = otherIndex - ortho;
                    if (componet.Contains(neg))
                    {
                        List<Vector2Int> points = new List<Vector2Int> { index, otherIndex, neg };
                        points = OrderVertsInClockWise(points);
                        AddVerts(points, meshSize, meshArgs, verts, triangles, uvs, vertToIndexMap, heightMap, waterMap, minHeight, ref terrainTileMap);
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

        // Set the clip offset to the height of the water, make sure to scale by the terrain size
        waterMesh.GetComponent<UnityStandardAssets.Water.Water>().clipPlaneOffset = (sumHeight / componet.Count) * (m_Terrain.terrainData.size.y);
        waterMesh.transform.SetParent(transform);
    }

    private void AddVerts(List<Vector2Int> vertsToAdd, int meshSize, MeshGeneratorArgs meshArgs, List<Vector3> verts, List<int> triangles,
        List<Vector2> uvs, Dictionary<Vector2Int, int> vertToIndexMap, float[,] heightMap, float[,] waterMap, float minComponentHeight, ref Terrain[,] terrainTileMap)
    {
        foreach(var vert in vertsToAdd)
        {
            int index;
            
            // check if we have already added the vert, if not add it
            if (!vertToIndexMap.TryGetValue(vert, out index))
            {
                index = verts.Count;
                Vector2 uv = new Vector2(vert.x / (meshSize - 1f), vert.y / (meshSize - 1f));
                Vector3 worldPos = new Vector3(uv.x, 0, uv.y) * m_Terrain.terrainData.bounds.max.x;

                float height = minComponentHeight; //heightMap[vert.x, vert.y];

                height += .001f;

                //if this vert is actually on water go a little below the land
                if (terrainTileMap[vert.x, vert.y] == Terrain.Water)
                {
                    //heightMap[vert.x, vert.y] -= .01f;
                    heightMap[vert.x, vert.y] -= waterMap[vert.x, vert.y] * meshArgs.WaterScale;
                }

                worldPos += Vector3.up * height * (m_Terrain.terrainData.size.y);

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

    public float QuadLerp(float a, float b, float c, float d, float u, float v)
    {
        v = 1 - v;
        float abu = Mathf.Lerp(a, b, u);
        float dcu = Mathf.Lerp(d, c, u);
        return Mathf.Lerp(abu, dcu, v);
    }

    private float[,] ScaleHeightMapResolution(float[,] rawMap, float resolutionScale)
    {
        int rawMapSize = rawMap.GetUpperBound(0);
        int scaledMapSize = (int) (rawMapSize * resolutionScale);
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

            // flip x and y for the terrain
            scaledMap[y, x] = height;
        });

        return scaledMap;
    }
}
