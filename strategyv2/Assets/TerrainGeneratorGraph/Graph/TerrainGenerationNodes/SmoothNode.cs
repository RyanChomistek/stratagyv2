using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothNode : TerrainNode
{
    [Input] public float[] InputArray = null;
    [Input] public int iterations = 1;
    [Output] public float[] OutputArray = null;

    public override object GetValue(XNode.NodePort port)
    {
        if (port.fieldName == "OutputArray")
            return OutputArray;

        return null;
    }

    public override void Recalculate()
    {
        float[] InputArray = GetInputValue("InputArray", this.InputArray);
        int iterations = GetInputValue("iterations", this.iterations);
        if (IsInputArrayValid(InputArray))
        {
            SquareArray<float> squareArray = new SquareArray<float>((float[])InputArray.Clone());

            for (int i = 0; i < iterations; i++)
            {
                squareArray = ArrayUtilityFunctions.SmoothMT(squareArray, 25, 16);
            }
            
            OutputArray = squareArray.Array;
        }
    }

    public override void Flush()
    {
        InputArray = null;
        OutputArray = null;
    }
}
