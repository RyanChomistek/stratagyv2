using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneNode : TerrainNode
{
    [Input] public int Iterations = 1;
    [Input] public float[] InputArray = null;
    [Input] public Vector2Int BottomLeft;
    [Input] public Vector2Int WidthHeight;
    [Input] public float Value = .5f;
    [Input] public float FallOffRadius = .5f;

    [Output] public float[] OutputArray = null;

    public override object GetValue(XNode.NodePort port)
    {
        if (port.fieldName == "OutputArray")
            return OutputArray;

        return null;
    }

    public override void Flush()
    {
        InputArray = null;
        OutputArray = null;
    }

    public override void Recalculate()
    {
        float[] InputArray = GetInputValue("InputArray", this.InputArray);
        float Iterations = GetInputValue("Iterations", this.Iterations);

        if (IsInputArrayValid(InputArray))
        {
            OutputArray = (float[])InputArray.Clone();
            SquareArray<float> OutputArraySquare = new SquareArray<float>(OutputArray);

            for(int i = 0; i < Iterations; i++)
            {
                Vector2Int BottomLeft = GetInputValue("BottomLeft", this.BottomLeft);
                Vector2Int WidthHeight = GetInputValue("WidthHeight", this.WidthHeight);
                float Value = GetInputValue("Value", this.Value);
                float FallOffRadius = GetInputValue("FallOffRadius", this.FallOffRadius);

                SquareArray<float> subBlock = OutputArraySquare.GetSubBlock(BottomLeft, WidthHeight.x, 0);
                ArrayUtilityFunctions.StandardDeviation(subBlock, out float mean, out float std);   

                Vector2Int center = new Vector2Int((BottomLeft.x + WidthHeight.x), (BottomLeft.x + WidthHeight.x)) / 2;
                Vector2 maxDistance = WidthHeight / 2;
                maxDistance *= FallOffRadius;
                for (int x = BottomLeft.x; x < BottomLeft.x + WidthHeight.x; x++)
                {
                    for (int y = BottomLeft.y; y < BottomLeft.y + WidthHeight.y; y++)
                    {
                        Vector2Int offset = (new Vector2Int(x, y) - BottomLeft);
                        Vector2 deltaToCenter = offset - center;
                        Vector2 normalized = new Vector2(deltaToCenter.x / maxDistance.x, deltaToCenter.y / maxDistance.y);
                        float delta = Mathf.Clamp(1 - normalized.magnitude, 0, 1);

                        if (OutputArraySquare.InBounds(x, y))
                        {
                            if(delta > 0)
                            {
                                float oldValue = OutputArraySquare[x, y];
                                OutputArraySquare[x, y] = Mathf.Lerp(oldValue, mean, delta);
                            }
                        }
                    }
                }
            }
        }
    }
}
