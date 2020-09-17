using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("TerrainNodes/Rock")]
public class RockNode : TerrainNode
{
    [Input] public float[] BeforeErosion = null;
    [Input] public float[] AfterErosion = null;
    [Input] public float[] Noise = null;

    [Output] public float[] OutputArray = null;

    public override object GetValue(XNode.NodePort port)
    {
        if (port.fieldName == "OutputArray")
            return OutputArray;

        return null;
    }

    public override void Flush()
    {
        BeforeErosion = null;
        AfterErosion = null;
        AfterErosion = null;
        OutputArray = null;
    }

    public override void Recalculate()
    {
        float[] BeforeErosion = GetInputValue("BeforeErosion", this.BeforeErosion);
        float[] AfterErosion = GetInputValue("AfterErosion", this.AfterErosion);
        float[] Noise = GetInputValue("Noise", this.Noise);

        if (AreAllValid(BeforeErosion, AfterErosion, Noise))
        {
            SquareArray<float> BeforeErosionSA = new SquareArray<float>((float[])BeforeErosion.Clone());
            SquareArray<float> AfterErosionSA = new SquareArray<float>((float[])AfterErosion.Clone());
            SquareArray<float> NoiseSA = new SquareArray<float>((float[])Noise.Clone());
            SquareArray<float> outputSA = new SquareArray<float>(BeforeErosionSA.SideLength);

            float min = 0;
            float max = .1f;
            float scale = 1 / max;

            float noiseScale = .2f;
            ArrayUtilityFunctions.Scale(NoiseSA, noiseScale);
            ArrayUtilityFunctions.Add(NoiseSA, 1 - noiseScale);

            // get the difference between the before/after erosion
            // multiply the diff by the noise
            for (int x = 0; x < BeforeErosionSA.SideLength; x++)
            {
                for (int y = 0; y < BeforeErosionSA.SideLength; y++)
                {
                    // before - after = (positive, means soil was taken away| negative soil was added)
                    float delta = (BeforeErosionSA[x, y] - AfterErosionSA[x, y]) * NoiseSA[x, y];
                    //outputSA[x, y] = delta * NoiseSA[x, y];
                    outputSA[x, y] = Mathf.Clamp(delta, min, max) * scale;
                }
            }

            OutputArray = outputSA.Array;
        }
    }
}
