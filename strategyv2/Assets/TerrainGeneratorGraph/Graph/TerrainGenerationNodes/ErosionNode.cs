using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class ErosionSettings
{
    [Header("Erosion Settings")]
    public bool enabled = true;
    public ComputeShader erosion;
    public int MaxNumThreads = 65535;
    public int numDropletsPerCell = 8;
    public int maxLifetime = 30;
    public float sedimentCapacityFactor = 3;
    public float minSedimentCapacity = .01f;
    public float depositSpeed = 0.3f;
    public float erodeSpeed = 0.3f;
    public float evaporateSpeed = .01f;
    public float gravity = 4;
    public float startSpeed = 1;
    public float startWater = 1;
    [Range(0, 1)]
    public float inertia = 0.3f;
    public int ErosionBrushRadius = 3;
}

public class ErosionOutput
{
    public float[] HeightMap;
    public float[] WaterMap;
}

[CreateNodeMenu("TerrainNodes/ErosionNode")]
public class ErosionNode : TerrainNode
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

    [Input] public float[] InputHeightMap = null;
    [Input] public float[] InputWaterMap = null;

    [Output] public float[] HeightMap = null;
    [Output] public float[] WaterMap = null;
    public int HeightMapSize = 128 * 128;

    public override object GetValue(XNode.NodePort port)
    {
        if(port.fieldName == "HeightMap")
            return HeightMap;

        if (port.fieldName == "WaterMap")
            return WaterMap;

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

            ErosionOutput EO = Run(ES, InputHeightMap, InputWaterMap);

            HeightMap = EO.HeightMap;
            WaterMap = EO.WaterMap;

            HeightMapSize = InputHeightMap.Length;
        }
    }

    public static ErosionOutput Run(ErosionSettings settings, float[] InputHeightMap, float[] InputWaterMap)
    {
        float[] HeightMap = (float[])InputHeightMap.Clone();
        float[] WaterMap = (float[])InputWaterMap.Clone();


        //ErodeCPU(settings, (int)Mathf.Sqrt(HeightMap.Length), HeightMap, WaterMap);
        ErodeGPU(settings, (int)Mathf.Sqrt(HeightMap.Length), HeightMap, WaterMap);

        return new ErosionOutput()
        {
            HeightMap = HeightMap,
            WaterMap = WaterMap
        };
    }

    private static void ErodeCPU(ErosionSettings Settings, int mapSize, float[] HeightMap, float[] WaterMap)
    {
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

        int numThreads = 1;
        int numElementsToProcess = Mathf.CeilToInt(HeightMap.Length / (float)numThreads);

        int mapBufferLength = mapSize * mapSize;
        int startIndex = 0 * numElementsToProcess;

        //for (int offset = 0; offset < numElementsToProcess && offset + startIndex < mapBufferLength; offset++)
        for (int cnt = 0; cnt < numElementsToProcess; cnt++)
        {
            int offset = Random.Range(0, mapBufferLength);
            int index = startIndex + offset;
            for (int dropletCnt = 0; dropletCnt < Settings.numDropletsPerCell; dropletCnt++)
            {
                float posX = (float)(index % mapSize);
                float posY = (float)(index / mapSize);
                float dirX = 0;
                float dirY = 0;
                float speed = Settings.startWater;
                float water = Settings.startSpeed;
                float sediment = 0;

                for (int lifetime = 0; lifetime < Settings.maxLifetime; lifetime++)
                {
                    int nodeX = (int)posX;
                    int nodeY = (int)posY;
                    int dropletIndex = nodeY * mapSize + nodeX;

                    // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
                    float cellOffsetX = posX - nodeX;
                    float cellOffsetY = posY - nodeY;

                    // Calculate droplet's height and direction of flow with bilinear interpolation of surrounding heights
                    Vector3 heightAndGradient = CalculateHeightAndGradient(posX, posY, HeightMap, mapSize);

                    // Update the droplet's direction and position (move position 1 unit regardless of speed)
                    dirX = (dirX * Settings.inertia - heightAndGradient.x * (1 - Settings.inertia));
                    dirY = (dirY * Settings.inertia - heightAndGradient.y * (1 - Settings.inertia));

                    // Normalize direction
                    float len = Mathf.Max(0.01f, Mathf.Sqrt(dirX * dirX + dirY * dirY));
                    dirX /= len;
                    dirY /= len;
                    posX += dirX;
                    posY += dirY;

                    // Stop simulating droplet if it's not moving or has flowed over edge of map
                    if ((dirX == 0 && dirY == 0) || posX < Settings.ErosionBrushRadius || posX > mapSize - Settings.ErosionBrushRadius || posY < Settings.ErosionBrushRadius || posY > mapSize - Settings.ErosionBrushRadius)
                    {
                        break;
                    }

                    // Find the droplet's new height and calculate the deltaHeight
                    float newHeight = CalculateHeightAndGradient(posX, posY, HeightMap, mapSize).z;

                   

                    float deltaHeight = newHeight - heightAndGradient.z;

                    // Calculate the droplet's sediment capacity (higher when moving fast down a slope and contains lots of water)
                    float sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * Settings.sedimentCapacityFactor, Settings.minSedimentCapacity);

                    // If carrying more sediment than capacity, or if flowing uphill:
                    if (sediment > sedimentCapacity || deltaHeight > 0)
                    {
                        // If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                        float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, sediment) : (sediment - sedimentCapacity) * Settings.depositSpeed;
                        sediment -= amountToDeposit;

                        // Add the sediment to the four nodes of the current cell using bilinear interpolation
                        // Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                        HeightMap[dropletIndex] += amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                        HeightMap[dropletIndex + 1] += amountToDeposit * cellOffsetX * (1 - cellOffsetY);
                        HeightMap[dropletIndex + mapSize] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                        HeightMap[dropletIndex + mapSize + 1] += amountToDeposit * cellOffsetX * cellOffsetY;
                    }
                    else
                    {
                        // Erode a fraction of the droplet's current carry capacity.
                        // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                        float amountToErode = Mathf.Min((sedimentCapacity - sediment) * Settings.erodeSpeed, -deltaHeight);

                        for (int i = 0; i < brushIndexOffsets.Count; i++)
                        {
                            int erodeIndex = dropletIndex + brushIndexOffsets[i];

                            float weightedErodeAmount = amountToErode * brushWeights[i];
                            if(erodeIndex < (mapSize * mapSize))
                            {
                                float deltaSediment = (HeightMap[erodeIndex] < weightedErodeAmount) ? HeightMap[erodeIndex] : weightedErodeAmount;
                                HeightMap[erodeIndex] -= deltaSediment;
                                sediment += deltaSediment;
                            }
                        }
                    }

                    // Update droplet's speed and water content
                    speed = Mathf.Sqrt(Mathf.Max(0, speed * speed + deltaHeight * Settings.gravity));
                    water *= (1 - Settings.evaporateSpeed);
                    WaterMap[dropletIndex] += water;
                }
            }
        }
    }

    static bool InBounds(int i, int mapSize)
    {
        return i > 0 && i < mapSize;
    }

    static bool CheckIfInboundsForGradient(float posX, float posY, int mapSize)
    {
        int coordX = (int)posX;
        int coordY = (int)posY;

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = posX - coordX;
        float y = posY - coordY;

        int nodeIndexNW = coordY * mapSize + coordX;
        int nodeIndexNE = nodeIndexNW + 1;
        int nodeIndexSW = nodeIndexNW + mapSize;
        int nodeIndexSE = nodeIndexNW + mapSize + 1;

        int arrayLen = mapSize * mapSize;

        return InBounds(nodeIndexNW, arrayLen) && InBounds(nodeIndexNE, arrayLen) && InBounds(nodeIndexSW, arrayLen) && InBounds(nodeIndexSE, arrayLen);
    }

    // Returns float3(gradientX, gradientY, height)
    private static Vector3 CalculateHeightAndGradient(float posX, float posY, float[] map, int mapSize)
    {
        if (!CheckIfInboundsForGradient(posX, posY, mapSize))
        {
            return Vector3.zero;
        }

        int coordX = (int)posX;
        int coordY = (int)posY;

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = posX - coordX;
        float y = posY - coordY;

        // Calculate heights of the four nodes of the droplet's cell
        int nodeIndexNW = coordY * mapSize + coordX;
        float heightNW = map[nodeIndexNW];
        float heightNE = map[nodeIndexNW + 1];
        float heightSW = map[nodeIndexNW + mapSize];
        float heightSE = map[nodeIndexNW + mapSize + 1];

        // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        float height = heightNW * (1 - x) * (1 - y) + heightNE * x * (1 - y) + heightSW * (1 - x) * y + heightSE * x * y;

        return new Vector3(gradientX, gradientY, height);
    }

    private static void ErodeGPU(ErosionSettings Settings, int mapSize, float[] HeightMap, float[] WaterMap)
    {
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

        int numThreads = System.Math.Min(HeightMap.Length, Settings.MaxNumThreads);
        int numElementsToProcess = Mathf.CeilToInt(HeightMap.Length / (float)numThreads);
        //Debug.Log($"Erosion: num elements = {HeightMap.Length}, num GPU Threads = {numThreads}, each doing {numElementsToProcess} elements");

        // HeightMap buffer
        ComputeBuffer mapBuffer = new ComputeBuffer(HeightMap.Length, sizeof(float));
        mapBuffer.SetData(HeightMap);
        Settings.erosion.SetBuffer(0, "map", mapBuffer);

        // WaterMap buffer
        ComputeBuffer waterMapBuffer = new ComputeBuffer(WaterMap.Length, sizeof(float));
        waterMapBuffer.SetData(WaterMap);
        Settings.erosion.SetBuffer(0, "waterMap", waterMapBuffer);

        // Settings
        Settings.erosion.SetInt("borderSize", Settings.ErosionBrushRadius);
        Settings.erosion.SetInt("mapSize", mapSize);

        Settings.erosion.SetInt("numThreads", numThreads);
        Settings.erosion.SetInt("numElementsToProcess", numElementsToProcess);

        Settings.erosion.SetInt("numDropletsPerCell", Settings.numDropletsPerCell);

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
        waterMapBuffer.GetData(WaterMap);

        // Release buffers
        mapBuffer.Release();
        waterMapBuffer.Release();
        brushIndexBuffer.Release();
        brushWeightBuffer.Release();
    }

    public override void Flush()
    {
        InputHeightMap = null;
        InputWaterMap = null;

        HeightMap = null;
        WaterMap = null;
    }
}