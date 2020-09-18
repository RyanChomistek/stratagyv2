using PathCreation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class MeshGeneratorArgs
{
    public float ElevationScale = 10;
    public float WaterScale = .1f;

    public bool GenerateWaterMeshes = true;
    public bool GenerateTreeMeshes = true;
    public bool GenerateDetailMeshes = true;
    public bool GenerateRoadMeshes = true;

    public int GrassDensity = 100;
    public float TreeDensity = .5f;

    // if this is false we will attempt to create lakes out of the individual water components
    public bool UseWaterPlane = true;

    public bool GenerateMillMeshes = true;
    public float MillDensityPerTile = .01f;

    public Vector3 MeshSize;
}

public class TerrainMeshGenerator : MonoBehaviour
{
    public static TerrainMeshGenerator Instance;

    #region Terrain Mesh
    [SerializeField]
    private UnityEngine.Terrain m_Terrain;
    #endregion Terrain Mesh

    #region Water Meshes
    [SerializeField]
    private GameObject m_WaterPlane = null;
    [SerializeField]
    private GameObject m_WaterTilePrefab;


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

    [SerializeField]
    private GameObject m_MillPrefab;
    private List<GameObject> m_DetailMeshes = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        m_Terrain.detailObjectDistance = 2000;
        m_Terrain.treeDistance = 2000;
        ClearDetailMeshes();
    }

    //TODO Block this into chuncks 
    public void ConstructMesh(MapData mapData, MeshGeneratorArgs otherArgs, Dictionary<Terrain, TerrainMapTile> terrainTileLookup)
    {
        TerrainData tData = m_Terrain.terrainData;

        SquareArray<float> scaledMap = ScaleHeightMapResolution(mapData.VertexHeightMap, tData);
        m_Terrain.terrainData.SetHeights(0, 0, scaledMap.To2D());

        float alphaMapScale = tData.alphamapWidth / ((float)mapData.TerrainMap.SideLength);
        float[,,] alphaData = tData.GetAlphamaps(0, 0, tData.alphamapWidth, tData.alphamapHeight);

        int numThreads = 16;

        SquareArray<AlphaMapChannels> channelsMap = new SquareArray<AlphaMapChannels>(tData.alphamapWidth);
        AlphaMapChannels[] neighborChannels = new AlphaMapChannels[numThreads];

        ProfilingUtilities.LogAction(() =>
        {
            for (int y = 0; y < tData.alphamapHeight; y++)
            {
                for (int x = 0; x < tData.alphamapWidth; x++)
                {
                    channelsMap[x, y] = new AlphaMapChannels();
                }
            }

            for (int i = 0; i < numThreads; i++)
            {
                neighborChannels[i] = new AlphaMapChannels();
            }
        }, "Setup channels maps");

        ProfilingUtilities.LogAction(() =>
        {
            int tileLookDistance = 2;
            float maxDistance = alphaMapScale * tileLookDistance;

            ArrayUtilityFunctions.ForMT(tData.alphamapHeight, (threadId, x, y) =>
            {
                Vector2Int terrainMapPos = new Vector2Int(Mathf.FloorToInt(x / alphaMapScale), Mathf.FloorToInt(y / alphaMapScale));
                Vector2Int alphaMapPos = new Vector2Int(x, y);
                AlphaMapChannels ac = channelsMap[x, y];
                AlphaMapChannels neighborAc = neighborChannels[threadId];
                Vector2 alphaMapPositionReal = VectorUtilityFunctions.Vec2IntToVec2(alphaMapPos);

                // loop through all of the neightbor tiles
                for (int dx = -tileLookDistance; dx <= tileLookDistance; dx++)
                {
                    for (int dy = -tileLookDistance; dy <= tileLookDistance; dy++)
                    {
                        Vector2Int neighborTerrainMapPos = terrainMapPos + new Vector2Int(dx, dy);
                        if (mapData.TerrainMap.InBounds(neighborTerrainMapPos))
                        {
                            Vector2 neighborTileAphaMapPosCenter = VectorUtilityFunctions.Vec2IntToVec2(neighborTerrainMapPos) * alphaMapScale + new Vector2(alphaMapScale, alphaMapScale) / 2;
                            float distance = (neighborTileAphaMapPosCenter - alphaMapPositionReal).magnitude;
                            float weight = 1 - (distance / maxDistance);
                            if (weight > 0)
                            {
                                neighborAc.Set(mapData.TerrainMap[neighborTerrainMapPos], mapData.ImprovmentMap[neighborTerrainMapPos], mapData.GradientMap[neighborTerrainMapPos]);
                                neighborAc.Scale(weight);
                                ac.Add(neighborAc);
                            }
                        }
                    }
                }

                ac.Normalize();
                ac.SetAlphaMapPos(alphaData, x, y);
            }, numThreads);

        }, "Set alpha maps");

        ClearDetailMeshes();

        ProfilingUtilities.LogAction(() => {
            ConstructTrees(tData, mapData, otherArgs);
            //ConstructGrass(tData, mapData, otherArgs);
            ConstructFarms(tData, mapData, otherArgs);
            ConstructMills(tData, mapData, otherArgs);
            ConstructHouses(tData, mapData, otherArgs);
            //ConstructRocks(tData, mapData, otherArgs);
            ConstructRoadMeshes(mapData, otherArgs);
        }, "Set Details");

        ProfilingUtilities.LogAction(() =>
        {
            tData.SetAlphamaps(0, 0, alphaData);
        }, "Save Alpha maps");
    }

    public static AlphaMapChannels GetInterpolateAphaChannels(Vector2Int alphaMapPos, Vector2Int terrainMapPos, MapData mapData, float alphaMapScale)
    {
        AlphaMapChannels ac = new AlphaMapChannels();
        Vector2 alphaMapPositionReal = VectorUtilityFunctions.Vec2IntToVec2(alphaMapPos);
        int tileLookDistance = 1;
        float maxDistance = alphaMapScale * tileLookDistance;

        // loop through all of the neightbor tiles
        for (int dx = -tileLookDistance; dx <= tileLookDistance; dx++)
        {
            for (int dy = -tileLookDistance; dy <= tileLookDistance; dy++)
            {
                Vector2Int neighborTerrainMapPos = terrainMapPos + new Vector2Int(dx, dy);
                if (mapData.TerrainMap.InBounds(neighborTerrainMapPos))
                {
                    Vector2 neighborTileAphaMapPosCenter = VectorUtilityFunctions.Vec2IntToVec2(neighborTerrainMapPos) * alphaMapScale + new Vector2(alphaMapScale, alphaMapScale);
                    float distance = (neighborTileAphaMapPosCenter - alphaMapPositionReal).magnitude;
                    float weight = 1 - (distance / maxDistance);
                    if (weight > 0)
                    {
                        AlphaMapChannels neighborAc = new AlphaMapChannels(mapData.TerrainMap[neighborTerrainMapPos], mapData.ImprovmentMap[neighborTerrainMapPos], mapData.GradientMap[neighborTerrainMapPos]);
                        neighborAc.Scale(weight);
                        ac.Add(neighborAc);
                    }
                }
            }
        }

        ac.Normalize();
        return ac;
    }

    public void ClearDetailMeshes()
    {
        m_DetailMeshes.ForEach(x =>
        {
            if (x != null)
            {
                DestroyImmediate(x);
            }
        });
        m_DetailMeshes.Clear();

        List<InfiniteRotator> rotators = FindObjectsOfType<InfiniteRotator>().ToList();

        rotators.ForEach(x => { DestroyImmediate(x.gameObject); });

        List<RoadMeshCreator> rmcs = FindObjectsOfType<RoadMeshCreator>().ToList();

        rmcs.ForEach(x => {
            if (x != null)
            {
                var GO = x.gameObject;
                x.GetComponent<RoadMeshCreator>().OnDestroy();
                DestroyImmediate(x.GetComponent<RoadMeshCreator>());
                DestroyImmediate(GO);
            }
        });

        m_PathCreators.Clear();
    }

    public void ConstructGridMesh(MapData mapdata)
    {
        m_GridMeshes.ForEach(x => DestroyImmediate(x));
        m_GridMeshes.Clear();

        float gridLineThickness = .1f;
        float gridHeight = 1;
        for(int x = 0; x < mapdata.TileMapSize; x++)
        {
            //draw a line from (x,1,0) to (x,1,mapsize)
            DrawGridLine(new Vector2(x, 0), new Vector2(x, mapdata.TileMapSize), gridHeight, gridLineThickness, mapdata);
        }

        for (int z = 0; z < mapdata.TileMapSize; z++)
        {
            //draw a line from (0,1,z) to (mapsize,1,z)
            DrawGridLine(new Vector2(0, z), new Vector2(mapdata.TileMapSize, z), gridHeight, gridLineThickness, mapdata);
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

        verts.Add(ConvertTilePositionToWorldPosition(startLeft, mapdata.TileMapSize)); // 0
        verts.Add(ConvertTilePositionToWorldPosition(startRight, mapdata.TileMapSize)); // 1
        verts.Add(ConvertTilePositionToWorldPosition(EndLeft, mapdata.TileMapSize)); // 2
        verts.Add(ConvertTilePositionToWorldPosition(EndRight, mapdata.TileMapSize)); // 3

        List<int> tri = new List<int>() {
            0,1,2,
            1,3,2
        };

        mesh.vertices = verts.ToArray();
        mesh.triangles = tri.ToArray();

        meshPrefab.transform.SetParent(m_GridMeshContainer.transform);
    }

    public void ConstructRoadMeshes(MapData mapdata, MeshGeneratorArgs otherArgs)
    {
        TerrainData tData = m_Terrain.terrainData;
        float alphaMapScale = tData.alphamapWidth / (float)mapdata.TileMapSize;

        if(!otherArgs.GenerateRoadMeshes)
        {
            return;
        }

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
                Vector2 scaledPoint = ConvertTilePositionToWorldPosition(point2DOffset, mapdata.TileMapSize);

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

    public Vector3 GetWorldPositionFromTilePosition(Vector2 position, int mapSize, bool center = false)
    {
        Vector3 tilePos = new Vector3(position.x, 0, position.y);
        Vector3 worldPos = ConvertTilePositionToWorldPosition(tilePos, mapSize, center);
        float height = GetHeightAtWorldPosition(worldPos);
        worldPos.y = height;

        return worldPos;
    }

    public Vector2Int ConvertWorldPositionToTilePosition(Vector3 worldPosition, MapData mapData)
    {
        float scale = m_Terrain.terrainData.bounds.max.x / mapData.TileMapSize;
        return new Vector2Int(Mathf.FloorToInt(worldPosition.x / scale), Mathf.FloorToInt(worldPosition.z / scale));
    }

    public Vector3 ConvertTilePositionToWorldPosition(Vector3 pos, int mapSize, bool center = false)
    {
        float scale = m_Terrain.terrainData.bounds.max.x / mapSize;
        var worldPos = new Vector3(pos.x * scale, pos.y * m_Terrain.terrainData.size.y, pos.z * scale);

        if(center)
        {
            worldPos += new Vector3(.5f, 0, .5f) * scale;
        }

        return worldPos;
    }

    public Vector2 ConvertTilePositionToWorldPosition(Vector2 pos, int mapSize)
    {
        float scale = m_Terrain.terrainData.bounds.max.x / mapSize;
        return new Vector2(pos.x * scale, pos.y * scale);
    }

    public void ConstructTrees(TerrainData tData, MapData md, MeshGeneratorArgs otherArgs)
    {
        List<TreeInstance> trees = new List<TreeInstance>();
        //float tileScaleToWorld = m_Terrain.terrainData.bounds.max.x / md.ImprovmentMap.SideLength;
        Vector3 tileScaleToNormalized = new Vector3(1 / (float)md.ImprovmentMap.SideLength, 0, 1 / (float)md.ImprovmentMap.SideLength);
        float nudgeScale = .25f;
        float scaleNudgeBase = .25f;

        if (otherArgs.GenerateTreeMeshes)
        {
            for (int x = 0; x < md.ImprovmentMap.SideLength; x++)
            {
                for (int y = 0; y < md.ImprovmentMap.SideLength; y++)
                {
                    if (md.ImprovmentMap[x, y] == Improvement.Forest)
                    {
                        for (int i = 0; i < otherArgs.TreeDensity; i++)
                        {
                            // Move the tree a little so that we dont get a grid
                            Vector3 nudge =
                                new Vector3(
                                    UnityEngine.Random.Range(-tileScaleToNormalized.x, tileScaleToNormalized.x) * nudgeScale,
                                    0,
                                    UnityEngine.Random.Range(-tileScaleToNormalized.z, tileScaleToNormalized.z) * nudgeScale);

                            Vector3 treePos = new Vector3(
                                    x / (float)md.ImprovmentMap.SideLength,
                                    0,
                                    y / (float)md.ImprovmentMap.SideLength) + nudge;

                            // Find closes vertex (do this because we need a higher resolution height than tiles can provide)
                            float vertexScale = md.VertexHeightMap.SideLength / (float)md.HeightMap.SideLength;
                            Vector2Int vertexIndex = VectorUtilityFunctions.RoundVector(new Vector2(x, y) * vertexScale);
                            treePos.y = md.VertexHeightMap[vertexIndex];

                            TreeInstance tree = new TreeInstance();
                            //tree.color = Color.white;

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
        }

        Debug.Log("trees placed: " + trees.Count);
        tData.treeInstances = trees.ToArray();
    }

    private Vector2Int ConvertDetailPositionToTilePosition(TerrainData tData, MapData mapData, Vector2Int detailPos, MeshGeneratorArgs otherArgs)
    {
        Vector2 positionScaled = detailPos;
        positionScaled.x /= tData.detailWidth;
        positionScaled.y /= tData.detailHeight;
        positionScaled *= mapData.TileMapSize;
        return VectorUtilityFunctions.FloorVector(positionScaled);
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
                if (improvement == Improvement.Empty)
                {
                    float numItems = 1;
                    float weightPerItem = 1 / numItems;

                    float gradientWeight = Mathf.Max(0, weightPerItem * (1- mapData.GradientMap[tilePosition.x, tilePosition.y].magnitude * 10));
                    //float waterWeight = weightPerItem - mapData.WaterMap[tilePosition.x, tilePosition.y];
                    //float elevationWeight = weightPerItem * (1 - mapData.HeightMap[tilePosition.x, tilePosition.y]);

                    float combinationWeight = gradientWeight;

                    return Mathf.CeilToInt(otherArgs.GrassDensity * combinationWeight);
                }
                    
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
    
    public void ConstructMills(TerrainData tData, MapData mapData, MeshGeneratorArgs otherArgs)
    {
        List<HashSet<Vector2Int>> components = ArrayUtilityFunctions.FindComponents(Improvement.Farm, 0, mapData.ImprovmentMap);

        foreach (HashSet<Vector2Int> component in components)
        {
            int numMills = Mathf.FloorToInt(component.Count * otherArgs.MillDensityPerTile);
            if (numMills != 0)
            {
                List<Vector2Int> componentList = component.ToList();
                for(int i = 0; i < numMills; i++)
                {
                    Vector2 tilePos = componentList[UnityEngine.Random.Range(0, componentList.Count)];
                    var worldPos = GetWorldPositionFromTilePosition(tilePos, mapData.HeightMap.SideLength, true);
                    var millObject = Instantiate(m_MillPrefab, worldPos, Quaternion.identity, transform);
                    m_DetailMeshes.Add(millObject);
                }
            }
        }
    }

    public void ConstructFarms(TerrainData tData, MapData mapData, MeshGeneratorArgs otherArgs)
    {
        ConstructDetailLayer(tData, mapData, otherArgs, 2, (tilePosition) =>
        {
            // make sure there are no improvements on the tile and that its grass
            if (mapData.ImprovmentMap.InBounds(tilePosition) && mapData.ImprovmentMap[tilePosition.x, tilePosition.y] == Improvement.Farm)
            {
                return otherArgs.GrassDensity;
            }
            else
            {
                return 0;
            }
        });
    }

    public void ConstructHouses(TerrainData tData, MapData mapData, MeshGeneratorArgs otherArgs)
    {
        ConstructDetailLayer(tData, mapData, otherArgs, 3, (tilePosition) =>
        {
            // make sure there are no improvements on the tile and that its grass
            if (mapData.ImprovmentMap.InBounds(tilePosition) && mapData.ImprovmentMap[tilePosition.x, tilePosition.y] == Improvement.Town)
            {
                return mapData.TerrainMap.SideLength / m_Terrain.terrainData.bounds.max.x;
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
    public void ConstructDetailLayer(TerrainData tData, MapData mapData, MeshGeneratorArgs otherArgs, int layer, Func<Vector2Int, float> densityFunc)
    {
        // Get all of layer.
        int[,] map = tData.GetDetailLayer(0, 0, tData.detailWidth, tData.detailHeight, layer);
        int tileLookDistance = 1;
        
        float detailMapScale = tData.detailWidth / ((float)mapData.TerrainMap.SideLength + 1);
        float maxDistance = detailMapScale * tileLookDistance * .75f;

        // For each pixel in the detail map...
        for (int y = 0; y < tData.detailHeight; y++)
        {
            for (int x = 0; x < tData.detailWidth; x++)
            {
                if (otherArgs.GenerateDetailMeshes)
                {
                    // Here we do y, x because the details coordinates are flipped from ours
                    Vector2Int tilePosition = ConvertDetailPositionToTilePosition(tData, mapData, new Vector2Int(y, x), otherArgs);
                    Vector2 detailPos = new Vector2(y, x);
                    float value = 0;

                    for (int dx = -tileLookDistance; dx <= tileLookDistance; dx++)
                    {
                        for (int dy = -tileLookDistance; dy <= tileLookDistance; dy++)
                        {
                            Vector2Int neighborTerrainMapPos = tilePosition + new Vector2Int(dx, dy);
                            Vector2 neighborTileAphaMapPosCenter = VectorUtilityFunctions.Vec2IntToVec2(neighborTerrainMapPos) * detailMapScale + new Vector2(detailMapScale, detailMapScale) / 2;
                            float distance = (neighborTileAphaMapPosCenter - detailPos).magnitude;
                            float weight = 1 - (distance / maxDistance);
                            
                            if (weight > 0)
                            {
                                value += densityFunc(neighborTerrainMapPos) * weight;
                            }
                        }
                    }

                    // if the value is less than one we need to randomly select if it should get a value
                    if(value < 1 && UnityEngine.Random.Range(0f,1f) < value)
                    {
                        value = 1;
                    }

                    map[x, y] = Mathf.FloorToInt(value);
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

    public void ConstructWaterPlaneMesh(MapData mapData, MeshGeneratorArgs meshArgs)
    {
        // Clear any old water meshes
        m_WaterMeshes.ForEach(x => DestroyImmediate(x));
        m_WaterMeshes.Clear();

        if (!meshArgs.GenerateWaterMeshes)
        {
            return;
        }

        //m_WaterPlane.transform.localScale = new Vector3(m_Terrain.terrainData.bounds.max.x, m_Terrain.terrainData.bounds.max.x, m_Terrain.terrainData.bounds.max.x) / 100;
        UnityStandardAssets.Water.PlanarReflection reflection = m_WaterPlane.GetComponent<UnityStandardAssets.Water.PlanarReflection>();
        UnityStandardAssets.Water.WaterBase waterBase = m_WaterPlane.GetComponent<UnityStandardAssets.Water.WaterBase>();

        m_WaterPlane.transform.position = Vector3.zero;

        int waterTileSize = 50;
        for (int x = 0; x < m_Terrain.terrainData.bounds.max.x; x += waterTileSize)
        {
            for (int z = 0; z < m_Terrain.terrainData.bounds.max.z; z += waterTileSize)
            {
                UnityStandardAssets.Water.WaterTile waterTile = GameObject.Instantiate(m_WaterTilePrefab).GetComponent<UnityStandardAssets.Water.WaterTile>();
                waterTile.reflection = reflection;
                waterTile.waterBase = waterBase;

                //m_Terrain.terrainData.bounds.max.y
                waterTile.transform.parent = waterBase.transform;
                waterTile.transform.localPosition = new Vector3(x + waterTileSize/2, 0, z + waterTileSize / 2);
                
                m_WaterMeshes.Add(waterTile.gameObject);
            }
        }
        
        m_WaterPlane.transform.position = new Vector3(0, mapData.WaterHeight * (m_Terrain.terrainData.size.y), 0);
    }

    public void ConstructLakeMeshes(MapData mapData, MeshGeneratorArgs meshArgs, SquareArray<float> heightMap, SquareArray<float> waterMap, SquareArray<Terrain> terrainTileMap, MeshGeneratorArgs otherArgs)
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
        ProfilingUtilities.LogAction(() => {
            components = ArrayUtilityFunctions.FindComponents(Terrain.Water, 2, terrainTileMap);
        }, "create components time");

        SquareArray<float> waterClone = waterMap.Clone() as SquareArray<float>;

        ArrayUtilityFunctions.Normalize(waterClone);

        foreach(var componet in components)
        {
            ConstructWaterMeshFromComponent(mapData, componet, meshArgs, ref heightMap, waterClone, ref terrainTileMap);
        }

        Debug.Log($"{components.Count} unique water zones");
    }

    public void ConstructWaterMeshFromComponent(MapData mapData, HashSet<Vector2Int> componet, MeshGeneratorArgs meshArgs, ref SquareArray<float> heightMap, SquareArray<float> waterMap, ref SquareArray<Terrain> terrainTileMap)
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        int meshSize = heightMap.SideLength;

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

        minHeight += .05f;

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

    private SquareArray<float> ScaleHeightMapResolution(SquareArray<float> rawMap, TerrainData tData)
    {
        float resolutionScale = tData.heightmapResolution / ((float)rawMap.SideLength - 1);

        // This is intentionaly left as (count - 1), because in the calculation we dont look a the last element
        int scaledMapSize = tData.heightmapResolution;
        SquareArray<float> scaledMap = new SquareArray<float>(scaledMapSize);
        float maxDistance = resolutionScale * Mathf.Sqrt(2);

        List<Vector2> interpolationDirections = new List<Vector2>()
        {
            Vector2.left, Vector2.right, Vector2.up, Vector2.down
        };

        ArrayUtilityFunctions.ForMT(scaledMap, (x, y) =>
        {
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
                heights[i] = rawMap[rawIndexes[i]];
            }

            Vector2 uv = rawPosition - rawIndex;
            float height = ArrayUtilityFunctions.QuadLerp(heights[0], heights[1], heights[2], heights[3], uv.x, uv.y);

            // flip x and y for the terrain
            scaledMap[y, x] = height;
        }, 1);

        return scaledMap;
    }
}
