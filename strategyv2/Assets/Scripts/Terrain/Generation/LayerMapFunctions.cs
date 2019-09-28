using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Threading.Tasks;

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

    public static void Droplets<T>(ref T[,] map,
        ref float[,] heightMap,
        ref Vector2[,] gradientMap,
        System.Random rand,
        T currentTerrain,
        float percentCovered)
    {
        float[,] dropletMap = new float[heightMap.GetUpperBound(0) + 1, heightMap.GetUpperBound(1) + 1];
        //float[,] dropletPathMap = new float[heightMap.GetUpperBound(0) + 1, heightMap.GetUpperBound(1) + 1];

        int numDroplets = (int) (heightMap.GetUpperBound(0) * heightMap.GetUpperBound(1) * percentCovered);
        //int numDroplets = 25;
        
        for(int i = 0; i < numDroplets; i++)
        {
            Vector2 start = new Vector2(Random.Range(0, map.GetUpperBound(0)), Random.Range(0, map.GetUpperBound(1)));
            var realPos = start;
            var gridPos = RoundVector(start);
            Vector2 momentum = gradientMap[gridPos.x, gridPos.y].normalized * 20;

            int cnt = 0;
            while (momentum.magnitude > 1 && cnt < 100)
            {
                cnt++;
                dropletMap[gridPos.x, gridPos.y] += 1;
                var gradient = gradientMap[gridPos.x, gridPos.y].normalized;
                
                var delta = gradient * -1;
                momentum += delta;
                momentum *= .98f;

                var nextStep = realPos + delta;
                var gridNextStep = RoundVector(nextStep);
                if (!MapManager.InBounds(heightMap, gridNextStep.x, gridNextStep.y))
                {
                    break;
                }

                realPos = nextStep;
                gridPos = gridNextStep;
            }

            //Debug.Log($"{i}: {cnt} ending momentum {momentum} {momentum.magnitude}");

            dropletMap[gridPos.x, gridPos.y] += 1;
        }

        Normalize(ref dropletMap);
        SmoothMT(ref dropletMap, 5, 4);

        for (int x = 0; x <= dropletMap.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= dropletMap.GetUpperBound(1); y++)
            {
                if(dropletMap[x, y] > .004f)
                {
                    map[x, y] = currentTerrain;
                    heightMap[x, y] -= dropletMap[x, y];
                }
                    
            }
        }
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
        float min = 1000, max = -1000;

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
