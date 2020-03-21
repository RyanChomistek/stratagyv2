using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ErosionBlockType
{
    Grid,
    Random
}

public class MapBlock
{
    public Vector2Int Offset;
    public SquareArray<float> HeightMapChunk;
    public SquareArray<float> WaterMapChunk;
}

[CreateNodeMenu("")]
public class BlockErosionNode : TerrainNode
{
    [Input] public ComputeShader erosion;
    [Input] public int MaxNumThreads = 65535;
    [Input] public int numDropletsPerCell = 8;
    [Input] public int maxLifetime = 30;
    [Input] public float sedimentCapacityFactor = 3;
    [Input] public float minSedimentCapacity = .01f;
    [Input] public float depositSpeed = 0.3f;
    [Input] public float erodeSpeed = 0.3f;
    [Input] public float evaporateSpeed = .01f;
    [Input] public float gravity = 4;
    [Input] public float startSpeed = 1;
    [Input] public float startWater = 1;
    [Input] public float inertia = 0.3f;

    [Input] public int ErosionBrushRadius = 3;
    [Input] public int numBlocks = 3;
    [Input] public int BlockSize = 256;
    [Input] public int GridSize = 256;

    [Input] public float FallOffRatio = .1f;
    [Input] public Vector2Int OffsetFromStart = Vector2Int.zero;

    [Input] public float[] InputHeightMap = null;
    [Input] public float[] InputWaterMap = null;

    [Output] public float[] OutputHeightMap = null;
    [Output] public float[] OutputWaterMap = null;
    [Output] public int[] HitMap = null;
    public int HeightMapSize = 128 * 128;


    public override object GetValue(XNode.NodePort port)
    {
        if (port.fieldName == "OutputHeightMap")
            return OutputHeightMap;

        if (port.fieldName == "OutputWaterMap")
            return OutputWaterMap;

        if (port.fieldName == "HitMap")
            return HitMap;

        return null;
    }

    public override void Recalculate()
    {
        ComputeShader erosion = GetInputValue("erosion", this.erosion);
        int MaxNumThreads = GetInputValue("MaxNumThreads", this.MaxNumThreads);
        int numDropletsPerCell = GetInputValue("numDropletsPerCell", this.numDropletsPerCell);
        int maxLifetime = GetInputValue("maxLifetime", this.maxLifetime);
        float sedimentCapacityFactor = GetInputValue("sedimentCapacityFactor", this.sedimentCapacityFactor);
        float minSedimentCapacity = GetInputValue("minSedimentCapacity", this.minSedimentCapacity);
        float depositSpeed = GetInputValue("depositSpeed", this.depositSpeed);
        float erodeSpeed = GetInputValue("erodeSpeed", this.erodeSpeed);
        float evaporateSpeed = GetInputValue("evaporateSpeed", this.evaporateSpeed);
        float gravity = GetInputValue("gravity", this.gravity);
        float startSpeed = GetInputValue("startSpeed", this.startSpeed);
        float startWater = GetInputValue("startWater", this.startWater);
        float inertia = GetInputValue("inertia", this.inertia);
        int numBlocks = GetInputValue("numBlocks", this.numBlocks);
        int BlockSize = GetInputValue("BlockSize", this.BlockSize);
        int GridSize = GetInputValue("GridSize", this.GridSize);
        float FallOffRatio = GetInputValue("FallOffRatio", this.FallOffRatio);
        Vector2Int OffsetFromStart = GetInputValue("OffsetFromStart", this.OffsetFromStart);

        float[] InputHeightMap = GetInputValue("InputHeightMap", this.InputHeightMap);
        float[] InputWaterMap = GetInputValue("InputWaterMap", this.InputWaterMap);

        if (InputHeightMap != null && InputWaterMap != null && InputHeightMap.Length > 0 && InputWaterMap.Length > 0)
        {
            ErosionSettings ES = new ErosionSettings()
            {
                erosion = erosion,
                MaxNumThreads = MaxNumThreads,
                numDropletsPerCell = numDropletsPerCell,
                maxLifetime = maxLifetime,
                sedimentCapacityFactor = sedimentCapacityFactor,
                minSedimentCapacity = minSedimentCapacity,
                depositSpeed = depositSpeed,
                erodeSpeed = erodeSpeed,
                evaporateSpeed = evaporateSpeed,
                gravity = gravity,
                startSpeed = startSpeed,
                startWater = startWater,
                inertia = inertia,
                ErosionBrushRadius = ErosionBrushRadius,
            };

            OutputHeightMap = (float[])InputHeightMap.Clone();
            SquareArray<float> heightMapSquare = new SquareArray<float>(OutputHeightMap);

            OutputWaterMap = (float[])InputWaterMap.Clone();
            SquareArray<float> WaterMapSquare = new SquareArray<float>(OutputWaterMap);

            HitMap = new int[OutputWaterMap.Length];
            SquareArray<int> HitMapSquare = new SquareArray<int>(HitMap);

            //int largeBlockSize = MaxMapSideLength; // 1024
            //Run(heightMapSquare, WaterMapSqaure, ES, Vector2Int.zero, largeBlockSize);

            //int smallBlockSize = MaxMapSideLength / 2; // 512
            Run(heightMapSquare, WaterMapSquare, HitMapSquare, ES, BlockSize, GridSize, numBlocks, FallOffRatio, OffsetFromStart);

            HeightMapSize = InputHeightMap.Length;
        }
    }

