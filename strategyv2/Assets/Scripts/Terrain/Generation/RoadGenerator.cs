using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Generator;
using System.Linq;

namespace Generator
{
    public struct PathElement
    {
        public Vector2 Position;
        public Vector2 Direction;
        public Vector2 Tangent;
    }
}

public class RoadGenerator
{
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
    public static T[,] GenerateRoad<T>(ref T[,] map,
        MapData mapData,
        System.Random rand,
        T currentTerrain,
        int width,
        bool CheckForBlockers,
        Dictionary<Terrain, TerrainMapTile> terrainTileLookup)
    {
        bool startFound = FindStartAndDir(map, mapData, rand, out Vector2Int start, out Vector2Int end);

        // theres the possibility that theres no land mass big enough to support, in this case abort
        if(!startFound)
        {
            return map;
        }

        

        List<Vector2Int> path = FindPath(mapData, start, end);

        // make sure we actually found a good path
        if(path.Last() == end)
        {
            SetTiles(map, mapData, path, currentTerrain, width);
        }

        return map;
    }

    public static List<Vector2Int> FindPath(MapData mapData, Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = null;
        LayerMapFunctions.LogAction(() => { path = CustomAStar.AStar(mapData, start, end,
            (current, adjacent) => {
                if(mapData.ImprovmentMap[adjacent.x, adjacent.y] == Improvement.Road)
                {
                    return 0;
                }

                float currHeight = mapData.HeightMap[current.x, current.y];
                float adjHeight = mapData.HeightMap[adjacent.x, adjacent.y];
                return Mathf.Abs(currHeight - adjHeight) * 100;
            }); }, "road path time");
        Debug.Log($"path size {path.Count}");

        return path;
    }

    public static bool FindStartAndDir<T>(T[,] map,
        MapData mapData,
        System.Random rand,
        out Vector2Int start,
        out Vector2Int end)
    {
        List<HashSet<Vector2Int>> landComponents = new List<HashSet<Vector2Int>>(mapData.LandComponents);

        //remove all small components
        landComponents = landComponents.Where(x => x.Count > 200).ToList();

        if(landComponents.Count == 0)
        {
            start = Vector2Int.zero;
            end = Vector2Int.zero;
            return false;
        }

        // Pick a random component
        HashSet<Vector2Int> component = landComponents[rand.Next(landComponents.Count)];

        // Get the edges of the component
        HashSet<Vector2Int> edges = LayerMapFunctions.FindEdgesOfComponent(component);

        // pick a random node on the edge
        start = edges.ElementAt(rand.Next(edges.Count));

        // Find average distance of every other edge to our start
        float sumDistance = 0;
        foreach(var edge in edges)
        {
            sumDistance += (edge - start).magnitude;
        }

        float averageDistance = sumDistance / edges.Count;
        end = start;

        // Pick a random other one thats reasonably far away from the picked one
        while (edges.Count != 0)
        {
            end = edges.ElementAt(rand.Next(edges.Count));
            edges.Remove(end);

            if((end - start).magnitude > averageDistance)
            {
                break;
            }
        }

        Debug.Log($"{(end - start).magnitude}, {averageDistance} | start {start} {mapData.TerrainMap[start.x, start.y]}, end {end} {mapData.TerrainMap[end.x, end.y]}");

        return true;
    }

    public static void SetTiles<T>(T[,] map,
        MapData mapData,
        List<Vector2Int> path,
        T currentTerrain,
        int width)
    {
        Vector2 dir = Vector2.zero;

        for(int index = 0; index < path.Count; index++)
        {
            Vector2Int pos = path[index];
            map[pos.x, pos.y] = currentTerrain;

            if (index + 1 < path.Count)
            {
                dir = path[index + 1] - path[index];
            }
            
            Vector2 tangent = Vector2.Perpendicular(dir).normalized;

            Vector2 realStep = new Vector2(pos.x, pos.y);
            Vector2 delta = pos - realStep;
            while (delta.magnitude < 1)
            {
                Vector2Int stepRounded = LayerMapFunctions.RoundVector(realStep);
                if(!LayerMapFunctions.InBounds(map, stepRounded))
                {
                    break;
                }

                map[stepRounded.x, stepRounded.y] = currentTerrain;

                for (int i = -width; i <= width; i++)
                {
                    Vector2 step = (realStep + tangent * i);
                    var tangentStep = new Vector2Int(Mathf.RoundToInt(step.x), Mathf.RoundToInt(step.y));
                    if (LayerMapFunctions.InBounds(map, tangentStep))
                    {
                        map[tangentStep.x, tangentStep.y] = currentTerrain;
                    }
                }

                realStep += dir * .1f;
                delta = pos - realStep;
            }
        }
    }
}
