using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LayerMapFunctions : MonoBehaviour
{
    public static Terrain[,] GenerateArray(int width, int height, Terrain defaultTerrain)
    {
        Terrain[,] map = new Terrain[width, height];
        
        for (int x = 0; x <= map.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++)
            {
                map[x, y] = defaultTerrain;
            }
        }
        return map;
    }

    public static void FillHeightRange(ref Terrain[,] map,
        ref float[,] heightMap,
        Terrain currentTerrain,
        float minHeight,
        float maxHeight)
    {
        for (int x = 0; x <= map.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++)
            {
                if (heightMap[x,y] > minHeight && heightMap[x, y] < maxHeight)
                {
                    map[x, y] = currentTerrain;
                    heightMap[x, y] = minHeight;
                }
            }
        }
    }

    public static bool InBounds(Terrain[,] map, Vector2Int position)
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

    public static Terrain[,] RandomSquares(Terrain[,] map, System.Random rand, Terrain currentTerrain, int width)
    {
        Vector2Int mid = new Vector2Int(map.GetUpperBound(0) /2, map.GetUpperBound(1) / 2);

        int midRadiusX = map.GetUpperBound(0) / 2;
        int midRadiusY = map.GetUpperBound(1) / 2;
        mid += new Vector2Int(rand.Next(-midRadiusX, midRadiusX),
            rand.Next(-midRadiusY, midRadiusY));

        for(int i = -width; i < width; i++)
        {
            for (int j = -width; j < width; j++)
            {
                var point = mid + new Vector2Int(i, j);
                if(InBounds(map, point))
                {
                    map[point.x, point.y] = currentTerrain;
                }
            }
        }

        return map;
    }

    public static void FollowGradient(ref Terrain[,] map,
        ref float[,] heightMap,
        ref Vector2[,] gradientMap,
        System.Random rand,
        Terrain currentTerrain,
        float minStartHeight,
        float minStopHeight,
        float maxWidth,
        float widthChangeThrotle)
    {
        //pick a random spot
        Vector2Int start = new Vector2Int(Random.Range(0, map.GetUpperBound(0)), Random.Range(0, map.GetUpperBound(1)));

        Debug.Log("start "+start);
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
                Debug.Log("HIT LOCAL MAX");
                break;
            }


            prevLocations.Add(currPos);

            var gradient = gradientMap[currPos.x, currPos.y].normalized + momentum;
            currPos += RoundVector(gradient);
            momentum = momentum * .9f;
        }
        Debug.Log("end " + currPos);
        //once we reach the min start height, start going back down, but leave the terrain behind
        float width = 0;
        prevLocations.Clear();
        while (heightMap[currPos.x, currPos.y] > minStopHeight)
        {
            map[currPos.x, currPos.y] = currentTerrain;
            
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
                Debug.Log("HIT LOCAL MAX");
                break;
            }
            prevLocations.Add(currPos);

            var delta = RoundVector(gradient) * -1;
            Debug.Log(delta + " " + (System.Math.Abs(delta.x) == System.Math.Abs(delta.y)));

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

    public static Terrain[,] RandomWalk2D(ref Terrain[,] map,
        ref Terrain[,] baseTerrainMap,
        System.Random rand,
        Terrain currentTerrain,
        int width,
        bool CheckForBlockers,
        Dictionary<Terrain, TerrainTileSettings> terrainTileLookup)
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
        Vector2Int mid = new Vector2Int(map.GetUpperBound(0) / 4, map.GetUpperBound(1) / 4);

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
                    !terrainTileLookup[baseTerrainMap[nextMoveExtendedRounded.x, nextMoveExtendedRounded.y]].tile.Improvable)
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

            for (int i = -width; i <= width; i++)
            {
                Vector2 step = (realPosition + tangent * i);
                stepRounded = new Vector2Int(Mathf.RoundToInt(step.x), Mathf.RoundToInt(step.y));
                if (InBounds(map, stepRounded))
                    map[stepRounded.x, stepRounded.y] = currentTerrain;
            }

            Vector2 nextMove = (realPosition + dir);
            
            //add in a random amound of deveation from dir so that its more curvy
            Vector2Int nextMoveRounded = new Vector2Int(Mathf.RoundToInt(nextMove.x), Mathf.RoundToInt(nextMove.y));
            Vector2Int delta = nextMoveRounded - gridPosition;

            //dont go diagonally, only keep one axis
            if (System.Math.Abs(delta.x) == System.Math.Abs(delta.y))
            {
                var keepxOry = rand.Next(2);
                if (keepxOry == 1)
                {
                    delta = new Vector2Int(delta.x, 0);
                    nextMove = realPosition + new Vector2(dir.x, 0);
                }
                else
                {
                    delta = new Vector2Int(0, delta.y);
                    nextMove = realPosition + new Vector2(0, dir.y);
                }
            }

            //dir += new Vector2((float)rand.NextDouble() - .5f, (float)rand.NextDouble() - .5f) * .01f;
            //dir = dir.normalized / 8;

            var directionSign = rand.Next(2) == 0 ? -1 : 1;
            //if we cant go farther take a turn
            dir = Vector2.MoveTowards(dir, tangent * directionSign, .008f);

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

    public static Terrain[,] PerlinNoise(ref Terrain[,] map, Terrain currentTerrain, System.Random rand, float scale, float threshold)
    {
        Vector2 shift = new Vector2((float)rand.NextDouble() * 1000, (float)rand.NextDouble() * 1000);
        for (int x = 0; x <= map.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++)
            {
                float xCoord = shift.x + x / (map.GetUpperBound(0) + 1f) * scale;
                float yCoord = shift.y + y / (map.GetUpperBound(1) + 1f) * scale;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);

                if(sample > threshold)
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
}
