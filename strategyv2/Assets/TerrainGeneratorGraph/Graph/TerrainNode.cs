using HeightMapGeneration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;

[CreateNodeMenu("")]
public class TerrainNode : SelfPropagatingNode
{
    [Input] public int seed;
    [Input] public bool randomizeSeed;

    [HideInInspector]
    public Texture2D visualization;

    public override void RecalculateNextNode(SelfPropagatingNode propogatingNode)
    {
        TerrainNode TNodeOther;
        if (TNodeOther = propogatingNode as TerrainNode)
        {
            TNodeOther.seed = this.seed;
            TNodeOther.randomizeSeed = this.randomizeSeed;
        }
        
        base.RecalculateNextNode(propogatingNode);
    }

    public bool IsInputArrayValid<T>(T[] arr)
    {
        return arr != null && arr.Length > 0;
    }

    protected void GenerateVisualization<T>(T[] arr, Func<T, Color> getColor)
    {
        SquareArray<T> TSquare = new SquareArray<T>(arr);
        int texSize = 1024;
        float scale = texSize / (float)TSquare.SideLength;
        visualization = new Texture2D(texSize, texSize);
        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                Color val = getColor(TSquare[(int) (x / scale), (int) (y / scale)]);
                visualization.SetPixel(x, y, val);
                //visualization.SetPixel(x, y, Color.red);
            }
        }

        visualization.Apply();
    }

    protected void GenerateVisualization<T>(T[] arr, Func<T, float> valueGetter)
    {
        SquareArray<T> TSquare = new SquareArray<T>(arr);
        float min = float.PositiveInfinity;
        float max = float.NegativeInfinity;
        for (int x = 0; x < TSquare.SideLength; x++)
        {
            for (int y = 0; y < TSquare.SideLength; y++)
            {
                float val = valueGetter(TSquare[x, y]);
                min = Mathf.Min(min, val);
                max = Mathf.Max(max, val);
            }
        }

        float delta = max - min;

        int texSize = 1024;
        float scale = texSize / (float)TSquare.SideLength;
        visualization = new Texture2D(texSize, texSize);
        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                float val = valueGetter(TSquare[(int)(x / scale), (int)(y / scale)]);
                Color color = Color.Lerp(Color.red, Color.green, (val - min) / delta);
                visualization.SetPixel(x, y, color);
            }
        }

        visualization.Apply();
    }
}
