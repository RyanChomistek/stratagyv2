using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("Visualizers/VisualizeVector3Array")]
public class VizualizeVector3ArrayNode : VisualizeArrayNode
{
    [Input] public Vector3[] Array;

    public override void Recalculate()
    {
        Vector3[] Array = GetInputValue("Array", this.Array);

        if (IsInputArrayValid(Array))
        {
            GenerateVisualization(Array, (val) => {
                return val.magnitude;
            });

            Length = Array.Length;
            SideLength = new SquareArray<Vector3>(Array).SideLength;
            base.Recalculate();
        }
    }
}

