using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("")]
public class VisualizeArrayNode : SelfPropagatingNode
{
    [HideInInspector]
    public Texture2D visualization;
    public int Length = -1;
    public int SideLength = -1;

    public float Min;
    public float Max;

    public override void Flush()
    {
        
    }

    public override void Recalculate()
    {
        
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
                Color val = getColor(TSquare[(int)(x / scale), (int)(y / scale)]);
                visualization.SetPixel(x, y, val);
            }
        }

        visualization.Apply();
    }

    protected void GenerateVisualization<T>(T[] arr, Func<T, float> valueGetter)
    {
        SquareArray<T> TSquare = new SquareArray<T>(arr);
        Min = float.PositiveInfinity;
        Max = float.NegativeInfinity;
        for (int x = 0; x < TSquare.SideLength; x++)
        {
            for (int y = 0; y < TSquare.SideLength; y++)
            {
                float val = valueGetter(TSquare[x, y]);
                Min = Mathf.Min(Min, val);
                Max = Mathf.Max(Max, val);
            }
        }

        float delta = Max - Min;
        if(delta == 0)
        {
            return;
        }

        int texSize = 1024;
        float scale = texSize / (float)TSquare.SideLength;
        visualization = new Texture2D(texSize, texSize);
        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                float val = valueGetter(TSquare[(int)(x / scale), (int)(y / scale)]);
                Color color = Color.Lerp(Color.red, Color.green, (val - Min) / delta);
                visualization.SetPixel(x, y, color);
            }
        }

        visualization.Apply();
    }
}
