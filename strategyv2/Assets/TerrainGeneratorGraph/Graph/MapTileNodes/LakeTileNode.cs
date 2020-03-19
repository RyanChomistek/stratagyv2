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

            GenerateWater(terrainMapSquare, outputTileHeightMapSquare, outputVertexHeightMapSquare, WaterMapSquare, GradientMapSquare, WaterPercentThreshold, TerrainGradientThreshold);
        }
    }

    public static void GenerateWater(
        SquareArray<Terrain> terrainMapSquare,
        SquareArray<float> tileHeightMapSquare,
        SquareArray<float> vertexHeightMapSquare,
        SquareArray<float> WaterMapSquare,
        SquareArray<Vector2> GradientMapSquare,
        float WaterPercentThreshold,
        float TerrainGradientThreshold)
    {
        ArrayUtilityFunctions.SmoothMT(WaterMapSquare, 5, 4);

        ArrayUtilityFunctions.StandardDeviation(WaterMapSquare, out float mean, out float std);

        float delta = std * 2;
        float min = Mathf.Max(mean - delta, 0);
        float max = mean + delta;

        ArrayUtilityFunctions.Normalize(WaterMapSquare, min, max);

        for (int x = 0; x < WaterMapSquare.SideLength; x++)
        {
            for (int y = 0; y < WaterMapSquare.SideLength; y++)
            {
                if (WaterMapSquare[x, y] > WaterPercentThreshold &&
                   GradientMapSquare[x, y].magnitude < TerrainGradientThreshold)
                {
                    terrainMapSquare[x, y] = Terrain.Water;
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

                vertexHeightMapSquare[x,y] = tileHeightMapSquare[tileCoord];

                //Vector2Int[] indexes = new Vector2Int[]
                //{
                //    new Vector2Int(tileCoord.x - 1, tileCoord.y + 1),   // topLeft
                //    new Vector2Int(tileCoord.x,     tileCoord.y + 1),   // topmiddle
                //    new Vector2Int(tileCoord.x + 1, tileCoord.y + 1),   // topRight

                //    new Vector2Int(tileCoord.x - 1, tileCoord.y),       // middle left
                //    new Vector2Int(tileCoord.x,     tileCoord.y),       // middle
                //    new Vector2Int(tileCoord.x + 1, tileCoord.y),       // middleRight

                //    new Vector2Int(tileCoord.x - 1, tileCoord.y - 1),   // bottomleft
                //    new Vector2Int(tileCoord.x,     tileCoord.y - 1),   // bottomMiddle
                //    new Vector2Int(tileCoord.x + 1, tileCoord.y - 1),   // bottomRight
                //};

                //float[] heights = new float[4];
                //for (int i = 0; i < indexes.Length; i++)
                //{
                //    Vector2Int pos = indexes[i];
                //    try
                //    {
                //        heights[i] = tileHeightMapSquare[pos];
                //    }
                //    catch(Exception e)
                //    {
                //        Debug.Log("asd");
                //    }
                //}

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


    //// TODO need to fix this so that it can deal with the vertex map being different from the tile map dimensions
    //private static void SetHeightMapData(int tileX, int tileY, float newHeight,
    //            SquareArray<float> tileHeightMapSquare, SquareArray<float> vertexHeightMapSquare)
    //{
    //    float currentHeight = tileHeightMapSquare[tileX, tileY];
    //    float deltaHeight = newHeight - currentHeight;

    //    float scale = vertexHeightMapSquare.SideLength / (float) tileHeightMapSquare.SideLength;

    //    // find the middle point of the tile in vertex coords
    //    // find all vertices that are scale distance away

    //    Vector2Int bottomLeftMeshVert = VectorUtilityFunctions.FloorVector(new Vector2(tileX,tileY) * scale);
    //    Vector2Int delta = VectorUtilityFunctions.FloorVector(new Vector2(scale, scale));
    //    //Vector2Int delta = VectorUtilityFunctions.CeilVector(new Vector2(1, 1));

    //    //Vector2Int topRightMeshVert = bottomLeftMeshVert + delta;

    //    //int numTiles = delta.x * delta.y;
    //    //float weight = (1 / (float)numTiles) * 2;
    //    //make this drop off the farther away we are?

    //    // distribute the change in height over all
    //    for (int dx = 0; dx <= delta.x; dx++)
    //    {
    //        for (int dy = 0; dy <= delta.y; dy++)
    //        {
    //            Vector2Int meshVert = bottomLeftMeshVert + new Vector2Int(dx, dy);

    //            // This has a .25 multiplyer because if we are hitting things in a grid every vertex will have 4 neighboring tiles so if this was 1 we would over drop the height by 4
    //            try
    //            {
    //                if()
    //                vertexHeightMapSquare[meshVert.x, meshVert.y] -= deltaHeight * .25f;
    //            }
    //            catch(Exception e)
    //            {
    //                Debug.LogError("asdasdasd");
    //            }
    //        }
    //    }

    //    tileHeightMapSquare[tileX, tileY] = newHeight;
    //}

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
