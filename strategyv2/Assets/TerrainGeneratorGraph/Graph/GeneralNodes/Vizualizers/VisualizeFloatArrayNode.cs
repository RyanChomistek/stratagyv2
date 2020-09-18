using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("Visualizers/VisualizeFloatArrayNode")]
public class VisualizeFloatArrayNode : VisualizeArrayNode
{
    [Input] public float[] Array;

    public override void Recalculate()
    {
        float[] Array = GetInputValue("Array", this.Array);

        if(IsInputArrayValid(Array))
        {
            GenerateVisualization(Array, (val) => {
                return val;
            });

            Length = Array.Length;
            SideLength = new SquareArray<float>(Array).SideLength;
            base.Recalculate();
        }
    }
}
