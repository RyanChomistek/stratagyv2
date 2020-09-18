using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("Visualizers/VisualizeImprovementArray")]
public class VisualizeImprovementArrayNode : VisualizeArrayNode
{
    [Input] public Improvement[] Array;

    static Color Orange = new Color(1, .64f, 0);

    private static Dictionary<Improvement, Color> improvementColors = new Dictionary<Improvement, Color>(){
        { Improvement.Empty, Color.black },
        { Improvement.Farm, Orange },
        { Improvement.Forest, Color.green },
        { Improvement.Road, Color.cyan },
        { Improvement.Town, Color.magenta },
        { Improvement.Desert, Color.yellow },
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