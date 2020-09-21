using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("Visualizers/VisualizeVector2Array")]
public class VisualizeVector2ArrayNode : VisualizeArrayNode
{
    [Input] public Vector2[] Array;

    public override void Recalculate()
    {
        Vector2[] Array = GetInputValue("Array", this.Array);

        if (IsInputArrayValid(Array))
        {
            GenerateVisualization(Array, (val) => {
                return val.magnitude;
            });

            Length = Array.Length;
            SideLength = new SquareArray<Vector2>(Array).SideLength;
            base.Recalculate();
        }
    }
}