    public void Run(
        SquareArray<float> heightMapSquare,
        SquareArray<float> WaterMapSquare,
        SquareArray<int> HitMapSquare,
        ErosionSettings ES,
        int blocksize,
        int gridSize,
        int numBlocks,
        float FallOffRatio,
        Vector2Int OffsetFromStart)
    {
        List<MapBlock> mapBlocks = null;
        mapBlocks = GetMapBlocks(heightMapSquare, WaterMapSquare, HitMapSquare, blocksize, gridSize, OffsetFromStart);

        // can probably run this in parallel
        for (int i = 0; i < mapBlocks.Count; i++)
        {
            var block = mapBlocks[i];
            ErodeGPU(ES, blocksize - ES.ErosionBrushRadius*2, blocksize, block.HeightMapChunk.Array, block.WaterMapChunk.Array);

            SetSubBlock(heightMapSquare, block.Offset, block.HeightMapChunk, FallOffRatio);
            SetSubBlock(WaterMapSquare, block.Offset, block.WaterMapChunk, FallOffRatio);
        }
    }

    public List<MapBlock> GetRandomMapBlocks(SquareArray<float> rawHeightMap, SquareArray<float> rawWaterMap, SquareArray<int> HitMapSqaure, int blocksize, int numBlocks)
    {
        List<MapBlock> blocks = new List<MapBlock>();

        if(rawHeightMap.SideLength != rawWaterMap.SideLength)
        {
            Debug.LogError("height and water map side lengths dont match");
        }

        for(int i = 0; i < numBlocks; i++)
        {
            // some how we want this to happen uniformly, currently we bias twords the center
            int x = (this.graph as TerrainGeneratorGraph).Rand.Next(0, rawHeightMap.SideLength - blocksize);
            int y = (this.graph as TerrainGeneratorGraph).Rand.Next(0, rawHeightMap.SideLength - blocksize);
            Vector2Int offset = new Vector2Int(x, y);
            SquareArray<float> heightSubArray = GetSubBlock(rawHeightMap, offset, blocksize, HitMapSqaure);
            SquareArray<float> waterSubArray = GetSubBlock(rawWaterMap, offset, blocksize, HitMapSqaure);
            blocks.Add(new MapBlock()
            {
                Offset = offset,
                HeightMapChunk = heightSubArray,
                WaterMapChunk = waterSubArray,
            });
        }

        return blocks;
    }

