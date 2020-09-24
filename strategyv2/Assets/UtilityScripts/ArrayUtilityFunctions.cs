using System;
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
        var kernel = CreateGaussianKernel(kernalSize);
        return Convolution2DMT(map, kernel, numThreads);
    }

    /// <summary>
    /// remove jagged edges, mutithreaded dont use for small maps
    /// </summary>
    public static SquareArray<T> SmoothMT<T>(SquareArray<T> map, int kernalSize, Func<T, T, float, T> aggrigate, int numThreads = DefaultNumThreadsForJob)
    {
        var kernel = CreateGaussianKernel(kernalSize);
        return Convolution2DMT(map, kernel, aggrigate, numThreads);
    }

    public static SquareArray<T> SmoothMT<T>(SquareArray<T> map, SquareArray<float> kernel, Func<T, T, float, T> aggrigate, int numThreads = DefaultNumThreadsForJob)
    {
        return Convolution2DMT(map, kernel, aggrigate, numThreads);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="kernalSize"> must be a power of 2</param>
    /// <returns></returns>
    public static SquareArray<float> CreateGaussianKernel(int kernalSize)
    {
        float sigma = 1;
        float r, s = 2.0f * sigma * sigma;
        int halfSize = kernalSize / 2;
        SquareArray<float> kernel = new SquareArray<float>(kernalSize+1);

        float sum = 0;
        for (int x = -kernalSize / 2; x <= kernalSize / 2; x++)
        {
            for (int y = -kernalSize / 2; y <= kernalSize / 2; y++)
            {
                r = Mathf.Sqrt(x * x + y * y);
                float val = (Mathf.Exp(-(r * r) / s)) / (Mathf.PI * s);

                sum += val;
                kernel[x + halfSize, y + halfSize] = (val);
            }
        }

        for (int i = 0; i < kernel.SideLength; i++)
        {
            for (int j = 0; j < kernel.SideLength; j++)
            {
                kernel[i, j] /= sum;
            }
        }

        return kernel;
    }

    public static void ForMTOneDimension(int length, System.Action<int> action, int numThreads = DefaultNumThreadsForJob)
    {
        int sizePerThread = length / numThreads;
        int remainder = length - (sizePerThread * numThreads);

        // Use the thread pool to parrellize update
        using (CountdownEvent e = new CountdownEvent(1))
        {
            // TODO make these blocks instead of rows so that we get better lock perf
            for (int i = 0; i < numThreads; i++)
            {
                //Vector2Int start = new Vector2Int(0, i * sizePerThread);
                //Vector2Int end = new Vector2Int(map.SideLength, ((i + 1) * sizePerThread));

                int start = i * sizePerThread;
                int end = ((i + 1) * sizePerThread);
                // on the last thread we need to deal with the remainder
                if (i == numThreads - 1)
                {
                    end += remainder;
                }

                e.AddCount();
                ThreadPool.QueueUserWorkItem(delegate (object state)
                {
                    try
                    {
                        for (int threadWorkingIndex = start; threadWorkingIndex < end; threadWorkingIndex++)
                        {
                            
                                action(threadWorkingIndex);
                            
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

    public static void ForMTTwoDimension(int sideLength, System.Action<int, int> action, int numThreads = DefaultNumThreadsForJob)
    {
        int size = sideLength / numThreads;
        int remainder = sideLength - (size * numThreads);

        // Use the thread pool to parrellize update
        using (CountdownEvent e = new CountdownEvent(1))
        {
            // TODO make these blocks instead of rows so that we get better lock perf
            for (int i = 0; i < numThreads; i++)
            {
                Vector2Int start = new Vector2Int(0, i * size);
                Vector2Int end = new Vector2Int(sideLength, ((i + 1) * size));

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

    public static void ForMT<T>(SquareArray<T> map, System.Action<int, int> action, int numThreads = DefaultNumThreadsForJob)
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

    public static void ForMTWithThreadID(int sideLength, System.Action<int, int, int> action, int numThreads = DefaultNumThreadsForJob)
    {
        int size = sideLength / numThreads;
        int remainder = sideLength - (size * numThreads);

        // Use the thread pool to parrellize update
        using (CountdownEvent e = new CountdownEvent(1))
        {
            // TODO make these blocks instead of rows so that we get better lock perf
            for (int i = 0; i < numThreads; i++)
            {
                int threadId = i;

                e.AddCount();
                ThreadPool.QueueUserWorkItem(delegate (object state)
                {
                    try
                    {
                        Vector2Int start = new Vector2Int(0, threadId * size);
                        Vector2Int end = new Vector2Int(sideLength, ((threadId + 1) * size));

                        // on the last thread we need to deal with the remainder
                        if (threadId == numThreads - 1)
                        {
                            end.y += remainder;
                        }

                        //ProfilingUtilities.LogAction(() =>
                        //{
                            for (int y = start.y; y < end.y; y++)
                            {
                                for (int x = start.x; x < end.x; x++)
                                {
                                    action(threadId, x, y);
                                }
                            }
                        //}, $"thread {threadId} time");
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

    public static void For(int sideLength, System.Action<int, int> action, int numThreads = DefaultNumThreadsForJob)
    {
        int size = sideLength / numThreads;
        int remainder = sideLength - (size * numThreads);

        // TODO make these blocks instead of rows so that we get better lock perf
        for (int i = 0; i < numThreads; i++)
        {
            Vector2Int start = new Vector2Int(0, i * size);
            Vector2Int end = new Vector2Int(sideLength, ((i + 1) * size));

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

    public static void For(SquareArray<float> map, System.Action<int, int> action, int numThreads = DefaultNumThreadsForJob)
    {
        For(map.SideLength, action, numThreads);
    }

    public static SquareArray<float> Convolution2DMT(SquareArray<float> map, SquareArray<float> kernel, int numThreads = DefaultNumThreadsForJob)
    {
        var convolutedArr = new SquareArray<float>(map.SideLength);
        int k_h = kernel.SideLength;
        int k_w = kernel.SideLength;

        //int size = (map.GetUpperBound(1) + 1) / numThreads;
        int mapSize = map.SideLength;

        ForMT(map, (x, y) => {
            for (int k_x = 0; k_x < k_w; k_x++)
            {
                for (int k_y = 0; k_y < k_h; k_y++)
                {
                    float k = kernel[k_x, k_y];
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

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="map"></param>
    /// <param name="kernel"></param>
    /// <param name="aggrigate"> arg1 current total, arg2 current value, arg3 scale | returns a new total</param>
    /// <param name="numThreads"></param>
    /// <returns></returns>
    public static SquareArray<T> Convolution2DMT<T>(
        SquareArray<T> map,
        SquareArray<float> kernel,
        Func<T, T, float, T> aggrigate,
        int numThreads = DefaultNumThreadsForJob)
    {
        var convolutedArr = new SquareArray<T>(map.SideLength);
        int k_h = kernel.SideLength;
        int k_w = kernel.SideLength;

        int mapSize = map.SideLength;

        ForMT(map, (x, y) =>
        {
            for (int k_x = 0; k_x < k_w; k_x++)
            {
                for (int k_y = 0; k_y < k_h; k_y++)
                {
                    float k = kernel[k_x, k_y];
                    int heightMapOffset_x = x + k_x - (k_w / 2);
                    int heightMapOffset_y = y + k_y - (k_h / 2);

                    if (map.InBounds(heightMapOffset_x, heightMapOffset_y))
                    {
                        convolutedArr[x, y] = aggrigate(convolutedArr[x, y], map[heightMapOffset_x, heightMapOffset_y], k);
                    }

                }
            }
        }, numThreads);

        return convolutedArr;
    }

    public static void Scale(SquareArray<float> arr, float scale = 1)
    {
        for (int x = 0; x < arr.SideLength; x++)
        {
            for (int y = 0; y < arr.SideLength; y++)
            {
                arr[x, y] *= scale;
            }
        }
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

        Normalize(arr, min, max);
    }

    public static void Normalize(SquareArray<float> arr, float min, float max, float scale = 1)
    {
        for (int x = 0; x < arr.SideLength; x++)
        {
            for (int y = 0; y < arr.SideLength; y++)
            {
                arr[x, y] = Mathf.InverseLerp(min, max, arr[x, y]) * scale;
            }
        }
    }

    public static void Invert(SquareArray<float> arr, float min, float max, float scale = 1)
    {
        for (int x = 0; x < arr.SideLength; x++)
        {
            for (int y = 0; y < arr.SideLength; y++)
            {
                arr[x, y] = max - arr[x,y];
            }
        }
    }

    public static float StandardDeviation(SquareArray<float> arr, out float mean, out float std, bool ignoreZero = false)
    {
        mean = 0;
        int len = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            if(ignoreZero)
            {
                if(arr[i] != 0)
                {
                    len++;
                }
            }
            else
            {
                len++;
            }

            mean += arr[i];
        }

        mean /= len;
        std = 0;

        for (int i = 0; i < arr.Length; i++)
        {
            std += Mathf.Pow(mean - arr[i], 2);
        }

        std /= len;
        return Mathf.Sqrt(std);
    }

    public static List<HashSet<Vector2Int>> FindComponents<T>(T target, int bufferWidth, SquareArray<T> tileMap)
    {
        SquareArray<int> componentMap = new SquareArray<int>(tileMap.SideLength);

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
                if (tileMap[x, y].Equals(target) && componentMap[x, y] == 0)
                {
                    HashSet<Vector2Int> componet = FloodFill(
                        target,
                        ref componentMap,
                        new Vector2Int(x, y),
                        componentCounter,
                        floodFillDirections,
                        bufferWidth,
                        tileMap);

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
        SquareArray<T> tileMap)
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
                if (IsUnmarkedTile(target, newPos, adjacentDirections, ref componentMap, tileMap))
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
        SquareArray<T> tileMap)
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

    public static void Add(SquareArray<float> arr, float amt)
    {
        for (int x = 0; x < arr.SideLength; x++)
        {
            for (int y = 0; y < arr.SideLength; y++)
            {
                arr[x, y] += amt;
            }
        }
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

    public static SquareArray<Color32> Blend(SquareArray<Color32> bigArray, SquareArray<Color32> smallArray, SquareArray<float> kernel)
    {
        float bigToSmallCordScale = smallArray.SideLength / (float) bigArray.SideLength;
        float smallToBigCordScale = bigArray.SideLength / (float) smallArray.SideLength;
        //float maxDistance = smallToBigCordScale * tileLookDistance;
        int tileLookDistance = kernel.SideLength / 2;
        SquareArray<Color32> output = new SquareArray<Color32>(bigArray.SideLength);

        ArrayUtilityFunctions.ForMT(bigArray, (x, y) =>
            {
                Vector2Int bigArrayPos = new Vector2Int(x, y);
                Vector2Int smallArrayPos = VectorUtilityFunctions.FloorVector(new Vector2(x, y) * bigToSmallCordScale);

                // loop through all of the neightbor tiles
                for (int dx = -tileLookDistance; dx <= tileLookDistance; dx++)
                {
                    for (int dy = -tileLookDistance; dy <= tileLookDistance; dy++)
                    {
                        var tileDelta = new Vector2Int(dx, dy);
                        var kernelPos = new Vector2Int(dx + tileLookDistance, dy + tileLookDistance);
                        Vector2Int smallArrayNeighborPos = smallArrayPos + tileDelta;
                        if (smallArray.InBounds(smallArrayNeighborPos))
                        {
                            // Get the midpoint of the neighbor tile
                            Vector2 bigArrayNeighborTilePosCenter =
                                VectorUtilityFunctions.Vec2IntToVec2(smallArrayNeighborPos) * smallToBigCordScale +
                                    new Vector2(smallToBigCordScale, smallToBigCordScale) / 2;

                            float distance = (bigArrayNeighborTilePosCenter - bigArrayPos).magnitude;
                            Color neighborColor = bigArray[VectorUtilityFunctions.FloorVector(bigArrayNeighborTilePosCenter)];
                            output[bigArrayPos] += neighborColor * (1/ distance);
                        }
                    }
                }

                // normalize
            });

        return output;
    }
}
