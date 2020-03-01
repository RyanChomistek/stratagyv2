using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LakeGenerator
{
    public static void Lake<T>(ref T[,] map,
        MapData mapData,
        ref float[,] heightMap,
        ref float[,] lakeMap,
        ref Vector2[,] gradientMap,
        ref Terrain[,] baseTerrainMap,
        T currentTerrain,
        MapLayerSettings layerSetting)
    {
        LayerMapFunctions.SmoothMT(ref lakeMap, 5, 4);
        LayerMapFunctions.Normalize(ref lakeMap);

        for (int x = 0; x <= lakeMap.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= lakeMap.GetUpperBound(1); y++)
            {
                if (lakeMap[x, y] > layerSetting.WaterPercentThreshold &&
                   gradientMap[x, y].magnitude < layerSetting.MaxWaterGradient)
                {
                    map[x, y] = currentTerrain;
                }
            }
        }

        Vector2Int[] floodFillDirections = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
        };

        int numWaterFlooded = 10000;
        int numIterations = 0;

        // even out the water level
        while (numWaterFlooded > (lakeMap.GetUpperBound(0) * .01f) && numIterations < 4)
        {
            numIterations++;
            numWaterFlooded = 0;

            // Make water flood lower areas, go forwards and backwards to make sure we spread evenly
            for (int x = 0; x <= lakeMap.GetUpperBound(0); x++)
            {
                for (int y = 0; y <= lakeMap.GetUpperBound(1); y++)
                {
                    FloodTile(x, y, currentTerrain, mapData, map, floodFillDirections, ref numWaterFlooded);
                }
            }

            for (int x = lakeMap.GetUpperBound(0); x >= 0; x--)
            {
                for (int y = lakeMap.GetUpperBound(1); y >= 0; y--)
                {
                    FloodTile(x, y, currentTerrain, mapData, map, floodFillDirections, ref numWaterFlooded);
                }
            }

            Debug.Log($"num water flooded {numWaterFlooded}");
        }

        Debug.Log($"num iterations {numIterations}");

        // make the water go slightly down
        for (int x = 0; x <= heightMap.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= heightMap.GetUpperBound(1); y++)
            {
                if(baseTerrainMap[x,y] == Terrain.Water)
                {
                    mapData.SetHeightMapData(x, y, heightMap[x,y] + .01f);
                }
            }
        }
    }

    /// <summary>
    /// evens out the height map between this tile and its adjacents
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="currentTerrain"></param>
    /// <param name="mapData"></param>
    /// <param name="map"></param>
    /// <param name="floodFillDirections"></param>
    /// <param name="numWaterFlooded"></param>
    public static void FloodTile<T>(int x, int y, T currentTerrain, MapData mapData, T[,] map, Vector2Int[] floodFillDirections, ref int numWaterFlooded)
    {
        bool hasAdjacentWater = false;
        float heighestAdjacentWater = -1;
        float lowestAdjacentWater = 1000;
        Vector2Int dirToWater;

        foreach (Vector2Int dir in floodFillDirections)
        {
            Vector2Int pos = new Vector2Int(x, y) + dir;
            if (LayerMapFunctions.InBounds(map, pos) && map[pos.x, pos.y].Equals(currentTerrain))
            {
                hasAdjacentWater = true;
                heighestAdjacentWater = Mathf.Max(heighestAdjacentWater, mapData.HeightMap[pos.x, pos.y]);
                lowestAdjacentWater = Mathf.Min(lowestAdjacentWater, mapData.HeightMap[pos.x, pos.y]);
                dirToWater = dir;
            }
        }

        if (hasAdjacentWater && heighestAdjacentWater >= mapData.HeightMap[x, y])
        {
            numWaterFlooded++;
            map[x, y] = currentTerrain;
            //heightMap[x, y] = heighestAdjacentWater - .01f;
            mapData.SetHeightMapData(x, y, heighestAdjacentWater);
        }
    }
}
