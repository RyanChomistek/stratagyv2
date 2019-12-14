using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class MapData
{
    [SerializeField]
    public int mapSize;
    [SerializeField]
    public int MeshHeightMapSize { get { return mapSize + 1; } }
    [SerializeField]
    public Terrain[,] TerrainMap;
    [SerializeField]
    public Improvement[,] ImprovmentMap;
    [SerializeField]
    public float[,] HeightMap;
    [SerializeField]
    public float[,] MeshHeightMap;
    [SerializeField]
    public float[,] WaterMap;
    [SerializeField]
    public float[,] RawWaterLevelMap;

    [SerializeField]
    public Vector2[,] GradientMap;
    [SerializeField]
    public Vector2[,] LayeredGradientMap;

    // Cached componets
    [NonSerialized]
    private List<HashSet<Vector2Int>> m_LandComponents = null;
    public List<HashSet<Vector2Int>> LandComponents {
        get
        {
            if(m_LandComponents == null)
            {
                m_LandComponents = LayerMapFunctions.FindComponents(Terrain.Grass, mapSize, 0, ref TerrainMap);
            }

            return m_LandComponents;
        }
        set
        {
            m_LandComponents = value;
        }
    }

    [SerializeField]
    public List<List<Vector3>> RoadPaths = new List<List<Vector3>>();

    private static List<Vector2Int> m_TileVertsOffsets = new List<Vector2Int>()
    {
        new Vector2Int(0,1), // Top left
        new Vector2Int(1,1), // Top right
        new Vector2Int(0,0), // Bottom left
        new Vector2Int(1,0), // Bottom Right
    };

    public void SetHeightMapData(int x, int y, float newHeight)
    {
        float currentHeight = HeightMap[x, y];
        float deltaHeight = newHeight - currentHeight;
        Vector2Int bottomLeftMeshVert = new Vector2Int(x, y);

        // distribute the change in height over all four verts
        foreach (Vector2Int vertOffset in m_TileVertsOffsets)
        {
            Vector2Int meshVert = bottomLeftMeshVert + vertOffset;
            MeshHeightMap[meshVert.x, meshVert.y] -= deltaHeight * .5f;
        }

        HeightMap[x, y] = newHeight;
    }

    public void Clear()
    {
        m_LandComponents = null;
        RoadPaths = new List<List<Vector3>>();
    }

    public MapData Clone()
    {
        MapData other = new MapData
        {
            mapSize = mapSize,
            TerrainMap = TerrainMap.Clone() as Terrain[,],
            ImprovmentMap = ImprovmentMap.Clone() as Improvement[,],
            HeightMap = HeightMap.Clone() as float[,],
            MeshHeightMap = MeshHeightMap.Clone() as float[,],
            WaterMap = WaterMap.Clone() as float[,],
            RawWaterLevelMap = RawWaterLevelMap.Clone() as float[,],
            GradientMap = GradientMap.Clone() as Vector2[,],
            LayeredGradientMap = LayeredGradientMap.Clone() as Vector2[,],
            RoadPaths = RoadPaths.Select(x => x.ToList()).ToList()
        };

        return other;
    }

    public static MapData LoadMapData(string fileName)
    {
        var sr = File.ReadAllText($"Assets/Saves/{fileName}");
        return JsonConvert.DeserializeObject<MapData>(sr);
    }

    public void SaveMapData(string fileName)
    {
        var MapDataJson = JsonConvert.SerializeObject(this);
        var sr = File.CreateText($"Assets/Saves/{fileName}");
        sr.WriteLine(MapDataJson);
        sr.Close();
    }
}
