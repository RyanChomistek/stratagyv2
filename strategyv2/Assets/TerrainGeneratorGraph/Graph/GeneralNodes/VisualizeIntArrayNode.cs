using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ArrayVisualizationType
{
    RedGreenGradient,
    RandomColors
}

[CreateNodeMenu("Visualizers/VisualizeIntArrayNode")]
public class VisualizeIntArrayNode : VisualizeArrayNode
{
    [Input] public int[] Array;
    [Input] public ArrayVisualizationType Type = ArrayVisualizationType.RedGreenGradient;

    public override void Recalculate()
    {
        int[] Array = GetInputValue("Array", this.Array);
        ArrayVisualizationType Type = GetInputValue("Type", this.Type);

        if (IsInputArrayValid(Array))
        {
            Length = Array.Length;

            if(Type == ArrayVisualizationType.RedGreenGradient)
            {
                GenerateVisualization(Array, (val) => {
                    return val;
                });
            }
            else
            {
                Dictionary<int, Color> colorMap = new Dictionary<int, Color>();
                GenerateVisualization(Array, (val) => {
                    if(colorMap.TryGetValue(val, out Color color))
                    {
                        return color;
                    }

                    Color randomColor = Random.ColorHSV();
                    colorMap[val] = randomColor;
                    return randomColor;
                });
            }
           
            base.Recalculate();
        }
    }
}

