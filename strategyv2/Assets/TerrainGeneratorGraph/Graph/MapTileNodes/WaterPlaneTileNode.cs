using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterPlaneTileNode : TerrainNode
{
    [Input] public Terrain[] InputTerrain = null;
    [Input] public float[] InputHeightMap = null;
    [Input] public float WaterLevel = .5f;

    [Output] public Terrain[] OutputTerrain = null;

    public override void Flush()
    {
        InputTerrain = null;
        OutputTerrain = null;
    }

    public override object GetValue(XNode.NodePort port)
    {
        if (port.fieldName == "OutputTerrain")
            return OutputTerrain;

        return null;
    }

    public override void Recalculate()
    {
        Terrain[] InputTerrain = GetInputValue("InputTerrain", this.InputTerrain);
        float[] InputHeightMap = GetInputValue("InputHeightMap", this.InputHeightMap);
        float WaterLevel = GetInputValue("WaterLevel", this.WaterLevel);

        if (IsInputArrayValid(InputTerrain) && IsInputArrayValid(InputHeightMap))
        {
            OutputTerrain = (Terrain[])InputTerrain.Clone();

            for(int i = 0; i < OutputTerrain.Length; i++)
            {
                if(InputHeightMap[i] < WaterLevel)
                {
                    OutputTerrain[i] = Terrain.Water;
                }
            }
        }
    }
}
