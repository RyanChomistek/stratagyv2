using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateNodeMenu("TileNodes/Town")]
public class TownTileNode : TerrainNode
{
    [Input] public Terrain[] InputTerrain = null;
    [Input] public Improvement[] InputImprovement = null;
    [Input] public Vector2[] InputGradientMap = null;
    [Input] public float[] InputWaterMap = null;

    [Input] public float HabitabilityThreshold = .1f;
    [Input] public float GradientThreshold = .2f;

    [Output] public Improvement[] OutputImprovement = null;

    public override void Flush()
    {
        InputTerrain = null;
        InputImprovement = null;
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
        Improvement[] InputImprovement = GetInputValue("InputImprovement", this.InputImprovement);
        float HabitabilityThreshold = GetInputValue("HabitabilityThreshold", this.HabitabilityThreshold);
        float GradientThreshold = GetInputValue("GradientThreshold", this.GradientThreshold);

        if (IsInputArrayValid(InputWaterMap) &&
            IsInputArrayValid(InputGradientMap) &&
            IsInputArrayValid(InputTerrain) &&
            IsInputArrayValid(InputImprovement))
        {
            SquareArray<Improvement> inputImprovementMapSquare = new SquareArray<Improvement>(InputImprovement);

            OutputImprovement = (Improvement[])InputImprovement.Clone();
            SquareArray<Improvement> outputImprovementMapSquare = new SquareArray<Improvement>(OutputImprovement);

            SquareArray<Terrain> terrainMapSquare = new SquareArray<Terrain>(InputTerrain);

            SquareArray<float> waterMapSquare = new SquareArray<float>(InputWaterMap);
            SquareArray<Vector2> GradientMapSquare = new SquareArray<Vector2>(InputGradientMap);

            for (int x = 0; x < terrainMapSquare.SideLength; x++)
            {
                for (int y = 0; y < terrainMapSquare.SideLength; y++)
                {
                    if (terrainMapSquare[x, y] == Terrain.Grass &&
                        inputImprovementMapSquare[x, y] != Improvement.Road &&
                        GradientMapSquare[x, y].magnitude < GradientThreshold)
                    {
                        var adjacentTerrain = terrainMapSquare.Adjacent(new Vector2Int(x, y)).DefaultIfEmpty();
                        var terrainScore = adjacentTerrain.Average(terrain =>
                        {
                            switch (terrain)
                            {
                                case Terrain.Grass:
                                    return 0;
                                case Terrain.Water:
                                    return 1;
                                case Terrain.Mountain:
                                    return -1;
                            }

                            return 0f;
                        });

                        var adjacentImprovements = inputImprovementMapSquare.Adjacent(new Vector2Int(x, y)).DefaultIfEmpty();
                        var improvementScore = adjacentImprovements.Average(improvement =>
                        {
                            switch (improvement)
                            {
                                case Improvement.Farm:
                                    return 1f;
                                case Improvement.Forest:
                                    return .2f;
                                case Improvement.Empty:
                                    return 0;
                                case Improvement.Road:
                                    return 1f;
                                case Improvement.Desert:
                                    return -1;
                            }

                            return 0;
                        });

                        var combinedScores = new List<float>() { terrainScore, improvementScore };

                        if (combinedScores.Average() > HabitabilityThreshold)
                        {
                            outputImprovementMapSquare[x, y] = Improvement.Town;
                        }
                    }

                }
            }
        }
        else
        {
            throw new System.Exception("input arrays invalid");
        }
    }
}
