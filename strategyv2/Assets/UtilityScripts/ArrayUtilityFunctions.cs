using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ArrayUtilityFunctions
{
    public const int DefaultNumThreadsForJob = 16;

    /// <summary>
    /// calculates the bilinear interpolation from the 4 points given u,v
    /// </summary>
    /// <param name="a"> top left </param>
    /// <param name="b"> top right </param>
    /// <param name="c"> bottom right </param>
    /// <param name="d"> bottom left </param>
    /// <param name="u"> horz position </param>
    /// <param name="v"> vert position </param>
    /// <returns></returns>
    public static float QuadLerp(float a, float b, float c, float d, float u, float v)
    {
        v = 1 - v;
        float abu = Mathf.Lerp(a, b, u);
        float dcu = Mathf.Lerp(d, c, u);
        return Mathf.Lerp(abu, dcu, v);
    }

    /// <summary>
    /// remove jagged edges, mutithreaded dont use for small maps
    /// </summary>
    public static SquareArray<float> SmoothMT(SquareArray<float> map, int kernalSize, int numThreads = DefaultNumThreadsForJob)
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

        return Convolution2DMT(map, kernel, numThreads);
    }

    public static void ForMT(SquareArray<float> map, System.Action<int, int> action, int numThreads = DefaultNumThreadsForJob)
    {
        int size = map.SideLength / numThreads;
        int remainder = map.SideLength - (size * numThreads);

        // Use the thread pool to parrellize update
        using (CountdownEvent e = new CountdownEvent(1))
        {
            // TODO make these blocks instead of rows so that we get better lock perf
            for (int i = 0; i < numThreads; i++)
            {
                Vector2Int start = new Vector2Int(0, i * size);
                Vector2Int end = new Vector2Int(map.SideLength, ((i + 1) * size));

                // on the last thread we need to deal with the remainder
                if (i == numThreads - 1)
                {
                    end.y += remainder;
                }

                e.AddCount();
                ThreadPool.QueueUserWorkItem(delegate (object state)
                {
                    try
                    {
                        for (int y = start.y; y < end.y; y++)
                        {
                            for (int x = start.x; x < end.x; x++)
                            {
                                action(x, y);
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

    public static void For(SquareArray<float> map, System.Action<int, int> action, int numThreads = DefaultNumThreadsForJob)
    {
        int size = map.SideLength / numThreads;
        int remainder = map.SideLength - (size * numThreads);

        // TODO make these blocks instead of rows so that we get better lock perf
        for (int i = 0; i < numThreads; i++)
        {
            Vector2Int start = new Vector2Int(0, i * size);
            Vector2Int end = new Vector2Int(map.SideLength, ((i + 1) * size));

            // on the last thread we need to deal with the remainder
            if (i == numThreads - 1)
            {
                end.y += remainder;
            }

            for (int y = start.y; y < end.y; y++)
            {
                for (int x = start.x; x < end.x; x++)
                {
                    action(x, y);
                }
            }
        }
    }

    public static SquareArray<float> Convolution2D(SquareArray<float> arr, List<List<float>> kernel)
    {
        //var convolutedArr = new float[arr.GetUpperBound(0) + 1, arr.GetUpperBound(1) + 1];
        SquareArray<float> convolutedArr = new SquareArray<float>(arr.SideLength);
        int k_h = kernel.Count;
        int k_w = kernel[0].Count;

        //do a 2d convolution
        for (int x = 0; x < arr.SideLength; x++)
        {
            for (int y = 0; y < arr.SideLength; y++)
            {
                for (int k_x = 0; k_x < k_w; k_x++)
                {
                    for (int k_y = 0; k_y < k_h; k_y++)
                    {
                        float k = kernel[k_y][k_x];
                        int heightMapOffset_x = x + k_x - (k_w / 2);
                        int heightMapOffset_y = y + k_y - (k_h / 2);

                        if (arr.InBounds(heightMapOffset_x, heightMapOffset_y))
                            convolutedArr[x, y] += arr[heightMapOffset_x, heightMapOffset_y] * k;
                    }
                }
            }
        }

        Normalize(convolutedArr);

        return convolutedArr;
    }

    public static SquareArray<float> Convolution2DMT(SquareArray<float> map, List<List<float>> kernel, int numThreads = DefaultNumThreadsForJob)
    {
        var convolutedArr = new SquareArray<float>(map.SideLength);
        int k_h = kernel.Count;
        int k_w = kernel[0].Count;

        //int size = (map.GetUpperBound(1) + 1) / numThreads;
        int mapSize = map.SideLength;

        ForMT(map, (x, y) => {
            for (int k_x = 0; k_x < k_w; k_x++)
            {
                for (int k_y = 0; k_y < k_h; k_y++)
                {
                    float k = kernel[k_y][k_x];
                    int heightMapOffset_x = x + k_x - (k_w / 2);
                    int heightMapOffset_y = y + k_y - (k_h / 2);

                    if (map.InBounds(heightMapOffset_x, heightMapOffset_y))
                        convolutedArr[x, y] += map[heightMapOffset_x, heightMapOffset_y] * k;
                }
            }
        }, numThreads);

        Normalize(convolutedArr);

        return convolutedArr;
    }

    public static void Normalize(SquareArray<float> arr)
    {
        float min = float.MaxValue, max = float.MinValue;

        for (int x = 0; x < arr.SideLength; x++)
        {
            for (int y = 0; y < arr.SideLength; y++)
            {
                min = Mathf.Min(min, arr[x, y]);
                max = Mathf.Max(max, arr[x, y]);
            }
        }

        for (int x = 0; x < arr.SideLength; x++)
        {
            for (int y = 0; y < arr.SideLength; y++)
            {
                arr[x, y] = Mathf.InverseLerp(min, max, arr[x, y]);
            }
        }
    }

    public static List<HashSet<Vector2Int>> FindComponents<T>(T target, int mapSize, int bufferWidth, ref SquareArray<T> terrainTileMap)
    {
        SquareArray<int> componentMap = new SquareArray<int>(mapSize);

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
        for (int y = 0; y < componentMap.SideLength; y++)
        {
            for (int x = 0; x < componentMap.SideLength; x++)
            {
                if (terrainTileMap[x, y].Equals(target) && componentMap[x, y] == 0)
                {
                    HashSet<Vector2Int> componet = FloodFill(
                        target,
                        ref componentMap,
                        new Vector2Int(x, y),
                        componentCounter,
                        floodFillDirections,
                        bufferWidth,
                        ref terrainTileMap);

                    components.Add(componet);
                    componentCounter++;
                }

            }
        }

        return components;
    }

    private static HashSet<Vector2Int> FloodFill<T>(
        T target,
        ref SquareArray<int> componentMap,
        Vector2Int startPos,
        int componentNumber,
        Vector2Int[] floodFillDirections,
        int bufferWidth,
        ref SquareArray<T> tileMap)
    {
        HashSet<Vector2Int> componet = new HashSet<Vector2Int>();

        Stack<Vector2Int> cellsToBeProcessed = new Stack<Vector2Int>();
        cellsToBeProcessed.Push(startPos);

        List<Vector2Int> adjacentDirections = new List<Vector2Int>();

        if (bufferWidth > 0)
        {
            adjacentDirections = new List<Vector2Int>() {
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

            for (int i = 0; i < bufferWidth - 1; i++)
            {
                List<Vector2Int> temp_adjacentDirections = new List<Vector2Int>();
                foreach (var dir in adjacentDirections)
                {
                    temp_adjacentDirections.Add(dir);
                    foreach (var dir2 in adjacentDirections)
                    {
                        temp_adjacentDirections.Add(dir + dir2);
                    }
                }
            }
        }

        while (cellsToBeProcessed.Count > 0)
        {
            Vector2Int currPos = cellsToBeProcessed.Pop();
            componet.Add(currPos);
            componentMap[currPos.x, currPos.y] = componentNumber;

            foreach (var dir in floodFillDirections)
            {
                Vector2Int newPos = currPos + dir;
                if (IsUnmarkedTile(target, newPos, adjacentDirections, ref componentMap, ref tileMap))
                {
                    cellsToBeProcessed.Push(newPos);
                }
            }
        }

        return componet;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="terrain"></param>
    /// <param name="Pos"></param>
    /// <param name="bufferWidth"> if this is greater than 0 a buffer zone around the components will be included </param>
    /// <param name="componentMap"></param>
    /// <param name="tileMap"></param>
    /// <returns></returns>
    private static bool IsUnmarkedTile<T>(
        T target,
        Vector2Int Pos,
        List<Vector2Int> adjacentDirections,
        ref SquareArray<int> componentMap,
        ref SquareArray<T> tileMap)
    {
        bool inBounds = componentMap.InBounds(Pos);
        if (inBounds && componentMap[Pos.x, Pos.y] == 0)
        {
            bool isCorrectTileType = tileMap[Pos.x, Pos.y].Equals(target);

            if (isCorrectTileType)
            {
                return true;
            }
            else
            {
                // check adjacent tiles
                foreach (var adj in adjacentDirections)
                {
                    Vector2Int adjacentPos = new Vector2Int(Pos.x + adj.x, Pos.y + adj.y);
                    if (componentMap.InBounds(adjacentPos) && tileMap[adjacentPos.x, adjacentPos.y].Equals(target))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static SquareArray<float> Add(SquareArray<float> left, SquareArray<float> right)
    {
        SquareArray<float> res = new SquareArray<float>(left.SideLength);
        for (int i = 0; i < left.Length; i++)
        {
            res[i] = left[i] + right[i];
        }

        return res;
    }

    public static SquareArray<float> Subtract(SquareArray<float> left, SquareArray<float> right)
    {
        SquareArray<float> res = new SquareArray<float>(left.SideLength);
        for (int i = 0; i < left.Length; i++)
        {
            res[i] = left[i] - right[i];
        }

        return res;
    }
}
