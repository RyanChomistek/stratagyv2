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
        FindStartAndDir(map, rand, out Vector2Int start, out Vector2 startDir, out Vector2Int end);

        Debug.Log($"start {start}, dir {startDir}, end {end}");

        float startHeight = mapData.HeightMap[start.x, start.y];

        Vector2Int gridPosition = start;
        Vector2 realPosition = start;
        Vector2 dir = startDir;
        float maxSteps = map.GetUpperBound(0) * map.GetUpperBound(0);
        int numSteps = 0;

        List<Vector2Int> path = FindPath(mapData, start, end);

        // make sure we actually found a good path
        if(path.Last() == end)
        {
            SetTiles(map, mapData, path, currentTerrain, width);
        }

        #region old path

        //while (LayerMapFunctions.InBounds(map, gridPosition) && numSteps < maxSteps)
        //{
        //    numSteps++;
        //    Vector2 tangent = Vector2.Perpendicular(dir).normalized;

        //    //check if any blockers ahead of us are impassible
        //    bool isBlocked = false;
        //    int lookAheadTiles = 5;
        //    for (int i = 2; i < lookAheadTiles; i++)
        //    {
        //        Vector2 nextMoveExtended = (realPosition + dir.normalized * i);
        //        Vector2Int nextMoveExtendedRounded = new Vector2Int(Mathf.RoundToInt(nextMoveExtended.x), Mathf.RoundToInt(nextMoveExtended.y));
        //        if (LayerMapFunctions.InBounds(mapData.TerrainMap, nextMoveExtendedRounded) &&
        //        !terrainTileLookup[mapData.TerrainMap[nextMoveExtendedRounded.x, nextMoveExtendedRounded.y]].Improvable)
        //        {
        //            isBlocked = true;
        //        }
        //    }

        //    if (isBlocked)
        //    {
        //        var tangentSign = rand.Next(2) == 0 ? -1 : 1;
        //        //if we cant go farther take a turn
        //        dir = Vector2.MoveTowards(dir, tangent * tangentSign, .1f);

        //        continue;
        //    }
            
        //    pathHistory.Add(new PathElement()
        //    {
        //        Position = realPosition,
        //        Direction = dir,
        //        Tangent = tangent
        //    });

        //    Vector2 nextMove = (realPosition + dir);

        //    //add in a random amound of deveation from dir so that its more curvy
        //    Vector2Int nextMoveRounded = new Vector2Int(Mathf.RoundToInt(nextMove.x), Mathf.RoundToInt(nextMove.y));
        //    Vector2Int delta = nextMoveRounded - gridPosition;

        //    //dont go diagonally, only keep one axis
        //    //if (System.Math.Abs(delta.x) == System.Math.Abs(delta.y))
        //    //{
        //    //    var keepxOry = rand.Next(2);
        //    //    if (keepxOry == 1)
        //    //    {
        //    //        delta = new Vector2Int(delta.x, 0);
        //    //        nextMove = realPosition + new Vector2(dir.x, 0);
        //    //    }
        //    //    else
        //    //    {
        //    //        delta = new Vector2Int(0, delta.y);
        //    //        nextMove = realPosition + new Vector2(0, dir.y);
        //    //    }
        //    //}

        //    //dir += new Vector2((float)rand.NextDouble() - .5f, (float)rand.NextDouble() - .5f) * .01f;
        //    //dir = dir.normalized / 8;

        //    var directionSign = rand.Next(2) == 0 ? -1 : 1;
        //    //if we cant go farther take a turn
        //    //dir = Vector2.MoveTowards(dir, tangent * directionSign, .008f);

        //    gridPosition = gridPosition + delta;
        //    realPosition = nextMove;
        //}

        #endregion old path

        
        return map;
    }

    static int ComputeHScore(Vector2Int pos, Vector2Int targetPos)
    {
        return Mathf.Abs(targetPos.x - pos.x) + Mathf.Abs(targetPos.y - pos.y);
    }

    public static List<Vector2Int> FindPath(MapData mapData, Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = null;
        LayerMapFunctions.LogAction(() => { path = CustomAStar.AStar(mapData, start, end,
            (current, adjacent) => {
                float currHeight = mapData.HeightMap[current.x, current.y];
                float adjHeight = mapData.HeightMap[adjacent.x, adjacent.y];
                return Mathf.Abs(currHeight - adjHeight) * 100;
            }); }, "road path time");
        Debug.Log($"path size {path.Count}");

        return path;
    }

    public static void FindStartAndDir<T>(T[,] map,
        System.Random rand,
        out Vector2Int start,
        out Vector2 dir,
        out Vector2Int end)
    {
        //start on an edge
        var startOnXOrY = rand.Next(2);
        var startAtBeginingOrEnd = rand.Next(2);
        start = new Vector2Int();
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

        dir = mid - start;

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

        end = mid;
        Vector2 realPos = new Vector2(end.x, end.y);
        while(LayerMapFunctions.InBounds(map, end))
        {
            realPos += dir;
            Vector2Int newEnd = LayerMapFunctions.RoundVector(realPos);

            if (LayerMapFunctions.InBounds(map, newEnd))
            {
                end = newEnd;
            }
            else
            {
                break;
            }
        }
    }

    public static void SetTiles<T>(T[,] map,
        MapData mapData,
        List<Vector2Int> path,
        T currentTerrain,
        int width)
    {
        Vector2 dir = Vector2.zero;
        //foreach (Vector2Int pos in path)
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
