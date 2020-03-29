using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesertTileNode : TerrainNode
{
    [Input] public Terrain[] InputTerrain = null;
    [Input] public Improvement[] InputImprovementTerrain = null;
    [Input] public float[] InputWaterMap = null;

    [Input] public float MaxWaterPercent = .1f;

    [Output] public Improvement[] OutputImprovement = null;

    public override void Flush()
    {
        InputTerrain = null;
        InputImprovementTerrain = null;
        InputWaterMap = null;

        OutputImprovement = null;
    }

    public override object GetValue(XNode.NodePort port)
    {
        if (port.fieldName == "OutputImprovement")
            return OutputImprovement;

        return null;
    }

    public override void Recalculate()
    {
        float[] InputWaterMap = GetInputValue("InputWaterMap", this.InputWaterMap);
        Terrain[] InputTerrain = GetInputValue("InputTerrain", this.InputTerrain);
        Improvement[] InputImprovementTerrain = GetInputValue("InputImprovementTerrain", this.InputImprovementTerrain);
        float MaxWaterPercent = GetInputValue("MaxWaterPercent", this.MaxWaterPercent);

        if (IsInputArrayValid(InputWaterMap) &&
            IsInputArrayValid(InputTerrain) &&
            IsInputArrayValid(InputImprovementTerrain))
        {
            OutputImprovement = (Improvement[])InputImprovementTerrain.Clone();
            SquareArray<Improvement> improvementMapSquare = new SquareArray<Improvement>(OutputImprovement);
            SquareArray<Terrain> terrainMapSquare = new SquareArray<Terrain>(InputTerrain);
            for (int i = 0; i < InputWaterMap.Length; i++)
            {
                if (InputWaterMap[i] < MaxWaterPercent && terrainMapSquare[i] == Terrain.Grass)
                {
                    improvementMapSquare[i] = Improvement.Desert;
                }
            }
        }
        else
        {
            throw new System.Exception("input arrays invalid");
        }
    }
}
