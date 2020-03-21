using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("Visualizers/VisualizeImprovementArray")]
public class VisualizeImprovementArrayNode : VisualizeArrayNode
{
    [Input] public Improvement[] Array;

    private static Dictionary<Improvement, Color> improvementColors = new Dictionary<Improvement, Color>(){
        { Improvement.Empty, Color.black },
        { Improvement.Farm, Color.yellow },
        { Improvement.Forest, Color.green },
        { Improvement.Road, Color.black },
        { Improvement.Town, Color.cyan },
    };

    public override void Recalculate()
    {
        Improvement[] Array = GetInputValue("Array", this.Array);

        if (IsInputArrayValid(Array))
        {
            GenerateVisualization(Array, (val) => {
                return improvementColors[val];
            });

            base.Recalculate();
        }
    }
}