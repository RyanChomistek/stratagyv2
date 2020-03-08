using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("TerrainNodes/GradientNode")]
public class GradientNode : TerrainNode
{
    [Input] public float[] InputHeightMap = null;
    [Output] public Vector2[] GradientMap = null;

    public override void Flush()
    {
        InputHeightMap = null;
        GradientMap = null;
    }

    public override object GetValue(XNode.NodePort port)
    {
        return GradientMap;
    }

    public override void Recalculate()
    {
        float[] InputHeightMap = GetInputValue("InputHeightMap", this.InputHeightMap);
        
        if (InputHeightMap != null && InputHeightMap.Length > 0)
        {
            SquareArray<float> heightMapSquare = new SquareArray<float>(InputHeightMap);

            GradientMap = new Vector2[InputHeightMap.Length];
            SquareArray<Vector2> gradientMapSquare = new SquareArray<Vector2>(GradientMap);

            ArrayUtilityFunctions.ParallelForFast(heightMapSquare, (x, y) => {
                //loop through every tiles neighbors
                for (int i = x - 1; i <= x + 1; i++)
                {
                    for (int j = y - 1; j <= y + 1; j++)
                    {
                        if (heightMapSquare.InBounds(i, j))
                        {
                            int d_x = i - x, d_y = j - y;
                            var dir = new Vector2(d_x, d_y);
                            var delta = heightMapSquare[i, j] - heightMapSquare[x, y];

                            Vector2 localGradient = dir * delta;
                            gradientMapSquare[x, y] += localGradient;
                        }
                    }
                }
            }, 16);
        }
    }
}
