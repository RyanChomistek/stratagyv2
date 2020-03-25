using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("TileNodes/FarmNode")]
public class FarmTileNode : TerrainNode
{
    [Input] public Terrain[] InputTerrain = null;
    [Input] public Improvement[] InputImprovementTerrain = null;
    [Input] public Vector2[] InputGradientMap = null;
    [Input] public float[] InputWaterMap = null;

    [Input] public float WaterPercentThreshold = .1f;
    [Input] public float GradientThreshold = .2f;

    [Output] public Improvement[] OutputImprovement = null;

    public override void Flush()
    {
        InputTerrain = null;
        InputImprovementTerrain = null;
        InputGradientMap = null;
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
        Vector2[] InputGradientMap = GetInputValue("InputGradientMap", this.InputGradientMap);
        Terrain[] InputTerrain = GetInputValue("InputTerrain", this.InputTerrain);
        Improvement[] InputImprovementTerrain = GetInputValue("InputImprovementTerrain", this.InputImprovementTerrain);
        float WaterPercentThreshold = GetInputValue("WaterPercentThreshold", this.WaterPercentThreshold);
        float GradientThreshold = GetInputValue("GradientThreshold", this.GradientThreshold);

        if (IsInputArrayValid(InputWaterMap) &&
            IsInputArrayValid(InputGradientMap) &&
            IsInputArrayValid(InputTerrain) &&
            IsInputArrayValid(InputImprovementTerrain))
        {
            OutputImprovement = (Improvement[]) InputImprovementTerrain.Clone();
            SquareArray<Improvement> improvementMapSquare = new SquareArray<Improvement>(OutputImprovement);

            SquareArray<Terrain> terrainMapSquare = new SquareArray<Terrain>(InputTerrain);
            SquareArray<float> waterMapSquare = new SquareArray<float>(InputWaterMap);
            SquareArray<Vector2> GradientMapSquare = new SquareArray<Vector2>(InputGradientMap);

            for(int i = 0; i < InputWaterMap.Length; i++)
            {
                if(InputWaterMap[i] > WaterPercentThreshold && terrainMapSquare[i] == Terrain.Grass && GradientMapSquare[i].magnitude < GradientThreshold)
                {
                    improvementMapSquare[i] = Improvement.Farm;
                }
            }
        }
    }
}
