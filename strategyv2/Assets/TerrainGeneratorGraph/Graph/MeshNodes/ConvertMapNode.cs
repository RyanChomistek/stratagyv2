using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConvertMapNode : TerrainNode
{
    [Input] public float[] InputMap = null;
    [Input] public int Padding;

    //this is the scale going from tile to vertex so if this is 2, the vertex coordinates will act like each tile is actually 4
    [Input] public int TileToVertexScale = 1;

    [Output] public float[] OutputVertexMap = null;
    [Output] public float[] OutputTileMap = null;
    [Output] public int OutputVertexMapSize;
    [Output] public int OutputTileMapSize;

    public override void Flush()
    {
        InputMap = null;
        OutputVertexMap = null;
        OutputTileMap = null;
    }

    public override object GetValue(XNode.NodePort port)
    {
        if (port.fieldName == "OutputVertexMap")
            return OutputVertexMap;

        if (port.fieldName == "OutputTileMap")
            return OutputTileMap;

        if (port.fieldName == "OutputVertexMapSize")
            return OutputVertexMapSize;

        if (port.fieldName == "OutputTileMapSize")
            return OutputTileMapSize;

        return null;
    }

    public override void Recalculate()
    {
        float[] InputMap = GetInputValue("InputMap", this.InputMap);
        int padding = GetInputValue("Padding", this.Padding);
        int TileToVertexScale = GetInputValue("TileToVertexScale", this.TileToVertexScale);
        if (IsInputArrayValid(InputMap))
        {
            SquareArray<float> InputMapSquare = new SquareArray<float>(InputMap);

            
            int vertexMapSideLength = InputMapSquare.SideLength - (2*padding);

            // we need this to line up with the scale
            // eventually we need tileLength * 2 == vertexMapSideLength - 1 for mesh gen
            vertexMapSideLength -= ((vertexMapSideLength - 1) % TileToVertexScale);

            OutputVertexMap = new float[vertexMapSideLength * vertexMapSideLength];
            SquareArray<float> OutputVertexMapSquare = new SquareArray<float>(OutputVertexMap);

            // we want this map to be as if we took the vertex map and shrunk it by one at the top and right
            int tileMapSideLength = (OutputVertexMapSquare.SideLength - 1) / TileToVertexScale;
            OutputTileMap = new float[tileMapSideLength * tileMapSideLength];
            SquareArray<float> OutputTileMapSquare = new SquareArray<float>(OutputTileMap);

            for (int x = 0; x < OutputVertexMapSquare.SideLength; x++)
            {
                for (int y = 0; y < OutputVertexMapSquare.SideLength; y++)
                {
                    OutputVertexMapSquare[x, y] = InputMapSquare[x + padding, y + padding];
                }
            }

            for (int x = 0; x < OutputTileMapSquare.SideLength; x++)
            {
                for (int y = 0; y < OutputTileMapSquare.SideLength; y++)
                {
                    Vector2Int rawIndex = new Vector2Int(x, y) * TileToVertexScale;
                    Vector2Int[] indexes = new Vector2Int[]
                    {
                        new Vector2Int(rawIndex.x,     rawIndex.y + TileToVertexScale), // topLeft
                        new Vector2Int(rawIndex.x + TileToVertexScale, rawIndex.y + TileToVertexScale), // topRight
                        new Vector2Int(rawIndex.x + TileToVertexScale, rawIndex.y),     // bottomRight
                        new Vector2Int(rawIndex.x,     rawIndex.y),     // bottomLeft
                    };

                    float[] heights = new float[4];
                    for (int i = 0; i < indexes.Length; i++)
                    {
                        Vector2Int pos = indexes[i];
                        heights[i] = OutputVertexMapSquare[indexes[i].x, indexes[i].y];
                    }

                    Vector2 uv = new Vector2(.5f, .5f);
                    float height = ArrayUtilityFunctions.QuadLerp(heights[0], heights[1], heights[2], heights[3], uv.x, uv.y);
                    OutputTileMapSquare[x, y] = height;
                }
            }


            // Create the tile height map
            // take every 4 height map points and find the middle value and use that
            //for (int x = 0; x < OutputVertexMapSquare.SideLength - 1; x++)
            //{
            //    for (int y = 0; y < OutputVertexMapSquare.SideLength - 1; y++)
            //    {
            //        Vector2Int rawIndex = new Vector2Int(x, y);
            //        Vector2Int[] indexes = new Vector2Int[]
            //        {
            //            new Vector2Int(rawIndex.x,     rawIndex.y + 1), // topLeft
            //            new Vector2Int(rawIndex.x + 1, rawIndex.y + 1), // topRight
            //            new Vector2Int(rawIndex.x + 1, rawIndex.y),     // bottomRight
            //            new Vector2Int(rawIndex.x,     rawIndex.y),     // bottomLeft
            //        };

            //        float[] heights = new float[4];
            //        for (int i = 0; i < indexes.Length; i++)
            //        {
            //            heights[i] = OutputVertexMapSquare[indexes[i].x, indexes[i].y];
            //        }

            //        Vector2 uv = new Vector2(.5f, .5f);

            //        float height = ArrayUtilityFunctions.QuadLerp(heights[0], heights[1], heights[2], heights[3], uv.x, uv.y);
            //        OutputTileMapSquare[x, y] = height;
            //    }
            //}

            OutputVertexMapSize = OutputVertexMapSquare.SideLength;
            OutputTileMapSize = OutputTileMapSquare.SideLength;
        }
    }
}
