using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ForestTileNode : TerrainNode
{
    [Input] public Improvement[] InputImprovements;
    [Input] public Vector2[] InputGradientMap;
    [Input] public float Scale = 2.5f;
    [Input] public float Threshold = .5f;

    [Output] public Improvement[] OutputImprovements;
    public override object GetValue(XNode.NodePort port)
    {
        return OutputImprovements;
    }

    public override void Recalculate()
    {
        //visualization = new Texture2D(128, 128);
        Improvement[] InputImprovements = GetInputValue("InputImprovements", this.InputImprovements);
        Vector2[] InputGradientMap = GetInputValue("InputGradientMap", this.InputGradientMap);
        
        if(IsInputArrayValid(InputImprovements) && IsInputArrayValid(InputGradientMap))
        {
            float Scale = GetInputValue("Scale", this.Scale);
            float Threshold = GetInputValue("Threshold", this.Threshold);

            OutputImprovements = (Improvement[]) InputImprovements.Clone();
            SquareArray<Improvement> improvmentMapSquare = new SquareArray<Improvement>(OutputImprovements);
            SquareArray<Vector2> gradientMapSquare = new SquareArray<Vector2>(InputGradientMap);
            System.Random rand;

            rand = new System.Random((int)seed);

            if (randomizeSeed)
            {
                seed = System.DateTime.Now.Millisecond + seed;
                rand = new System.Random((int)seed);
            }

            Vector2 shift = new Vector2((float)rand.NextDouble() * 1000, (float)rand.NextDouble() * 1000);

            float sum = 0;
            for(int i = 0; i < gradientMapSquare.Length; i++)
            {
                sum += gradientMapSquare[i].magnitude;
            }

            Debug.Log(sum / InputGradientMap.Length);

            for (int x = 0; x < improvmentMapSquare.SideLength; x++)
            {
                for (int y = 0; y < improvmentMapSquare.SideLength; y++)
                {
                    float xCoord = shift.x + x / (float) (improvmentMapSquare.SideLength + 1) * Scale;
                    float yCoord = shift.y + y / (float) (improvmentMapSquare.SideLength + 1) * Scale;
                    float sample = Mathf.PerlinNoise(xCoord, yCoord);

                    if (sample > Threshold && gradientMapSquare[x, y].magnitude < .075f)
                    //if (sample > Threshold)
                    {
                        improvmentMapSquare[x, y] = Improvement.Forest;
                    }
                }
            }

            GenerateVisualization(OutputImprovements, (val) => { return val == Improvement.Forest ? Color.green : Color.red; });
        }

        base.Recalculate();
    }

}
