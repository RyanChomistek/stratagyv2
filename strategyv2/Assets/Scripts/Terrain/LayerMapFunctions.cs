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
    
    public static void RenderMapWithTiles(Terrain[,] map, Tilemap tilemap, List<MapLayerSettings> layerSettings)
    {
        tilemap.ClearAllTiles(); //Clear the map (ensures we dont overlap)
        for (int x = 0; x <= map.GetUpperBound(0); x++) //Loop through the width of the map
        {
            for (int y = 0; y <= map.GetUpperBound(1); y++) //Loop through the height of the map
            {
                if(map[x, y] != Terrain.Empty)
                {
                    var settings = layerSettings.Find(terrain => terrain.terrain == map[x, y]);
                    tilemap.SetTile(new Vector3Int(x, y, 0), settings.tile);
                }
            }
        }
    }
    
    public static Terrain[,] RandomWalkLeftToRight(Terrain[,] map, float seed, Terrain currentTerrain)
    {
        //Seed our random
        System.Random rand = new System.Random(seed.GetHashCode());

        //Set our starting height
        int lastHeight = Random.Range(0, map.GetUpperBound(1));

        //Cycle through our width
        for (int x = 0; x < map.GetUpperBound(0); x++)
        {
            //Flip a coin
            int nextMove = rand.Next(2);
            int lastLastHeight = lastHeight;
            //If heads, and we aren't near the bottom, minus some height
            if (nextMove == 0 && lastHeight > 2)
            {
                lastHeight--;
            }
            //If tails, and we aren't near the top, add some height
            else if (nextMove == 1 && lastHeight < map.GetUpperBound(1) - 2)
            {
                lastHeight++;
            }
            //Debug.Log(lastLastHeight + " "+ lastHeight);
            
            map[x, lastHeight] = currentTerrain;

            if (x > 0 && map[x - 1, lastHeight] != currentTerrain)
            {
                Debug.Log(map[x - 1, lastHeight] +" "+ currentTerrain);
                map[x, lastLastHeight] = currentTerrain;
            }
        }
        //Return the map
        return map;
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
    
    public static Terrain[,] RandomWalk2D(Terrain[,] map, System.Random rand, Terrain currentTerrain, int width)
    {
        //start on an edge
        var startOnXOrY = rand.Next(2);
        var startAtBeginingOrEnd = rand.Next(2);
        Vector2Int start = new Vector2Int();
        if (startOnXOrY == 1)
        {
            if (startAtBeginingOrEnd == 1)
                start = new Vector2Int(Random.Range(0, map.GetUpperBound(0)), 0);
            else
                start = new Vector2Int(Random.Range(0, map.GetUpperBound(0)),  map.GetUpperBound(1));
        }
        else
        {
            if (startAtBeginingOrEnd == 1)
                start = new Vector2Int(0, Random.Range(0, map.GetUpperBound(1)));
            else
                start = new Vector2Int(map.GetUpperBound(0), Random.Range(0, map.GetUpperBound(1)));
        }

        //pick a random point in the middle to go through
        Vector2Int mid = new Vector2Int(map.GetUpperBound(0)/4, map.GetUpperBound(1)/4);

        int midRadiusX = map.GetUpperBound(0) / 2;
        int midRadiusY = map.GetUpperBound(1) / 2;
        mid += new Vector2Int(rand.Next(-midRadiusX, midRadiusX), 
            rand.Next(-midRadiusY, midRadiusY));

        Vector2 dir = mid - start;

        //if we picked to start in the middle just pick a direction
        if(dir == Vector2.zero)
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

        Vector2Int gridPosition = start;
        Vector2 realPosition = start;
        while (InBounds(map, gridPosition))
        {
            Vector2 tangent = Vector2.Perpendicular(dir).normalized;
            //Debug.Log(tangent.magnitude);

            Vector2Int stepRounded = RoundVector(realPosition);
            map[stepRounded.x, stepRounded.y] = currentTerrain;

            for (int i = -width; i < width; i++)
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
            dir += new Vector2((float)rand.NextDouble() - .5f, (float)rand.NextDouble() - .5f) * .01f;
            dir = dir.normalized / 8;
            gridPosition = gridPosition + delta;
            realPosition = nextMove;
        }

        //Debug.Log(dir/8);

        //RandomWalk2DHelper(ref map, rand, currentTerrain, start, start, dir/8, width, 0);
        return map;
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
