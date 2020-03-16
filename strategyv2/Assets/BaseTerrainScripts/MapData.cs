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
    public int TileMapSize { get { return TerrainMap.SideLength; } }

    [SerializeField]
    public SquareArray<Terrain> TerrainMap;
    [SerializeField]
    public SquareArray<Improvement> ImprovmentMap;
    [SerializeField]
    public SquareArray<float> HeightMap;
    [SerializeField]
    public SquareArray<float> VertexHeightMap;
    [SerializeField]
    public SquareArray<float> WaterMap;
    [SerializeField]
    public SquareArray<Vector2> GradientMap;
    [SerializeField]
    public SquareArray<Vector2> LayeredGradientMap;

    // Cached componets
    [NonSerialized]
    private List<HashSet<Vector2Int>> m_LandComponents = null;
    public List<HashSet<Vector2Int>> LandComponents {
        get
        {
            if(m_LandComponents == null)
            {
                m_LandComponents = ArrayUtilityFunctions.FindComponents(Terrain.Grass, TerrainMap.SideLength, 0, ref TerrainMap);
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

    public void Clear()
    {
        m_LandComponents = null;
        RoadPaths = new List<List<Vector3>>();
    }

    public MapData Clone()
    {
        MapData other = new MapData
        {
            TerrainMap = TerrainMap.Clone() as SquareArray<Terrain>,
            ImprovmentMap = ImprovmentMap.Clone() as SquareArray<Improvement>,
            HeightMap = HeightMap.Clone() as SquareArray<float>,
            VertexHeightMap = VertexHeightMap.Clone() as SquareArray<float>,
            WaterMap = WaterMap.Clone() as SquareArray<float>,
            GradientMap = GradientMap.Clone() as SquareArray<Vector2>,
            LayeredGradientMap = LayeredGradientMap.Clone() as SquareArray<Vector2>,
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
