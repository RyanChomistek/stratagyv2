using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomAStarTester : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        MapData mapData = LoadTestMapData();
        Vector2Int start = new Vector2Int(0, 0);
        Vector2Int target = new Vector2Int(61, 69);
        //Vector2Int target = new Vector2Int(25, 60);
        LayerMapFunctions.LogAction(() => CustomAStar.AStar(mapData, start, target, 
            (current, adjacent) => {
                return 0;
            }), "road time"); 
    }

    MapData LoadTestMapData()
    {
        return MapData.LoadMapData("MapDataJsonTest.mapdata");
        //return MapData.LoadMapData("CreateTestMapData.mapdata");
    }

    MapData CreateTestMapData()
    {
        MapData test = new MapData();

        int mapSize = 5;

        test.TerrainMap = new Terrain[mapSize, mapSize];
        test.ImprovmentMap = new Improvement[mapSize, mapSize];
        test.HeightMap = new float[mapSize, mapSize];
        test.WaterMap = new float[mapSize, mapSize];

        test.GradientMap = MapGenerator.CalculateGradients(test.HeightMap);

        test.SaveMapData("CreateTestMapData.mapdata");

        return test;
    }

    void PrintTestMap(MapData test)
    {
        string str = "";
        for (int row = test.TerrainMap.GetUpperBound(0); row >= 0 ; row--) 
        {
            str += row + ": ";

            for (int col = 0; col <= test.TerrainMap.GetUpperBound(1); col++)
            {
                str += test.TerrainMap[row, col] + ", ";
            }

            str += "\n";
        }

        Debug.Log(str);
    }
}