    public List<MapBlock> GetMapBlocks(SquareArray<float> rawHeightMap, SquareArray<float> rawWaterMap, SquareArray<int> HitMapSqaure, int blocksize, int gridSize, Vector2Int OffsetFromStart)
    {
        List<MapBlock> blocks = new List<MapBlock>();

        if (rawHeightMap.SideLength != rawWaterMap.SideLength)
        {
            Debug.LogError("height and water map side lengths dont match");
        }

        int sizeRatio = Mathf.CeilToInt(blocksize / (float)gridSize);

        int numGridStepsX = rawHeightMap.SideLength / gridSize;
        int numGridStepsY = rawHeightMap.SideLength / gridSize;
        for(int blockX = -1; blockX < numGridStepsX; blockX++)
        {
            for (int blockY = -1; blockY < numGridStepsY; blockY++)
            {
                Vector2Int offset = new Vector2Int(blockX, blockY) * gridSize + OffsetFromStart;
                SquareArray<float> heightSubArray = GetSubBlock(rawHeightMap, offset, blocksize, HitMapSqaure);
                SquareArray<float> waterSubArray = GetSubBlock(rawWaterMap, offset, blocksize, HitMapSqaure);
                blocks.Add(new MapBlock()
                {
                    Offset = offset,
                    HeightMapChunk = heightSubArray,
                    WaterMapChunk = waterSubArray,
                });
            }
        }

        return blocks;
    }

    public SquareArray<float> GetSubBlock(SquareArray<float> array, Vector2Int offset, int newSideLength, SquareArray<int> HitMapSqaure)
    {
        SquareArray<float> newSquareArray = new SquareArray<float>(newSideLength);

        for (int x = 0; x < newSideLength; x++)
        {
            for (int y = 0; y < newSideLength; y++)
            {
                int offsetX = x + offset.x;
                int offsetY = y + offset.y;

                if(array.InBounds(offsetX, offsetY))
                {
                    newSquareArray[x, y] = array[offsetX, offsetY];
                    HitMapSqaure[offsetX, offsetY]++;
                }
                else
                {
                    newSquareArray[x, y] = 0;
                }
                
            }
        }

        return newSquareArray;
    }

    public void SetSubBlock(SquareArray<float> array, Vector2Int offset, SquareArray<float> other, float fallOffScale)
    {
        Vector2 center = new Vector2(other.SideLength / (float)2, other.SideLength / (float)2);
        float maxDistanceFromCenter = center.x;

        for (int x = 0; x < other.SideLength; x++)
        {
            for (int y = 0; y < other.SideLength; y++)
            {
                int offsetX = x + offset.x;
                int offsetY = y + offset.y;

                if(array.InBounds(offsetX, offsetY))
                {
                    float delta = other[x, y] - array[offsetX, offsetY];
                    //delta = delta / (float) HitMapSqaure[offsetX, offsetY];
                    float distanceFromCenter = (new Vector2(x, y) - center).magnitude * fallOffScale;
                    float distanceFromCenterNormalized = Mathf.Max(0, 1 - (distanceFromCenter / (maxDistanceFromCenter)));
                    array[offsetX, offsetY] += delta * distanceFromCenterNormalized;
                }
            }
        }
    }

    private void ErodeGPU(ErosionSettings Settings, int mapSize, int mapSizeWithBorder, float[] HeightMap, float[] WaterMap)
    {
        SquareArray<float> heightMapSquare = new SquareArray<float>(HeightMap);

        // Create brush
        List<int> brushIndexOffsets = new List<int>();
        List<float> brushWeights = new List<float>();

        float weightSum = 0;
        for (int brushY = -Settings.ErosionBrushRadius; brushY <= Settings.ErosionBrushRadius; brushY++)
        {
            for (int brushX = -Settings.ErosionBrushRadius; brushX <= Settings.ErosionBrushRadius; brushX++)
            {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst < Settings.ErosionBrushRadius * Settings.ErosionBrushRadius)
                {
                    brushIndexOffsets.Add(brushY * mapSize + brushX);
                    float brushWeight = 1 - Mathf.Sqrt(sqrDst) / Settings.ErosionBrushRadius;
                    weightSum += brushWeight;
                    brushWeights.Add(brushWeight);
                }
            }
        }
        for (int i = 0; i < brushWeights.Count; i++)
        {
            brushWeights[i] /= weightSum;
        }

