using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LakeGenerator
{
    public static void Lake<T>(ref T[,] map,
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
            //Vector2Int.up + Vector2Int.left,
            //Vector2Int.down + Vector2Int.left,
            //Vector2Int.up + Vector2Int.right,
            //Vector2Int.down + Vector2Int.left,
        };

        int numWaterFlooded = 10000;
        int numIterations = 0;

        while (numWaterFlooded > (lakeMap.GetUpperBound(0) * .01f) && numIterations < 100)
        {
            numIterations++;
            numWaterFlooded = 0;
            // Make water flood lower areas
            for (int x = 0; x <= lakeMap.GetUpperBound(0); x++)
            {
                for (int y = 0; y <= lakeMap.GetUpperBound(1); y++)
                {
                    // skip this tile if it is water
                    if (map[x, y].Equals(currentTerrain))
                    {
                        continue;
                    }

                    FloodTile(x, y, currentTerrain, map, heightMap, floodFillDirections, ref numWaterFlooded);
                }
            }

            for (int x = lakeMap.GetUpperBound(0); x >= 0; x--)
            {
                for (int y = lakeMap.GetUpperBound(1); y >= 0; y--)
                {
                    // skip this tile if it is water
                    if (map[x, y].Equals(currentTerrain))
                    {
                        continue;
                    }

                    FloodTile(x, y, currentTerrain, map, heightMap, floodFillDirections, ref numWaterFlooded);
                }
            }

            Debug.Log($"num water flooded {numWaterFlooded}");
        }

        Debug.Log($"num iterations {numIterations}");


        // TODO store this value so we can use it later when doing water meshes
        List<HashSet<Vector2Int>> components = LayerMapFunctions.FindComponents(Terrain.Water, heightMap.GetUpperBound(0), ref baseTerrainMap);
        float averageComponentSize = 0;

        // flatten each water componenet to the level of its lowest point
        foreach (var componet in components)
        {
            averageComponentSize += componet.Count;
            float minHeight = 0;
            foreach (var pos in componet)
            {
                minHeight = Mathf.Max(heightMap[pos.x, pos.y], minHeight);
            }

            foreach (var pos in componet)
            {
                //heightMap[pos.x, pos.y] = minHeight;
            }
        }

        averageComponentSize /= components.Count();
    }

    public static void FloodTile<T>(int x, int y, T currentTerrain, T[,] map, float[,] heightMap, Vector2Int[] floodFillDirections, ref int numWaterFlooded)
    {
        bool hasAdjacentWater = false;
        float heighestAdjacentWater = -1;
        Vector2Int dirToWater;

        foreach (Vector2Int dir in floodFillDirections)
        {
            Vector2Int pos = new Vector2Int(x, y) + dir;
            if (LayerMapFunctions.InBounds(map, pos) && map[pos.x, pos.y].Equals(currentTerrain))
            {
                hasAdjacentWater = true;
                heighestAdjacentWater = Mathf.Max(heighestAdjacentWater, heightMap[pos.x, pos.y]);
                dirToWater = dir;
            }
        }

        if (hasAdjacentWater && heighestAdjacentWater >= heightMap[x, y])
        {
            numWaterFlooded++;
            map[x, y] = currentTerrain;
        }
    }
}
