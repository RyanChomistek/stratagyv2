using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateNodeMenu("TileNodes/LakeTileNode")]
public class LakeTileNode : TerrainNode
{
    [Input] public Terrain[] InputTerrain = null;
    [Input] public float[] InputTileHeightMap = null;
    [Input] public float[] InputVertexHeightMap = null;

    [Input] public Vector2[] InputGradientMap = null;
    [Input] public float[] InputWaterMap = null;

    [Input] public float WaterPercentThreshold = .1f;
    [Input] public float TerrainGradientThreshold = .1f;
    [Input] public float MaxHeightForWater = .5f;

    [Output] public Terrain[] OutputTerrain = null;
    [Output] public float[] OutputTileHeightMap = null;
    [Output] public float[] OutputVertexHeightMap = null;

    public override object GetValue(XNode.NodePort port)
    {
        if (port.fieldName == "OutputTerrain")
            return OutputTerrain;

        if (port.fieldName == "OutputTileHeightMap")
            return OutputTileHeightMap;
        
        if (port.fieldName == "OutputVertexHeightMap")
            return OutputVertexHeightMap;

        return null;
    }

    public override void Recalculate()
    {
        float[] InputWaterMap = GetInputValue("InputWaterMap", this.InputWaterMap);
        Vector2[] InputGradientMap = GetInputValue("InputGradientMap", this.InputGradientMap);

        float[] InputVertexHeightMap = GetInputValue("InputVertexHeightMap", this.InputVertexHeightMap);
        float[] InputTileHeightMap = GetInputValue("InputTileHeightMap", this.InputTileHeightMap);
        Terrain[] InputTerrain = GetInputValue("InputTerrain", this.InputTerrain);

        float WaterPercentThreshold = GetInputValue("WaterPercentThreshold", this.WaterPercentThreshold);
        float TerrainGradientThreshold = GetInputValue("TerrainGradientThreshold", this.TerrainGradientThreshold);
        float MaxHeightForWater = GetInputValue("MaxHeightForWater", this.MaxHeightForWater);

        if (IsInputArrayValid(InputVertexHeightMap) &&
            IsInputArrayValid(InputTileHeightMap) &&
            IsInputArrayValid(InputWaterMap) &&
            IsInputArrayValid(InputGradientMap) &&
            IsInputArrayValid(InputTerrain))
        {
            OutputTerrain = (Terrain[])InputTerrain.Clone();
            SquareArray<Terrain> terrainMapSquare = new SquareArray<Terrain>(OutputTerrain);

            OutputTileHeightMap = (float[])InputTileHeightMap.Clone();
            SquareArray<float> outputTileHeightMapSquare = new SquareArray<float>(OutputTileHeightMap);

            OutputVertexHeightMap = (float[])InputVertexHeightMap.Clone();
            SquareArray<float> outputVertexHeightMapSquare = new SquareArray<float>(OutputVertexHeightMap);

            SquareArray<float> WaterMapSquare = new SquareArray<float>(InputWaterMap);
            SquareArray<Vector2> GradientMapSquare = new SquareArray<Vector2>(InputGradientMap);

            GenerateWater(terrainMapSquare, outputTileHeightMapSquare, outputVertexHeightMapSquare, WaterMapSquare, GradientMapSquare, WaterPercentThreshold, TerrainGradientThreshold, MaxHeightForWater);
        }
    }

    public static void GenerateWater(
        SquareArray<Terrain> terrainMapSquare,
        SquareArray<float> tileHeightMapSquare,
        SquareArray<float> vertexHeightMapSquare,
        SquareArray<float> WaterMapSquare,
        SquareArray<Vector2> GradientMapSquare,
        float WaterPercentThreshold,
        float TerrainGradientThreshold,
        float MaxHeightForWater)
    {
        ArrayUtilityFunctions.SmoothMT(WaterMapSquare, 5, 4);

        ArrayUtilityFunctions.StandardDeviation(WaterMapSquare, out float mean, out float std, true);

        float delta = std;
        float min = Mathf.Max(mean - delta, 0);
        float max = mean + delta;

        ArrayUtilityFunctions.Normalize(WaterMapSquare, min, max);

        ArrayUtilityFunctions.StandardDeviation(WaterMapSquare, out mean, out std, true);

        float threshhold = (mean + (std * WaterPercentThreshold));

        int cnt = 0;

        for (int x = 0; x < WaterMapSquare.SideLength; x++)
        {
            for (int y = 0; y < WaterMapSquare.SideLength; y++)
            {
                //&& GradientMapSquare[x, y].magnitude < TerrainGradientThreshold
                if (WaterMapSquare[x, y] > threshhold && tileHeightMapSquare[x,y] < MaxHeightForWater)
                {
                    terrainMapSquare[x, y] = Terrain.Water;
                    cnt++;
                }
            }
        }

        Vector2Int[] floodFillDirections = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
        };

