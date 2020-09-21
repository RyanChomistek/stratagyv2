using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("TileNodes/Desert")]
public class DesertTileNode : TerrainNode
{
    [Input] public Terrain[] InputTerrain = null;
    [Input] public Improvement[] InputImprovementTerrain = null;
    [Input] public float[] InputMoistureMap = null;

    public float MaxMoisturePercent = 2f;

    [Output] public Improvement[] OutputImprovement = null;

    public override void Recalculate()
    {
        base.Recalculate();

        OutputImprovement = (Improvement[])InputImprovementTerrain.Clone();
        SquareArray<Improvement> improvementMapSquare = new SquareArray<Improvement>(OutputImprovement);

        for (int i = 0; i < InputMoistureMap.Length; i++)
        {
            if (InputMoistureMap[i] < MaxMoisturePercent && InputTerrain[i] == Terrain.Grass)
            {
                improvementMapSquare[i] = Improvement.Desert;
            }
        }
    }
}
