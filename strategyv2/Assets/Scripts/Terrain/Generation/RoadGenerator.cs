using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        while (LayerMapFunctions.InBounds(map, gridPosition) && numSteps < maxSteps)
        {
            numSteps++;
            Vector2 tangent = Vector2.Perpendicular(dir).normalized;
            #region check blockers
            if (CheckForBlockers)
            {
                //check if any block ahead of us are impassible
                bool isBlocked = false;
                int lookAheadTiles = 5;
                for (int i = 2; i < lookAheadTiles; i++)
                {
                    Vector2 nextMoveExtended = (realPosition + dir.normalized * i);
                    Vector2Int nextMoveExtendedRounded = new Vector2Int(Mathf.RoundToInt(nextMoveExtended.x), Mathf.RoundToInt(nextMoveExtended.y));
                    if (LayerMapFunctions.InBounds(baseTerrainMap, nextMoveExtendedRounded) &&
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

            Vector2Int stepRounded = LayerMapFunctions.RoundVector(realPosition);
            map[stepRounded.x, stepRounded.y] = currentTerrain;
            //heightMap[stepRounded.x, stepRounded.y] = startHeight;

            for (int i = -width; i <= width; i++)
            {
                Vector2 step = (realPosition + tangent * i);
                stepRounded = new Vector2Int(Mathf.RoundToInt(step.x), Mathf.RoundToInt(step.y));
                if (LayerMapFunctions.InBounds(map, stepRounded))
                {
                    map[stepRounded.x, stepRounded.y] = currentTerrain;
                    //heightMap[stepRounded.x, stepRounded.y] = startHeight;
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
}
