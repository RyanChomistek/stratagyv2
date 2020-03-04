using PathCreation;
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

    public bool GenerateWaterMeshes= true;
    public bool GenerateTreeMeshes = true;
    public bool GenerateDetailMeshes = true;
    public bool GenerateRoadMeshes = true;

    public int GrassDensity = 100;
    public float TreeDensity = .5f;
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

    #region Road Mesh
    public PathCreator m_PathCreatorPrefab;
    private List<PathCreator> m_PathCreators = new List<PathCreator>();
    #endregion Road Mesh

    #region GridMesh
    [SerializeField]
    private GameObject m_GridMeshPrefab;
    private List<GameObject> m_GridMeshes = new List<GameObject>();

    [SerializeField]
    private GameObject m_GridMeshContainer;
    #endregion GridMesh

    void Awake()
    {
        Instance = this;
        m_Terrain.detailObjectDistance = 2000;
    }

    //TODO Block this into chuncks 
    public void ConstructMesh(MapData mapData, MeshGeneratorArgs otherArgs, Dictionary<Terrain, TerrainMapTile> terrainTileLookup)
    {
        TerrainData tData = m_Terrain.terrainData;

        float resolutionScale = tData.heightmapResolution / ((float) mapData.VertexHeightMap.GetUpperBound(0));
        float[,] scaledMap = ScaleHeightMapResolution(mapData.VertexHeightMap, resolutionScale);

        float alphaMapScale = tData.alphamapWidth / ((float)mapData.TerrainMap.GetUpperBound(0) + 1);
        
        m_Terrain.terrainData.SetHeights(0, 0, scaledMap);

        float[,,] alphaData = tData.GetAlphamaps(0, 0, tData.alphamapWidth, tData.alphamapHeight);

        int GRASS = 0;
        int WATER = 1;
        int ROCK = 2;
        int ROAD = 3;

        LayerMapFunctions.LogAction(() =>
        {
            for (int y = 0; y < tData.alphamapHeight; y++)
            {
                for (int x = 0; x < tData.alphamapWidth; x++)
                {
                    Vector2Int terrainMapPos = new Vector2Int(Mathf.FloorToInt(x / alphaMapScale), Mathf.FloorToInt(y / alphaMapScale));
                    Terrain terrain = mapData.TerrainMap[terrainMapPos.x, terrainMapPos.y];
                    Improvement improvement = mapData.ImprovmentMap[terrainMapPos.x, terrainMapPos.y];
                    Vector2 gradient = mapData.GradientMap[terrainMapPos.x, terrainMapPos.y];

                    if (terrain == Terrain.Water)
                    {
                        alphaData[y, x, GRASS] = 0;
                        alphaData[y, x, WATER] = 1;
                        alphaData[y, x, ROCK] = 0;
                        alphaData[y, x, ROAD] = 0;
                    }
                    else if (terrain == Terrain.Mountain)
                    {
                        alphaData[y, x, GRASS] = 0;
                        alphaData[y, x, WATER] = 0;
                        alphaData[y, x, ROCK] = 0;
                        alphaData[y, x, ROAD] = 1;
                    }
                    else
                    {
                        float rockyness = gradient.magnitude * 10;

                        alphaData[y, x, GRASS] = 1 - rockyness;
                        alphaData[y, x, WATER] = 0;
                        alphaData[y, x, ROCK] = rockyness;
                        alphaData[y, x, ROAD] = 0;
                    }


                    if (improvement == Improvement.Road)
                    {
                        alphaData[y, x, GRASS] = 0;
                        alphaData[y, x, WATER] = 0;
                        alphaData[y, x, ROCK] = 0;
                        alphaData[y, x, ROAD] = 1;
                    }
                }
            }
        }, "Set alpha maps");


        LayerMapFunctions.LogAction(() => {
            ConstructTrees(tData, mapData.ImprovmentMap, mapData.HeightMap, mapData.GradientMap, otherArgs);
            ConstructGrass(tData, mapData, otherArgs);
            ConstructRocks(tData, mapData, otherArgs);
        }, "Set Details");

        LayerMapFunctions.LogAction(() =>
        {
            tData.SetAlphamaps(0, 0, alphaData);
        }, "Save Alpha maps");
    }

    public void ConstructGridMesh(MapData mapdata)
    {
        m_GridMeshes.ForEach(x => DestroyImmediate(x));
        m_GridMeshes.Clear();

        float gridLineThickness = .1f;
        float gridHeight = 1;
        for(int x = 0; x < mapdata.mapSize; x++)
        {
            //draw a line from (x,1,0) to (x,1,mapsize)
            DrawGridLine(new Vector2(x, 0), new Vector2(x, mapdata.mapSize), gridHeight, gridLineThickness, mapdata);
        }

        for (int z = 0; z < mapdata.mapSize; z++)
        {
            //draw a line from (0,1,z) to (mapsize,1,z)
            DrawGridLine(new Vector2(0, z), new Vector2(mapdata.mapSize, z), gridHeight, gridLineThickness, mapdata);
        }
    }

    public void DrawGridLine(Vector2 start, Vector2 end, float height, float lineThickness, MapData mapdata)
    {
        GameObject meshPrefab = Instantiate(m_GridMeshPrefab);
        m_GridMeshes.Add(meshPrefab);

        //make the fill quad
        MeshFilter mf = meshPrefab.GetComponent<MeshFilter>();
        var mesh = new Mesh();
        mf.mesh = mesh;
        List<Vector3> verts = new List<Vector3>();

        Vector2 dir = (end - start).normalized;
        Vector2 tangent = new Vector2(-dir.y, dir.x);
        Vector3 scaledTangent = new Vector3(tangent.x, 0, tangent.y) * lineThickness;

        Vector3 start3 = new Vector3(start.x, height, start.y);
        Vector3 end3 = new Vector3(end.x, height, end.y);

        Vector3 startLeft = start3 - scaledTangent; // 0
        Vector3 startRight = start3 + scaledTangent; // 1

        Vector3 EndLeft = end3 - scaledTangent; // 2
        Vector3 EndRight = end3 + scaledTangent; // 3

        verts.Add(ConvertTilePositionToWorldPosition(startLeft, mapdata.mapSize)); // 0
        verts.Add(ConvertTilePositionToWorldPosition(startRight, mapdata.mapSize)); // 1
        verts.Add(ConvertTilePositionToWorldPosition(EndLeft, mapdata.mapSize)); // 2
        verts.Add(ConvertTilePositionToWorldPosition(EndRight, mapdata.mapSize)); // 3

        List<int> tri = new List<int>() {
            0,1,2,
            1,3,2
        };

        mesh.vertices = verts.ToArray();
        mesh.triangles = tri.ToArray();

        meshPrefab.transform.SetParent(m_GridMeshContainer.transform);
    }

    public void ConstructRoadMeshes(MapData mapdata)
    {
        TerrainData tData = m_Terrain.terrainData;
        float alphaMapScale = tData.alphamapWidth / (float)mapdata.mapSize;

        m_PathCreators.ForEach(x =>
        {
            if(x != null)
            {
                var GO = x.gameObject;
                x.GetComponent<RoadMeshCreator>().OnDestroy();
                DestroyImmediate(x.GetComponent<RoadMeshCreator>());
                DestroyImmediate(GO);
            }
        });

        m_PathCreators.Clear();
        
        List<List<Vector3>> scaledPaths = new List<List<Vector3>>();

        float heightOffset = 1f;

        foreach (var path in mapdata.RoadPaths)
        {
            // Scale paths to that of the terrain
            List<Vector3> scaledPath = new List<Vector3>();
            foreach(Vector3 point in path)
            {
                var point2D = new Vector2(point.x, point.z);
                var point2DOffset = point2D + (Vector2.up + Vector2.right) / 2;
                Vector2 scaledPoint = ConvertTilePositionToWorldPosition(point2DOffset, mapdata.mapSize);

                scaledPath.Add(new Vector3(
                            scaledPoint.x,
                            point.y * m_Terrain.terrainData.size.y + heightOffset,
                            scaledPoint.y));
            }

            PathCreator pathCreator = Instantiate(m_PathCreatorPrefab);
            pathCreator.bezierPath = new BezierPath(scaledPath, false, PathSpace.xyz);
            pathCreator.transform.SetParent(transform);
            pathCreator.GetComponent<RoadMeshCreator>().TriggerUpdate();
            m_PathCreators.Add(pathCreator);
        }
    }

    public float GetMeshMapSize()
    {
        return m_Terrain.terrainData.bounds.max.x;
    }

    public float GetHeightAtWorldPosition(Vector3 worldPosition)
    {
        return m_Terrain.SampleHeight(worldPosition);
    }

    public Vector2Int ConvertWorldPositionToTilePosition(Vector3 worldPosition, MapData mapData)
    {
        float scale = m_Terrain.terrainData.bounds.max.x / mapData.mapSize;
        return new Vector2Int(Mathf.FloorToInt(worldPosition.x / scale), Mathf.FloorToInt(worldPosition.z / scale));
    }

    public Vector3 ConvertTilePositionToWorldPosition(Vector3 pos, int mapSize)
    {
        float scale = m_Terrain.terrainData.bounds.max.x / mapSize;
        return new Vector3(pos.x * scale, pos.y * m_Terrain.terrainData.size.y, pos.z * scale);
    }

    public Vector2 ConvertTilePositionToWorldPosition(Vector2 pos, int mapSize)
    {
        float scale = m_Terrain.terrainData.bounds.max.x / mapSize;
        return new Vector2(pos.x * scale, pos.y * scale);
    }

    public void ConstructTrees(TerrainData tData, Improvement[,] improvementMap, float[,] heightMap, Vector2[,] gradientMap, MeshGeneratorArgs otherArgs)
    {
        List<TreeInstance> trees = new List<TreeInstance>();
        Vector3 tileSize = new Vector3(1 / (float)improvementMap.GetUpperBound(0), 0, 1 / (float)improvementMap.GetUpperBound(1));
        float nudgeRadius = .25f;
        float scaleNudgeBase = .2f;

        //TreeDensity
        //int numTreeIterations = (int)TreeDensity < 1 ? 1 : (int)TreeDensity;
        if (otherArgs.GenerateTreeMeshes)
        {
            for (int x = 0; x <= improvementMap.GetUpperBound(0); x++)
            {
                for (int y = 0; y <= improvementMap.GetUpperBound(1); y++)
                {
                    if (improvementMap[x, y] == Improvement.Forest && UnityEngine.Random.value < otherArgs.TreeDensity)
                    {
                        // Move the tree a little so that we dont get a grid
                        Vector3 nudge =
                            new Vector3(
                                UnityEngine.Random.Range(-nudgeRadius, nudgeRadius) * tileSize.x,
                                0,
                                UnityEngine.Random.Range(-nudgeRadius, nudgeRadius) * tileSize.z);

                        Vector3 treePos =
                            new Vector3(
                                x / (float)improvementMap.GetUpperBound(0),
                                heightMap[x, y],
                                y / (float)improvementMap.GetUpperBound(1)) + nudge;

                        TreeInstance tree = new TreeInstance();
                        tree.color = Color.white;

                        float scaleNudge = UnityEngine.Random.Range(-scaleNudgeBase, scaleNudgeBase);
                        tree.heightScale = .5f + scaleNudge;
                        tree.widthScale = .5f + scaleNudge;

                        tree.lightmapColor = Color.white;
                        tree.position = treePos;
                        tree.prototypeIndex = 0;
                        tree.widthScale = 1;
                        trees.Add(tree);
                    }
                }
            }
        }

        Debug.Log("trees placed: " + trees.Count);
        tData.treeInstances = trees.ToArray();
    }

    private Vector2Int ConvertDetailPositionToTilePosition(TerrainData tData, MapData mapData, Vector2Int detailPos, MeshGeneratorArgs otherArgs)
    {
        Vector2 positionScaled = detailPos;
        positionScaled.x /= tData.detailWidth;
        positionScaled.y /= tData.detailHeight;
        positionScaled *= mapData.mapSize;
        return LayerMapFunctions.FloorVector(positionScaled);
    }

    public void ConstructGrass(TerrainData tData, MapData mapData, MeshGeneratorArgs otherArgs)
    {
        ConstructDetailLayer(tData, mapData, otherArgs, 0, (tilePosition) =>
        {
            Improvement improvement = mapData.ImprovmentMap[tilePosition.x, tilePosition.y];
            bool acceptableImprovement = improvement == Improvement.Empty || improvement == Improvement.Forest;
            // make sure there are no improvements on the tile and that its grass
            if (mapData.TerrainMap[tilePosition.x, tilePosition.y] == Terrain.Grass)
            {
                if(improvement == Improvement.Empty)
                    return otherArgs.GrassDensity;
                if (improvement == Improvement.Forest)
                    return otherArgs.GrassDensity / 10;
            }

            return 0;
        });
    }

    public void ConstructRocks(TerrainData tData, MapData mapData, MeshGeneratorArgs otherArgs)
    {
        ConstructDetailLayer(tData, mapData, otherArgs, 1, (tilePosition) =>
        {
            // make sure there are no improvements on the tile and that its grass
            if (mapData.TerrainMap[tilePosition.x, tilePosition.y] == Terrain.Mountain)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tData"></param>
    /// <param name="mapData"></param>
    /// <param name="otherArgs"></param>
    /// <param name="layer"></param>
    /// <param name="densityFunc"> function to get the density of the detail given a tile location </param>
    public void ConstructDetailLayer(TerrainData tData, MapData mapData, MeshGeneratorArgs otherArgs, int layer, Func<Vector2Int, int> densityFunc)
    {
        

        // Get all of layer.
        var map = tData.GetDetailLayer(0, 0, tData.detailWidth, tData.detailHeight, layer);

        // For each pixel in the detail map...
        for (int y = 0; y < tData.detailHeight; y++)
        {
            for (int x = 0; x < tData.detailWidth; x++)
            {
                if (otherArgs.GenerateDetailMeshes)
                {
                    // Here we do y, x because the details coordinates are flipped from ours
                    Vector2Int tilePosition = ConvertDetailPositionToTilePosition(tData, mapData, new Vector2Int(y, x), otherArgs);
                    map[x, y] = densityFunc(tilePosition);
                }
                else
                {
                    map[x, y] = 0;
                }
                
            }
        }

        // Assign the modified map back.
        tData.SetDetailLayer(0, 0, layer, map);
    }

    public void ConstructWaterMeshes(MapData mapData, MeshGeneratorArgs meshArgs, float[,] heightMap, float[,] waterMap, Terrain[,] terrainTileMap, MeshGeneratorArgs otherArgs)
    {
        if (!otherArgs.GenerateWaterMeshes)
        {
            return;
        }

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
            ConstructWaterMeshFromComponent(mapData, componet, meshArgs, ref heightMap, waterClone, ref terrainTileMap);
        }

        Debug.Log($"{components.Count} unique water zones");
    }

    public void ConstructWaterMeshFromComponent(MapData mapData, HashSet<Vector2Int> componet, MeshGeneratorArgs meshArgs, ref float[,] heightMap, float[,] waterMap, ref Terrain[,] terrainTileMap)
    {
        List<Vector3> verts = new List<Vector3>();
        Dictionary<Vector2, int> vertToIndexMap = new Dictionary<Vector2, int>();
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

        //Debug.Log($"average componenet water height {sumHeight / componet.Count}");

        List<Vector2Int> tileVertsOffsets = new List<Vector2Int>()
        {
            new Vector2Int(0,1), // Top left
            new Vector2Int(1,1), // Top right
            new Vector2Int(0,0), // Bottom left
            new Vector2Int(1,0), // Bottom Right
        };

        foreach (Vector2Int index in componet)
        {
            int tileVertStart = verts.Count;
            foreach(Vector2Int vertOffset in tileVertsOffsets)
            {
                int vertIndex = verts.Count;
                Vector2Int meshIndex = vertOffset + index;
                Vector2 uv = new Vector2(meshIndex.x / ((float) meshSize), meshIndex.y / ((float) meshSize));
                Vector3 vert = new Vector3(uv.x, 0, uv.y) * m_Terrain.terrainData.bounds.max.x;
                vert += Vector3.up * minHeight * (m_Terrain.terrainData.size.y);
                verts.Add(vert);
                uvs.Add(uv);
            }

            // generate the tri indexes
            List<int> quad = new List<int>() {
                0,1,2,
                1,3,2
            };

            foreach(var i in quad)
            {
                triangles.Add(tileVertStart + i);
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

    /// <summary>
    /// calculates the bilinear interpolation from the 4 points given u,v
    /// </summary>
    /// <param name="a"> top left </param>
    /// <param name="b"> top right </param>
    /// <param name="c"> bottom right </param>
    /// <param name="d"> bottom left </param>
    /// <param name="u"> horz position </param>
    /// <param name="v"> vert position </param>
    /// <returns></returns>
    public static float QuadLerp(float a, float b, float c, float d, float u, float v)
    {
        v = 1 - v;
        float abu = Mathf.Lerp(a, b, u);
        float dcu = Mathf.Lerp(d, c, u);
        return Mathf.Lerp(abu, dcu, v);
    }

    private float[,] ScaleHeightMapResolution(float[,] rawMap, float resolutionScale)
    {
        // This is intentionaly left as (count - 1), because in the calculation we dont look a the last element
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
        }, 1);

        return scaledMap;
    }
}
