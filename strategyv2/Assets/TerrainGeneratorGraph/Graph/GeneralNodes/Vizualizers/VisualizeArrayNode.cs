using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("")]
public class VisualizeArrayNode : SelfPropagatingNode
{
    public int textureSize = 256;

    [HideInInspector]
    public Texture2D visualization;
    public int Length = -1;
    public int SideLength = -1;

    public bool SetMinMax = false;
    public float Min;
    public float Max;
    public float Average;
    public override void Flush()
    {
        
    }

    public override void Recalculate()
    {
        
    }

    protected void GenerateVisualization<T>(T[] arr, Func<T, Color> getColor)
    {
        if(!Graph.IsVisualizationsEnabled)
        {
            return;
        }

        SquareArray<T> TSquare = new SquareArray<T>(arr);
        float scale = textureSize / (float)TSquare.SideLength;
        visualization = new Texture2D(textureSize, textureSize);

        SquareArray<Color> colors = new SquareArray<Color>(textureSize);
        ArrayUtilityFunctions.ForMTTwoDimension(textureSize, (x, y) =>
        {
            colors[x, y] = getColor(TSquare[(int)(x / scale), (int)(y / scale)]);
        });

        visualization.SetPixels(colors.Array);
        visualization.Apply();
    }

    protected void GenerateVisualization<T>(T[] arr, Func<T, float> valueGetter)
    {
        if (!Graph.IsVisualizationsEnabled)
        {
            return;
        }

        SquareArray<T> TSquare = new SquareArray<T>(arr);

        if(!SetMinMax)
        {
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
        }

        Average = 0;
        for (int x = 0; x < TSquare.SideLength; x++)
        {
            for (int y = 0; y < TSquare.SideLength; y++)
            {
                float val = valueGetter(TSquare[x, y]);
                Average += val;
            }
        }

        Average /= TSquare.Length;

        float delta = Max - Min;
        if(delta == 0)
        {
            return;
        }

        
        float scale = textureSize / (float)TSquare.SideLength;
        visualization = new Texture2D(textureSize, textureSize);

        SquareArray<Color> colors = new SquareArray<Color>(textureSize);

        ArrayUtilityFunctions.ForMTTwoDimension(textureSize, (x, y) =>
        {
            float val = valueGetter(TSquare[(int)(x / scale), (int)(y / scale)]);
            colors[x, y] = Color.Lerp(Color.red, Color.green, (val - Min) / delta);
        });

        visualization.SetPixels(colors.Array);
        visualization.Apply();
    }
}