        int numWaterFlooded = 10000;
        int numIterations = 0;

        // even out the water level
        while (numWaterFlooded > (WaterMapSquare.SideLength * .01f) && numIterations < 4)
        {
            numIterations++;
            numWaterFlooded = 0;

            // Make water flood lower areas, go forwards and backwards to make sure we spread evenly
            for (int x = 0; x < WaterMapSquare.SideLength; x++)
            {
                for (int y = 0; y < WaterMapSquare.SideLength; y++)
                {
                    FloodTile(x, y, terrainMapSquare, tileHeightMapSquare, floodFillDirections, ref numWaterFlooded);
                }
            }

            for (int x = WaterMapSquare.SideLength - 1; x >= 0; x--)
            {
                for (int y = WaterMapSquare.SideLength - 1; y >= 0; y--)
                {
                    FloodTile(x, y, terrainMapSquare, tileHeightMapSquare, floodFillDirections, ref numWaterFlooded);
                }
            }

            //Debug.Log($"num water flooded {numWaterFlooded}");
        }

        //Debug.Log($"num iterations {numIterations}");

        for (int x = 0; x < tileHeightMapSquare.SideLength; x++)
        {
            for (int y = 0; y < tileHeightMapSquare.SideLength; y++)
            {
                if (terrainMapSquare[x, y] == Terrain.Water)
                {
                    tileHeightMapSquare[x, y] -= .05f;
                }
            }
        }

        // Vertex height map is not out of sync since we messed with the tile map, sync it back up
        int scale = (vertexHeightMapSquare.SideLength - 1) / tileHeightMapSquare.SideLength;

        // minus 1 on the sidelength since we cant interpolate off the array
        for (int x = 0; x < vertexHeightMapSquare.SideLength - 1; x++)
        {
            for (int y = 0; y < vertexHeightMapSquare.SideLength - 1; y++)
            {
                Vector2Int tileCoord = new Vector2Int(x / scale, y / scale);

                //if(x == vertexHeightMapSquare.SideLength)
                //{
                //    tileCoord.x = tileCoord.x - 1;
                //}

                if (terrainMapSquare[tileCoord] != Terrain.Water)
                {
                    continue;
                }

                vertexHeightMapSquare[x, y] = tileHeightMapSquare[tileCoord];

            }
        }
    }

    private static List<Vector2Int> m_TileVertsOffsets = new List<Vector2Int>()
    {
        new Vector2Int(0,1), // Top left
        new Vector2Int(1,1), // Top right
        new Vector2Int(0,0), // Bottom left
        new Vector2Int(1,0), // Bottom Right
    };


    /// <summary>
    /// evens out the height map between this tile and its adjacents
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="currentTerrain"></param>
    /// <param name="mapData"></param>
    /// <param name="map"></param>
    /// <param name="floodFillDirections"></param>
    /// <param name="numWaterFlooded"></param>
    public static void FloodTile(int x, int y, SquareArray<Terrain> terrainMapSquare, SquareArray<float> tileHeightMapSquare, Vector2Int[] floodFillDirections, ref int numWaterFlooded)
    {
        bool hasAdjacentWater = false;
        float heighestAdjacentWater = -1;
        float lowestAdjacentWater = 1000;
        Vector2Int dirToWater;

        foreach (Vector2Int dir in floodFillDirections)
        {
            Vector2Int pos = new Vector2Int(x, y) + dir;
            if (terrainMapSquare.InBounds(pos) && terrainMapSquare[pos.x, pos.y].Equals(Terrain.Water))
            {
                hasAdjacentWater = true;
                heighestAdjacentWater = Mathf.Max(heighestAdjacentWater, tileHeightMapSquare[pos.x, pos.y]);
                lowestAdjacentWater = Mathf.Min(lowestAdjacentWater, tileHeightMapSquare[pos.x, pos.y]);
                dirToWater = dir;
            }
        }

        if (hasAdjacentWater && heighestAdjacentWater >= tileHeightMapSquare[x, y])
        {
            numWaterFlooded++;
            terrainMapSquare[x, y] = Terrain.Water;
            tileHeightMapSquare[x,y] = heighestAdjacentWater;
        }
    }

    public override void Flush()
    {
        InputTerrain = null;
        InputTileHeightMap = null;
        InputVertexHeightMap = null;

        InputGradientMap = null;
        InputWaterMap = null;
        OutputTerrain = null;
        OutputTileHeightMap = null;
        OutputVertexHeightMap = null;
    }
}