        // Send brush data to compute shader
        ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int));
        ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(int));
        brushIndexBuffer.SetData(brushIndexOffsets);
        brushWeightBuffer.SetData(brushWeights);
        Settings.erosion.SetBuffer(0, "brushIndices", brushIndexBuffer);
        Settings.erosion.SetBuffer(0, "brushWeights", brushWeightBuffer);

        //

        //Debug.Log(numThreads);

        //
        //Debug.Log($"Erosion: num elements = {HeightMap.Length}, num GPU Threads = {numThreads}, each doing {numElementsToProcess} elements");

        // HeightMap buffer
        ComputeBuffer mapBuffer = new ComputeBuffer(HeightMap.Length, sizeof(float));
        mapBuffer.SetData(HeightMap);
        Settings.erosion.SetBuffer(0, "map", mapBuffer);

        // WaterMap buffer
        //ComputeBuffer waterMapBuffer = new ComputeBuffer(WaterMap.Length, sizeof(float));
        //waterMapBuffer.SetData(WaterMap);
        //Settings.erosion.SetBuffer(0, "waterMap", waterMapBuffer);

        // Generate random indices for droplet placement
        int numDroplets = (int) (Settings.numDropletsPerCell * HeightMap.Length);
        int[] randomIndices = new int[numDroplets];
        int blockBuffer = 0;
        for (int i = 0; i < numDroplets; i++)
        {
            //int randomX = Random.Range(Settings.ErosionBrushRadius + blockBuffer, mapSize + Settings.ErosionBrushRadius - blockBuffer);
            //int randomY = Random.Range(Settings.ErosionBrushRadius + blockBuffer, mapSize + Settings.ErosionBrushRadius - blockBuffer);
            //randomIndices[i] = randomY * mapSize + randomX;

            int randomX = Random.Range(blockBuffer, mapSize - blockBuffer);
            int randomY = Random.Range(blockBuffer, mapSize - blockBuffer);
            randomIndices[i] = heightMapSquare.Convert2DCoordinateTo1D(randomX, randomY);
        }

        //int numThreads = numDroplets / 1024;
        int numThreads = System.Math.Min(randomIndices.Length, Settings.MaxNumThreads);
        int numElementsPerThreadToProcess = Mathf.CeilToInt(randomIndices.Length / (float)numThreads);

        //Debug.Log($"{numThreads} {randomIndices.Length} {numElementsPerThreadToProcess}");

        // Send random indices to compute shader
        ComputeBuffer randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int));
        randomIndexBuffer.SetData(randomIndices);
        Settings.erosion.SetBuffer(0, "randomIndices", randomIndexBuffer);

        // Settings
        Settings.erosion.SetInt("borderSize", Settings.ErosionBrushRadius);
        Settings.erosion.SetInt("mapSize", mapSizeWithBorder);
        Settings.erosion.SetInt("numThreads", numThreads);
        Settings.erosion.SetInt("numElementsPerThreadToProcess", numElementsPerThreadToProcess);
        Settings.erosion.SetInt("numDroplets", numDroplets);
        Settings.erosion.SetInt("brushLength", brushIndexOffsets.Count);
        Settings.erosion.SetInt("maxLifetime", Settings.maxLifetime);
        Settings.erosion.SetFloat("inertia", Settings.inertia);
        Settings.erosion.SetFloat("sedimentCapacityFactor", Settings.sedimentCapacityFactor);
        Settings.erosion.SetFloat("minSedimentCapacity", Settings.minSedimentCapacity);
        Settings.erosion.SetFloat("depositSpeed", Settings.depositSpeed);
        Settings.erosion.SetFloat("erodeSpeed", Settings.erodeSpeed);
        Settings.erosion.SetFloat("evaporateSpeed", Settings.evaporateSpeed);
        Settings.erosion.SetFloat("gravity", Settings.gravity);
        Settings.erosion.SetFloat("startSpeed", Settings.startSpeed);
        Settings.erosion.SetFloat("startWater", Settings.startWater);

        // Run compute shader
        Settings.erosion.Dispatch(0, numThreads, 1, 1);
        mapBuffer.GetData(HeightMap);
        //waterMapBuffer.GetData(WaterMap);

        // Release buffers
        mapBuffer.Release();
        //waterMapBuffer.Release();
        brushIndexBuffer.Release();
        brushWeightBuffer.Release();
        randomIndexBuffer.Release();
    }

    public override void Flush()
    {
        InputHeightMap = null;
        InputWaterMap = null;

        OutputHeightMap = null;
        OutputWaterMap = null;
        HitMap = null;
    }
}
