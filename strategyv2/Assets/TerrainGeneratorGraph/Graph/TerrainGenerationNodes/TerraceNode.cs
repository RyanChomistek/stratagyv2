using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerraceNode : TerrainNode
{
    [Input] public int NumSteps = 5;
    [Input] public float[] InputHeightMap = new float[128 * 128];
    [Output] public float[] OutputHeightMap;

    public override object GetValue(XNode.NodePort port)
    {
        return OutputHeightMap;
    }

    private int GetLayerIndexByHeight(float height, int numLayers)
    {
        int z = (int)(height * numLayers);
        //if the z is exactly numzlayers it will cause at out of bound on out bounds on the layers list
        if (z == numLayers)
        {
            z--;
        }
        return z;
    }

    public override void Recalculate()
    {
        float[] InputHeightMap = GetInputValue("InputHeightMap", this.InputHeightMap);
        if(base.IsInputArrayValid(InputHeightMap))
        {
            OutputHeightMap = (float[])InputHeightMap.Clone();
            SquareArray<float> heightMapSquare = new SquareArray<float>(OutputHeightMap);

            int numSteps = GetInputValue("NumSteps", this.NumSteps);

            for (int x = 0; x < heightMapSquare.SideLength; x++)
            {
                for (int y = 0; y < heightMapSquare.SideLength; y++)
                {
                    heightMapSquare[x, y] = GetLayerIndexByHeight(heightMapSquare[x, y], numSteps) / (float) numSteps;
                }
            }

            GenerateVisualization(OutputHeightMap, (val) => {
                return Color.Lerp(Color.green, Color.red, val);
            });
        }
        
    }
}
