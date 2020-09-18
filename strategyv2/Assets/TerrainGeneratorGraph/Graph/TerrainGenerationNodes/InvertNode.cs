using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("Operators/Array/Invert")]
public class InvertNode : TerrainNode
{
    [Input] public float[] InputArray = null;
    [Input] public float Min = 0;
    [Input] public float Max = 1;

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
        float Min = GetInputValue("Min", this.Min);
        float Max = GetInputValue("Max", this.Max);

        if (IsInputArrayValid(InputArray))
        {
            SquareArray<float> squareArray = new SquareArray<float>((float[])InputArray.Clone());

            ArrayUtilityFunctions.Invert(squareArray, Min, Max, Max);

            OutputArray = squareArray.Array;
        }
    }
}
