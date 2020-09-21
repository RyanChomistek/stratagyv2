using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("TileNodes/Desert")]
public class DesertTileNode : TerrainNode
{
    [Input] public Terrain[] InputTerrain = null;
    [Input] public Improvement[] InputImprovementTerrain = null;
    [Input] public float[] InputMoistureMap = null;

    [Input] public float MaxMoisturePercent = 2f;

    [Output] public Improvement[] OutputImprovement = null;

    public override void Flush()
    {
        InputTerrain = null;
        InputImprovementTerrain = null;
        InputMoistureMap = null;

        OutputImprovement = null;
    }

    public override void Recalculate()
    {
        base.Recalculate();
        Improvement[] InputImprovementTerrain = GetInputValue("InputImprovementTerrain", this.InputImprovementTerrain);

        if (IsInputArrayValid(InputMoistureMap) &&
            IsInputArrayValid(InputTerrain) &&
            IsInputArrayValid(InputImprovementTerrain))
        {
            OutputImprovement = (Improvement[])InputImprovementTerrain.Clone();
            SquareArray<Improvement> improvementMapSquare = new SquareArray<Improvement>(OutputImprovement);
            // SquareArray<Terrain> terrainMapSquare = new SquareArray<Terrain>(InputTerrain);
            for (int i = 0; i < InputMoistureMap.Length; i++)
            {
                if (InputMoistureMap[i] < MaxMoisturePercent && InputTerrain[i] == Terrain.Grass)
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
