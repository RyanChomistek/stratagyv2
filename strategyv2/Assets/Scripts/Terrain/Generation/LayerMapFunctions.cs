﻿using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Threading.Tasks;
using System.Linq;

public class LayerMapFunctions : MonoBehaviour
{
    public const int DefaultNumThreadsForJob = 16;

    public static T[,] GenerateArray<T>(int width, int height, T defaultValue)
    {
        T[,] map = new T[width, height];

        for (int x = 0; x <= map.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++)
            {
                map[x, y] = defaultValue;
            }
        }
        return map;
    }

    /// <summary>
    /// fills the tiles within a given height range to be a specified tile
    /// </summary>
    /// <param name="map"></param>
    /// <param name="heightMap"></param>
    /// <param name="currentTerrain"></param>
    /// <param name="minHeight"></param>
    /// <param name="maxHeight"></param>
    public static void FillHeightRange<T>(ref T[,] map,
        ref float[,] heightMap,
        T currentTerrain,
        float minHeight,
        float maxHeight)
    {
        for (int x = 0; x <= map.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++)
            {
                if (heightMap[x, y] > minHeight && heightMap[x, y] < maxHeight)
                {
                    map[x, y] = currentTerrain;
                    heightMap[x, y] = minHeight;
                }
            }
        }
    }

    public static bool InBounds<T>(T[,] map, Vector2Int position)
    {
        //Debug.Log($"{position}{map.GetUpperBound(0)} {position.x <= map.GetUpperBound(0)} { position.y <= map.GetUpperBound(1)} { position.x > 0} { position.y > 0} ");
        if (position.x <= map.GetUpperBound(0) &&
            position.y <= map.GetUpperBound(1) &&
            position.x >= 0 &&
            position.y >= 0)
        {
            return true;
        }

        return false;
    }

    public static T[,] RandomSquares<T>(ref T[,] map, System.Random rand, T currentTerrain, int width)
    {
        Vector2Int mid = new Vector2Int(map.GetUpperBound(0) / 2, map.GetUpperBound(1) / 2);

        int midRadiusX = map.GetUpperBound(0) / 2;
        int midRadiusY = map.GetUpperBound(1) / 2;
        mid += new Vector2Int(rand.Next(-midRadiusX, midRadiusX),
            rand.Next(-midRadiusY, midRadiusY));

        for (int i = -width; i < width; i++)
        {
            for (int j = -width; j < width; j++)
            {
                var point = mid + new Vector2Int(i, j);
                if (InBounds(map, point))
                {
                    map[point.x, point.y] = currentTerrain;
                }
            }
        }

        return map;
    }

    public static void AjdacentTiles<T>(ref T[,] map,
    ref float[,] heightMap,
    ref Vector2[,] gradientMap,
    ref Terrain[,] baseTerrainMap,
    ref Improvement[,] baseImprovementMap,
    System.Random rand,
    T currentTerrain,
    float minHabitability,
    float maxGadient,
    int radius,
    float spawnChance,
    Dictionary<Terrain, TerrainMapTile> terrainTileLookup,
    Dictionary<Improvement, ImprovementMapTile> improvementTileLookup)
    {
        //float[,] HabitabilityMap = new float[baseTerrainMap.GetUpperBound(0) + 1, baseTerrainMap.GetUpperBound(1) + 1];
        int cnt = 0;
        for (int x = 0; x <= baseTerrainMap.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= baseTerrainMap.GetUpperBound(1); y++)
            {
                if(!terrainTileLookup[baseTerrainMap[x,y]].Improvable)
                {
                    continue;
                }

                float habitabilityScore = 0;

                int numNeighborsChecked = 0;
                int range = radius;
                float maxScore = 0;
                //loop through every tiles neighbors
                for (int i = x - range; i <= x + range; i++)
                {
                    for (int j = y - range; j <= y + range; j++)
                    {
                        if (MapManager.InBounds(baseTerrainMap, i, j))
                        {
                            numNeighborsChecked++;
                            var otherTerrainTile = baseTerrainMap[i, j];
                            var otherImprovementTile = baseImprovementMap[i, j];
                            var distance = new Vector2(i, j).magnitude;
                            maxScore += 1 / distance;
                            if (otherTerrainTile == Terrain.Water || otherImprovementTile == Improvement.Road)
                            {
                                habitabilityScore += 1/distance;
                                cnt++;
                            }
                        }
                    }
                }

                habitabilityScore /= maxScore;

                var TerrainTile = baseTerrainMap[x, y];
                var ImprovementTile = baseImprovementMap[x, y];

                if (habitabilityScore > minHabitability && gradientMap[x, y].magnitude < maxGadient &&
                    (terrainTileLookup[TerrainTile].Improvable && improvementTileLookup[ImprovementTile].Improvable) &&
                    rand.NextDouble() < spawnChance)
                {
                    map[x, y] = currentTerrain;
                }

            }
        }
    }

    public static void Lake<T>(ref T[,] map,
        ref float[,] heightMap,
        ref float[,] lakeMap,
        ref Vector2[,] gradientMap,
        ref Terrain[,] baseTerrainMap,
        T currentTerrain,
        MapLayerSettings layerSetting)
    {
        SmoothMT(ref lakeMap, 5, 4);
        Normalize(ref lakeMap);

        for (int x = 0; x <= lakeMap.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= lakeMap.GetUpperBound(1); y++)
            {
                if(lakeMap[x, y] > layerSetting.WaterPercentThreshold &&
                   gradientMap[x, y].magnitude < layerSetting.MaxWaterGradient)
                {
                    map[x, y] = currentTerrain;
                    //heightMap[x, y] -= Mathf.Clamp(lakeMap[x, y], 0, layerSetting.MaxWaterDepth);
                   //heightMap[x, y] -= .01f;
                }    
            }
        }

        // TODO store this value so we can use it later when doing water meshes
        List<HashSet<Vector2Int>> components = FindComponents(Terrain.Water, heightMap.GetUpperBound(0), ref baseTerrainMap);
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
                heightMap[pos.x, pos.y] = minHeight;
            }
        }

        averageComponentSize /= components.Count();

        //List<Vector2Int> centroids = FindCentroids(components);

        //if(centroids.Count == 0)
        //{
        //    return;
        //}

        //Vector2Int start = centroids[Random.Range(0, centroids.Count)];
        //Vector2Int End = centroids[Random.Range(0, centroids.Count)];
        //for (int i = 0; i < centroids.Count; i++)
        //{
        //    Vector2Int centroid = centroids[i];
        //    var component = components[i];
        //    Stream(currentTerrain, ref heightMap, ref gradientMap, ref map, centroid, 20, Mathf.RoundToInt(component.Count / (averageComponentSize*2)));
        //}
    }

    public static void Stream<T>(
        T currentTerrain,
        ref float[,] heightMap,
        ref Vector2[,] gradientMap,
        ref T[,] map,
        Vector2Int center,
        int searchRadius,
        int numStreams)
    {
        
        float maxHeight = 0;
        // Find the delta heights in the area around the center
        for(int i = -searchRadius; i < searchRadius; i++)
        {
            for (int j = -searchRadius; j < searchRadius; j++)
            {
                Vector2Int pos = new Vector2Int(i, j) + center;
                if(InBounds(heightMap, pos) && heightMap[pos.x, pos.y] > maxHeight)
                {
                    maxHeight = heightMap[pos.x, pos.y];
                }
            }
        }

        // pick a random elem thats with 25% of the heighest
        float heightThreshold = maxHeight * .75f;

        Vector2Int maxHeightPos = Vector2Int.zero;
        List<Vector2Int> positionsInThreshold = new List<Vector2Int>();
        for (int i = -searchRadius; i < searchRadius; i++)
        {
            for (int j = -searchRadius; j < searchRadius; j++)
            {
                Vector2Int pos = new Vector2Int(i, j) + center;
                if (InBounds(heightMap, pos) && heightMap[pos.x, pos.y] > heightThreshold)
                {
                    positionsInThreshold.Add(pos);
                }
            }
        }

        for(int i = 0; i < numStreams; i++)
        {
            Vector2 currPos = positionsInThreshold[Random.Range(0, positionsInThreshold.Count)];
            Vector2Int gridCurrPos = RoundVector(currPos);
            int step = 0;
            Vector2 mementum = (new Vector2(center.x, center.y) - currPos).normalized * 0f;
            HashSet<Vector2Int> path = new HashSet<Vector2Int>();

            // follow gradient down
            while (step < searchRadius * 100 && InBounds(gradientMap, gridCurrPos) && !map[gridCurrPos.x, gridCurrPos.y].Equals(currentTerrain))
            {
                Vector2 dirToEnd = (new Vector2(center.x, center.y) - currPos).normalized * .025f;
                Vector2 gradient = gradientMap[gridCurrPos.x, gridCurrPos.y];
                mementum -= gradient;
                Vector2 dir = (mementum + dirToEnd).normalized * .5f;
                //var gridNextPos = RoundVector(currPos + dir);
                //var deltaHeight = heightMap[gridNextPos.x, gridNextPos.y] - heightMap[gridCurrPos.x, gridCurrPos.y];

                currPos += dir;
                gridCurrPos = RoundVector(currPos);
                //heightMap[gridCurrPos.x, gridCurrPos.y] -= .01f;
                if(InBounds(map, gridCurrPos))
                    path.Add(gridCurrPos);
                step++;
            }

            foreach(var pos in path)
            {
                map[pos.x, pos.y] = currentTerrain;
            }
        }
    }

    private class CentroidDistance
    {
        public Vector2Int Centroid;
        public Vector2 displacement;
        public int step;
    }

    /// <summary>
    /// connect two lakes by going through other lakes, doesnt work well
    /// </summary>
    /// <typeparam name="T"> terrain or improvement</typeparam>
    /// <param name="currentTerrain"></param>
    /// <param name="heightMap"></param>
    /// <param name="map"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="centroids"></param>
    public static void River<T>(
        T currentTerrain,
        ref float[,] heightMap,
        ref Vector2[,] gradientMap,
        ref T[,] map,
        Vector2Int start,
        Vector2Int end,
        List<Vector2Int> centroids,
        HashSet<Vector2Int> prevCentroids)
    {
        if(start == end)
        {
            return;
        }
        prevCentroids.Add(start);

        Debug.Log($"start {start} end {end}");
        float startHeight = heightMap[start.x, start.y];

        Vector2 currPos = start;
        Vector2Int gridCurrPos = RoundVector(currPos);
        int step = 0;
        Dictionary<Vector2Int, CentroidDistance> CentroidDistanceMap = new Dictionary<Vector2Int, CentroidDistance>();
        // map[gridCurrPos.x, gridCurrPos.y] = currentTerrain;
        // 

        // draw a line a build a list of how far the min distance from each lake is to the line, and consider the number of steps it took to get there 
        while (step < heightMap.GetUpperBound(0) * 2 && gridCurrPos != end)
        {
            Vector2 dir = (new Vector2(end.x, end.y) - currPos).normalized;
            currPos += dir;
            gridCurrPos = RoundVector(currPos);
            step++;
            //heightMap[gridCurrPos.x, gridCurrPos.y] = 1f;
            var distanceToEnd = (end - currPos).magnitude;

            float minDistance = float.MaxValue;
            CentroidDistance minCentroidDistance = null;
            // Update centroid Distances
            foreach (var centroid in centroids)
            {
                if (centroid == start || centroid == end || prevCentroids.Contains(centroid))
                {
                    continue;
                }

                Vector2 displacement = (currPos - centroid);
                float distance = displacement.magnitude;
                if (distance < distanceToEnd && distance < minDistance)
                {
                    minDistance = distance;

                    CentroidDistance CD = new CentroidDistance
                    {
                        displacement = displacement,
                        Centroid = centroid,
                        step = step
                    };
                    minCentroidDistance = CD;
                }
            }

            // Update the CD value for this position if we havent added it before, or if this distance is smaller than its last one
            if (minCentroidDistance != null)
            {
                if(CentroidDistanceMap.TryGetValue(minCentroidDistance.Centroid, out CentroidDistance lastCD))
                {
                    if(minDistance < lastCD.displacement.magnitude)
                    {
                        CentroidDistanceMap[minCentroidDistance.Centroid] = minCentroidDistance;
                    }
                }
                else
                {
                    CentroidDistanceMap[minCentroidDistance.Centroid] = minCentroidDistance;
                }

                break;
            }
        }

        List<CentroidDistance> centroidDistances = CentroidDistanceMap.Values.ToList();
        centroidDistances.Sort( (l,r) => {
            return l.step - r.step;
        });

        // Just handle going to the next one and recurse
        currPos = start;
        step = 0;
        gridCurrPos = RoundVector(currPos);

        var nextCentroid = end;
        if (centroidDistances.Count > 0)
        {
            nextCentroid = centroidDistances[0].Centroid;
        }

        //float waterLevel = (nextCentroid - start).magnitude * .25f;
        float waterLevel = .25f;

        while (step < heightMap.GetUpperBound(0) * 2 && gridCurrPos != nextCentroid)
        {
            Vector2 dir = (new Vector2(nextCentroid.x, nextCentroid.y) - currPos).normalized;
            var gridNextPos = RoundVector(currPos + dir);
            var deltaHeight = heightMap[gridNextPos.x, gridNextPos.y] - heightMap[gridCurrPos.x, gridCurrPos.y];

            waterLevel -= deltaHeight;
            if(waterLevel > 0)
                map[gridCurrPos.x, gridCurrPos.y] = currentTerrain;

            currPos += dir;
            gridCurrPos = RoundVector(currPos);
            //heightMap[gridCurrPos.x, gridCurrPos.y] -= .01f;
            
            step++;

        }

        River(currentTerrain, ref heightMap, ref gradientMap, ref map, nextCentroid, end, centroids, prevCentroids);
    }

    public static List<Vector2Int> FindCentroids(List<HashSet<Vector2Int>> components)
    {
        List<Vector2Int> centroids = new List<Vector2Int>();
        foreach(var component in components)
        {
            centroids.Add(FindCentroid(component));
        }

        return centroids;
    }

    public static Vector2Int FindCentroid(HashSet<Vector2Int> component)
    {
        Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
        Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);

        foreach (var pos in component)
        {
            min.x = Mathf.Min(min.x, pos.x);
            min.y = Mathf.Min(min.y, pos.y);
            max.x = Mathf.Max(max.x, pos.x);
            max.y = Mathf.Max(max.y, pos.y);
        }

        Vector2 centroid = Vector2.zero;

        foreach (var pos in component)
        {
            Vector2Int posOffset = pos - min;
            centroid += posOffset;
        }

        centroid /= component.Count;

        return RoundVector(centroid) + min;
    }

    public static List<HashSet<Vector2Int>> FindComponents(Terrain terrain, int mapSize, ref Terrain[,] terrainTileMap)
    {
        int[,] componentMap = new int[mapSize, mapSize];

        int componentCounter = 1;

        // MUST NOT USE DIAGONALS will mess up mesh generation
        Vector2Int[] floodFillDirections = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
        };

        List<HashSet<Vector2Int>> components = new List<HashSet<Vector2Int>>();

        // 0 represents unmarked, -1 is a failure, and any other number is a component
        for (int y = 0; y <= componentMap.GetUpperBound(0); y++)
        {
            for (int x = 0; x <= componentMap.GetUpperBound(1); x++)
            {
                if (terrainTileMap[x, y] == terrain && componentMap[x, y] == 0)
                {
                    HashSet<Vector2Int> componet = FloodFill(
                        terrain,
                        ref componentMap,
                        new Vector2Int(x, y),
                        componentCounter,
                        floodFillDirections,
                        ref componentMap,
                        ref terrainTileMap);

                    components.Add(componet);
                    componentCounter++;
                }
                    
            }
        }

        return components;
    }

    private static HashSet<Vector2Int> FloodFill(
        Terrain terrain,
        ref int[,] componentMap,
        Vector2Int startPos,
        int componentNumber,
        Vector2Int[] floodFillDirections,
        ref int[,] waterComponentMap,
        ref Terrain[,] terrainTileMap)
    {
        HashSet<Vector2Int> componet = new HashSet<Vector2Int>();

        Stack<Vector2Int> cellsToBeProcessed = new Stack<Vector2Int>();
        cellsToBeProcessed.Push(startPos);

        while (cellsToBeProcessed.Count > 0)
        {
            Vector2Int currPos = cellsToBeProcessed.Pop();
            componet.Add(currPos);
            componentMap[currPos.x, currPos.y] = componentNumber;

            foreach (var dir in floodFillDirections)
            {
                Vector2Int newPos = currPos + dir;
                if (IsUnmarkedTile(terrain, newPos, ref waterComponentMap, ref terrainTileMap))
                {
                    cellsToBeProcessed.Push(newPos);
                }
            }
        }

        return componet;
    }

    private static bool IsUnmarkedTile( 
        Terrain terrain,
        Vector2Int Pos,
        ref int[,] componentMap,
        ref Terrain[,] terrainTileMap)
    {
        bool inBounds = LayerMapFunctions.InBounds(componentMap, Pos);
        if (inBounds && componentMap[Pos.x, Pos.y] == 0)
        {
            bool isWaterTile = terrainTileMap[Pos.x, Pos.y] == Terrain.Water;

            if (isWaterTile)
            {
                return true;
            }
            else
            {
                // check adjacent tiles
                Vector2Int[] adjacentDirections = {
                    Vector2Int.zero,
                    Vector2Int.up,
                    Vector2Int.down,
                    Vector2Int.left,
                    Vector2Int.right,
                    Vector2Int.up + Vector2Int.left,
                    Vector2Int.up + Vector2Int.right,
                    Vector2Int.down + Vector2Int.left,
                    Vector2Int.down + Vector2Int.right,
                };

                foreach (var adj in adjacentDirections)
                {
                    foreach (var adj2 in adjacentDirections)
                    {
                        Vector2Int adjacentPos = new Vector2Int(Pos.x + adj.x + adj2.x, Pos.y + adj.y + adj2.y);
                        if (LayerMapFunctions.InBounds(componentMap, adjacentPos) && terrainTileMap[adjacentPos.x, adjacentPos.y] == terrain)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    public static void GadientDescent<T>(ref T[,] map,
        ref float[,] heightMap,
        ref Vector2[,] gradientMap,
        System.Random rand,
        T currentTerrain,
        float minStartHeight,
        float minStopHeight,
        float maxWidth,
        float widthChangeThrotle)
    {
        //pick a random spot
        Vector2Int start = new Vector2Int(Random.Range(0, map.GetUpperBound(0)), Random.Range(0, map.GetUpperBound(1)));
        
        //follow its gradient to the top
        var currPos = start;
        var momentum = gradientMap[currPos.x, currPos.y].normalized*0;

        HashSet<Vector2Int> prevLocations = new HashSet<Vector2Int>();

        //while(heightMap[currPos.x, currPos.y] < minStartHeight)
        while(true)
        {
            //we reached a local maximum
            if (prevLocations.Contains(currPos))
            {
                break;
            }

            prevLocations.Add(currPos);

            var gradient = gradientMap[currPos.x, currPos.y].normalized + momentum;
            currPos += RoundVector(gradient);
            momentum = momentum * .9f;
        }

        //once we reach the min start height, start going back down, but leave the terrain behind
        float width = 0;
        prevLocations.Clear();
        int numSteps = 0;
        while (heightMap[currPos.x, currPos.y] > minStopHeight)
        {
            map[currPos.x, currPos.y] = currentTerrain;
            numSteps++;
            var gradient = gradientMap[currPos.x, currPos.y].normalized;
            width += gradientMap[currPos.x, currPos.y].magnitude * widthChangeThrotle;
            width = Mathf.Clamp(width, 0, maxWidth);
            Vector2 tangent = Vector2.Perpendicular(gradient).normalized;
            for (float i = -width; i < width; i+= .1f)
            {
                Vector2 step = (currPos + tangent * i);
                var stepRounded = new Vector2Int(Mathf.RoundToInt(step.x), Mathf.RoundToInt(step.y));
                if (InBounds(map, stepRounded))
                    map[stepRounded.x, stepRounded.y] = currentTerrain;
            }
            
            //we reached a local min
            if (prevLocations.Contains(currPos))
            {
                break;
            }
            prevLocations.Add(currPos);

            var delta = RoundVector(gradient) * -1;
            //randomly add in a side step
            var sideStep = rand.Next(3) - 1; // range -1, 0 , 1
            delta = RoundVector(((sideStep * tangent) + delta).normalized);

            //Debug.Log(delta + " " + (System.Math.Abs(delta.x) == System.Math.Abs(delta.y)));

            //dont go diagonally, only keep one axis
            if (System.Math.Abs(delta.x) == System.Math.Abs(delta.y))
            {
                var keepxOry = rand.Next(2);
                if (keepxOry == 1)
                {
                    delta = new Vector2Int(delta.x, 0);
                }
                else
                {
                    delta = new Vector2Int(0, delta.y);
                }
            }

            var nextStep = currPos + delta;
            if(!MapManager.InBounds(heightMap, nextStep.x, nextStep.y))
            {
                break;
            }

            currPos = nextStep;
        }

        //fill everything at about that z level
        prevLocations.Clear();
        Stack<Vector2Int> nextLocations = new Stack<Vector2Int>();
        nextLocations.Push(currPos);
        float height = heightMap[currPos.x, currPos.y];
        while (nextLocations.Count != 0)
        {
            currPos = nextLocations.Pop();
            map[currPos.x, currPos.y] = currentTerrain;
            heightMap[currPos.x, currPos.y] = height;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector2Int step = currPos + new Vector2Int(x, y);
                    if (MapManager.InBounds(heightMap, step.x, step.y) &&
                        !prevLocations.Contains(step) &&
                        gradientMap[step.x,step.y].magnitude < .05f)
                    {
                        nextLocations.Push(step);
                        prevLocations.Add(step);
                    }
                }
            }
        }
    }

    public static void FollowAlongGradient<T>(ref T[,] map,
        ref float[,] heightMap,
        ref Vector2[,] gradientMap,
        System.Random rand,
        T currentTerrain,
        float Width)
    {
        //pick a random spot
        Vector2Int start = new Vector2Int(Random.Range(0, map.GetUpperBound(0)), Random.Range(0, map.GetUpperBound(1)));

        Debug.Log("start " + start);
        //follow its gradient to the top
        var currPos = start;
        var momentum = gradientMap[currPos.x, currPos.y].normalized * 0;

        HashSet<Vector2Int> prevLocations = new HashSet<Vector2Int>();

        //while(heightMap[currPos.x, currPos.y] < minStartHeight)
        int cnt = 0;
        while (MapManager.InBounds(heightMap, currPos.x, currPos.y) && cnt < 1000)
        {
            cnt++;
            map[currPos.x, currPos.y] = currentTerrain;
            //we reached a local maximum
            if (prevLocations.Contains(currPos))
            {
                Debug.Log("HIT LOCAL MAX");
                break;
            }

            prevLocations.Add(currPos);

            var gradient = gradientMap[currPos.x, currPos.y].normalized;
            Vector2 tangent = Vector2.Perpendicular(gradient).normalized;
            currPos += RoundVector(tangent);
            momentum = momentum * .9f;
        }
    }

    /// <summary>
    /// does a random walk, can avoid blocking terrain (not improvements) if the parameter is set
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="map"></param>
    /// <param name="baseTerrainMap"></param>
    /// <param name="heightMap"></param>
    /// <param name="rand"></param>
    /// <param name="currentTerrain"></param>
    /// <param name="width"></param>
    /// <param name="CheckForBlockers"></param>
    /// <param name="terrainTileLookup"></param>
    /// <returns></returns>
    public static T[,] RandomWalk2D<T>(ref T[,] map,
        ref Terrain[,] baseTerrainMap,
        ref float[,] heightMap,
        System.Random rand,
        T currentTerrain,
        int width,
        bool CheckForBlockers,
        Dictionary<Terrain, TerrainMapTile> terrainTileLookup)
    {
        #region setup start and target points
        //start on an edge
        var startOnXOrY = rand.Next(2);
        var startAtBeginingOrEnd = rand.Next(2);
        Vector2Int start = new Vector2Int();
        if (startOnXOrY == 1)
        {
            if (startAtBeginingOrEnd == 1)
                start = new Vector2Int(Random.Range(0, map.GetUpperBound(0)), 0);
            else
                start = new Vector2Int(Random.Range(0, map.GetUpperBound(0)), map.GetUpperBound(1));
        }
        else
        {
            if (startAtBeginingOrEnd == 1)
                start = new Vector2Int(0, Random.Range(0, map.GetUpperBound(1)));
            else
                start = new Vector2Int(map.GetUpperBound(0), Random.Range(0, map.GetUpperBound(1)));
        }

        //pick a random point in the middle to go through
        Vector2Int mid = new Vector2Int(map.GetUpperBound(0) / 2, map.GetUpperBound(1) / 2);

        int midRadiusX = map.GetUpperBound(0) / 2;
        int midRadiusY = map.GetUpperBound(1) / 2;
        mid += new Vector2Int(rand.Next(-midRadiusX, midRadiusX),
            rand.Next(-midRadiusY, midRadiusY));

        Vector2 dir = mid - start;

        //if we picked to start in the middle just pick a direction
        if (dir == Vector2.zero)
        {
            var directions = new List<Vector2Int>(){ new Vector2Int(1, 1), new Vector2Int(1, -1),
                                                 new Vector2Int(-1, 1), new Vector2Int(-1, -1),
                                                 new Vector2Int(0, 1), new Vector2Int(1, 0) ,
                                                 new Vector2Int(0, -1) , new Vector2Int(-1, 0) };
            var randIndex = rand.Next(directions.Count);
            dir = directions[randIndex];
        }
        else
        {
            dir = dir.normalized;
        }
        #endregion

        float startHeight = heightMap[start.x, start.y];

        Vector2Int gridPosition = start;
        Vector2 realPosition = start;
        float maxSteps = map.GetUpperBound(0) * map.GetUpperBound(0);
        int numSteps = 0;
        while (InBounds(map, gridPosition) && numSteps < maxSteps)
        {
            numSteps++;
            Vector2 tangent = Vector2.Perpendicular(dir).normalized;
            #region check blockers
            if(CheckForBlockers)
            {
                //check if any block ahead of us are impassible
                bool isBlocked = false;
                int lookAheadTiles = 5;
                for (int i = 2; i < lookAheadTiles; i++)
                {
                    Vector2 nextMoveExtended = (realPosition + dir.normalized * i);
                    Vector2Int nextMoveExtendedRounded = new Vector2Int(Mathf.RoundToInt(nextMoveExtended.x), Mathf.RoundToInt(nextMoveExtended.y));
                    if (InBounds(baseTerrainMap, nextMoveExtendedRounded) &&
                    !terrainTileLookup[baseTerrainMap[nextMoveExtendedRounded.x, nextMoveExtendedRounded.y]].Improvable)
                    {
                        isBlocked = true;
                    }
                }

                if (isBlocked)
                {
                    var tangentSign = rand.Next(2) == 0 ? -1 : 1;
                    //if we cant go farther take a turn
                    dir = Vector2.MoveTowards(dir, tangent * tangentSign, .1f);

                    continue;
                }
            }
            #endregion
            Vector2Int stepRounded = RoundVector(realPosition);
            map[stepRounded.x, stepRounded.y] = currentTerrain;
            heightMap[stepRounded.x, stepRounded.y] = startHeight;

            for (int i = -width; i <= width; i++)
            {
                Vector2 step = (realPosition + tangent * i);
                stepRounded = new Vector2Int(Mathf.RoundToInt(step.x), Mathf.RoundToInt(step.y));
                if (InBounds(map, stepRounded))
                {
                    map[stepRounded.x, stepRounded.y] = currentTerrain;
                    heightMap[stepRounded.x, stepRounded.y] = startHeight;
                }
            }

            Vector2 nextMove = (realPosition + dir);
            
            //add in a random amound of deveation from dir so that its more curvy
            Vector2Int nextMoveRounded = new Vector2Int(Mathf.RoundToInt(nextMove.x), Mathf.RoundToInt(nextMove.y));
            Vector2Int delta = nextMoveRounded - gridPosition;

            //dont go diagonally, only keep one axis
            //if (System.Math.Abs(delta.x) == System.Math.Abs(delta.y))
            //{
            //    var keepxOry = rand.Next(2);
            //    if (keepxOry == 1)
            //    {
            //        delta = new Vector2Int(delta.x, 0);
            //        nextMove = realPosition + new Vector2(dir.x, 0);
            //    }
            //    else
            //    {
            //        delta = new Vector2Int(0, delta.y);
            //        nextMove = realPosition + new Vector2(0, dir.y);
            //    }
            //}

            //dir += new Vector2((float)rand.NextDouble() - .5f, (float)rand.NextDouble() - .5f) * .01f;
            //dir = dir.normalized / 8;

            var directionSign = rand.Next(2) == 0 ? -1 : 1;
            //if we cant go farther take a turn
            //dir = Vector2.MoveTowards(dir, tangent * directionSign, .008f);

            gridPosition = gridPosition + delta;
            realPosition = nextMove;
        }

        //Debug.Log(dir/8);

        //RandomWalk2DHelper(ref map, rand, currentTerrain, start, start, dir/8, width, 0);
        return map;
    }

    public static Vector2Int FloorVector(Vector2 vec)
    {
        return new Vector2Int(Mathf.FloorToInt(vec.x), Mathf.FloorToInt(vec.y));
    }

    public static Vector2Int RoundVector(Vector2 vec)
    {
        return new Vector2Int(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y));
    }

    public static void RandomWalk2DHelper(ref Terrain[,] map, System.Random rand, Terrain currentTerrain, Vector2Int gridPosition, Vector2 realPosition, Vector2 dir, int width, int numSteps)
    {
        if(!InBounds(map, gridPosition))
        {
            return;
        }

        Vector2 tangent = Vector2.Perpendicular(dir);

        for (int i = -width; i < width; i++)
        {
            Vector2 step = (realPosition + tangent*i);
            Vector2Int stepRounded = new Vector2Int(Mathf.RoundToInt(step.x), Mathf.RoundToInt(step.y));
            if(InBounds(map, stepRounded))
                map[stepRounded.x, stepRounded.y] = currentTerrain;
        }

        //map[gridPosition.x, gridPosition.y] = currentTerrain;
        
        Vector2 nextMove = (realPosition + dir);

        //add in a random amound od deveation from dir so that its more curvy
        Vector2Int nextMoveRounded = new Vector2Int(Mathf.RoundToInt(nextMove.x), Mathf.RoundToInt(nextMove.y));
        Vector2Int delta = nextMoveRounded - gridPosition;


        dir += new Vector2((float) rand.NextDouble() - .5f, (float) rand.NextDouble() - .5f) * .01f;
        dir = dir.normalized / 8;
        //Debug.Log((float)rand.NextDouble() - .5f);
        RandomWalk2DHelper(ref map, rand, currentTerrain, gridPosition + delta, nextMove, dir, width, numSteps);
    }

    public static T[,] PerlinNoise<T>(ref T[,] map,
        ref Terrain[,] baseTerrainMap,
        ref Vector2[,] gradientMap,
        T currentTerrain,
        System.Random rand,
        float scale,
        float threshold,
        float maxGadient,
        Dictionary<Terrain, TerrainMapTile> terrainTileLookup)
    {
        Vector2 shift = new Vector2((float)rand.NextDouble() * 1000, (float)rand.NextDouble() * 1000);
        for (int x = 0; x <= map.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++)
            {
                float xCoord = shift.x + x / (map.GetUpperBound(0) + 1f) * scale;
                float yCoord = shift.y + y / (map.GetUpperBound(1) + 1f) * scale;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);

                if(sample > threshold && gradientMap[x,y].magnitude < .05f && terrainTileLookup[baseTerrainMap[x,y]].Improvable)
                {
                    map[x, y] = currentTerrain;
                }
            }
        }

        return map;
    }

    public static bool FindPathToEdge(ref Terrain[,] map, System.Random rand, Terrain currentTerrain, Vector2Int position, int currentStep, int maxSteps)
    {
        Debug.Log("position " + position);
        if (position.x > map.GetUpperBound(0) || 
            position.y > map.GetUpperBound(1) ||
            position.x < 0 ||
            position.y < 0 || currentStep > maxSteps)
        {
            return true;
        }

        if(map[position.x, position.y] == currentTerrain)
        {
            return false;
        }

        map[position.x, position.y] = currentTerrain;
        List<Vector2Int> possibleMoves = new List<Vector2Int>();

        for(int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if(i != 0 && j != 0)
                {
                    continue;
                }
                Vector2Int nextMove = new Vector2Int(i,j) + position;
                
                if (InBounds(map, nextMove))
                {
                    if(map[nextMove.x, nextMove.y] != currentTerrain)
                    {
                        //Debug.Log(nextMove);
                        possibleMoves.Add(nextMove);
                    }
                }
                else
                {
                    possibleMoves.Add(nextMove);
                }
            }
        }
        
        if(possibleMoves.Count == 0)
        {
            return false;
        }

        while(possibleMoves.Count > 0)
        {
            var randIndex = rand.Next(possibleMoves.Count);
            bool foundEdge = FindPathToEdge(ref map, rand, currentTerrain, possibleMoves[randIndex], currentStep + 1, maxSteps);
            possibleMoves.RemoveAt(randIndex);
            if (foundEdge)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// remove jagged edges
    /// </summary>
    public static void Smooth(ref float[,] map)
    {
        //gausian smoothing filter
        var kernel = new List<List<float>>()
        {
            new List<float>{1,4,7,4,1 },
            new List<float>{4,16,26,16,4},
            new List<float>{7,26,41,26,7},
            new List<float>{4,16,26,16,4},
            new List<float>{1,4,7,4,1 },
        };

        int k_h = kernel.Count;
        int k_w = kernel[0].Count;

        for (int k_x = 0; k_x < k_w; k_x++)
        {
            for (int k_y = 0; k_y < k_h; k_y++)
            {
                kernel[k_y][k_x] /= 273f;
            }
        }

        map = Convolution2D(map, kernel);
    }

    /// <summary>
    /// remove jagged edges
    /// </summary>
    public static void Smooth(ref float[,] map, int kernalSize)
    {
        float sigma = 1;
        float r, s = 2.0f * sigma * sigma;

        //gausian smoothing filter
        var kernel = new List<List<float>>();

        float sum = 0;
        for (int x = -kernalSize/2; x <= kernalSize/2; x++)
        {
            var row = new List<float>();
            for (int y = -kernalSize/2; y <= kernalSize/2; y++)
            {
                r = Mathf.Sqrt(x * x + y * y);
                float val = (Mathf.Exp(-(r * r) / s)) / (Mathf.PI * s);
                sum += val;
                row.Add(val);
            }

            kernel.Add(row);
        }

        for(int i = 0; i < kernel.Count; i++)
        {
            var row = kernel[i];
            for (int j = 0; j < kernel.Count; j++)
            {
                row[j] /= sum;
            }
        }

        map = Convolution2D(map, kernel);
    }

    /// <summary>
    /// remove jagged edges, mutithreaded dont use for small maps
    /// </summary>
    public static void SmoothMT(ref float[,] map, int kernalSize, int numThreads = DefaultNumThreadsForJob)
    {
        float sigma = 1;
        float r, s = 2.0f * sigma * sigma;

        //gausian smoothing filter
        var kernel = new List<List<float>>();

        float sum = 0;
        for (int x = -kernalSize / 2; x <= kernalSize / 2; x++)
        {
            var row = new List<float>();
            for (int y = -kernalSize / 2; y <= kernalSize / 2; y++)
            {
                r = Mathf.Sqrt(x * x + y * y);
                float val = (Mathf.Exp(-(r * r) / s)) / (Mathf.PI * s);
                sum += val;
                row.Add(val);
            }

            kernel.Add(row);
        }

        for (int i = 0; i < kernel.Count; i++)
        {
            var row = kernel[i];
            for (int j = 0; j < kernel.Count; j++)
            {
                row[j] /= sum;
            }
        }

        map = Convolution2DMT(map, kernel, numThreads);
    }

    public static void Normalize(ref float[,] arr)
    {
        float min = 100000, max = -100000;

        for (int x = 0; x <= arr.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= arr.GetUpperBound(1); y++)
            {
                min = Mathf.Min(min, arr[x, y]);
                max = Mathf.Max(max, arr[x, y]);
            }
        }

        for (int x = 0; x <= arr.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= arr.GetUpperBound(1); y++)
            {
                arr[x, y] = Mathf.InverseLerp(min, max, arr[x, y]);
            }
        }
    }

    public static float[,] Convolution2D(float[,] arr, List<List<float>> kernel)
    {
        var convolutedArr = new float[arr.GetUpperBound(0) + 1, arr.GetUpperBound(1) + 1];
        int k_h = kernel.Count;
        int k_w = kernel[0].Count;

        //do a 2d convolution
        for (int x = 0; x <= arr.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= arr.GetUpperBound(1); y++)
            {
                for (int k_x = 0; k_x < k_w; k_x++)
                {
                    for (int k_y = 0; k_y < k_h; k_y++)
                    {
                        float k = kernel[k_y][k_x];
                        int heightMapOffset_x = x + k_x - (k_w / 2);
                        int heightMapOffset_y = y + k_y - (k_h / 2);

                        if (MapManager.InBounds(arr, heightMapOffset_x, heightMapOffset_y))
                            convolutedArr[x, y] += arr[heightMapOffset_x, heightMapOffset_y] * k;
                    }
                }
            }
        }

        Normalize(ref convolutedArr);

        return convolutedArr;
    }

    public static void ParallelForFast(float[,] map, System.Action<int, int> action, int numThreads = DefaultNumThreadsForJob)
    {
        int size = (map.GetUpperBound(1) + 1) / numThreads;
        int mapSize = map.GetUpperBound(1);

        // Use the thread pool to parrellize update
        using (CountdownEvent e = new CountdownEvent(1))
        {
            // TODO make these blocks instead of rows so that we get better lock perf
            for (int i = 0; i < numThreads; i++)
            {
                Vector2Int start = new Vector2Int(0, i * size);
                Vector2Int end = new Vector2Int(map.GetUpperBound(1) + 1, ((i + 1) * size) - 1);
                e.AddCount();
                ThreadPool.QueueUserWorkItem(delegate (object state)
                {
                    try
                    {
                        for (int y = start.y; y <= end.y; y++)
                        {
                            for (int x = start.x; x < end.x; x++)
                            {
                                action(x,y);
                            }
                        }
                    }
                    finally
                    {
                        e.Signal();
                    }
                },
                null);
            }

            e.Signal();
            e.Wait();
        }
    }

    public static float[,] Convolution2DMT(float[,] map, List<List<float>> kernel, int numThreads = DefaultNumThreadsForJob)
    {
        var convolutedArr = new float[map.GetUpperBound(0) + 1, map.GetUpperBound(1) + 1];
        int k_h = kernel.Count;
        int k_w = kernel[0].Count;

        //int size = (map.GetUpperBound(1) + 1) / numThreads;
        int mapSize = map.GetUpperBound(1);

        ParallelForFast(map, (x, y) => {
            for (int k_x = 0; k_x < k_w; k_x++)
            {
                for (int k_y = 0; k_y < k_h; k_y++)
                {
                    float k = kernel[k_y][k_x];
                    int heightMapOffset_x = x + k_x - (k_w / 2);
                    int heightMapOffset_y = y + k_y - (k_h / 2);

                    if (MapManager.InBounds(mapSize, mapSize, heightMapOffset_x, heightMapOffset_y))
                        convolutedArr[x, y] += map[heightMapOffset_x, heightMapOffset_y] * k;
                }
            }
        }, numThreads);

        Normalize(ref convolutedArr);

        return convolutedArr;
    }

    public static bool LogTimes = true;

    public static void LogAction(System.Action action, string text)
    {
        System.DateTime start = System.DateTime.Now;
        action();
        if (LogTimes)
        {
            System.DateTime end = System.DateTime.Now;
            Debug.Log($"{text} : {end - start}");
        }
    }
}
