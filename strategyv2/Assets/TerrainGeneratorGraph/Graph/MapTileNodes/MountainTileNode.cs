using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("TileNodes/MountainTileNode")]
public class MountainTileNode : TerrainNode
{
    [Input] public Terrain[] InputTerrain = null;
    [Input] public float[] InputHeightMap = null;
    [Input] public Vector2[] InputGradientMap = null;
    [Input] public float Threshold = .9f;
    [Input] public float ThresholdGradient = .05f;

    [Output] public Terrain[] OutputTerrain;

    public override void Flush()
    {
        InputTerrain = null;
        InputHeightMap = null;
        InputGradientMap = null;
    }

    public override object GetValue(XNode.NodePort port)
    {
        return OutputTerrain;
    }

    public override void Recalculate()
    {
        Terrain[] InputTerrain = GetInputValue("InputTerrain", this.InputTerrain);
        float[] InputHeightMap = GetInputValue("InputHeightMap", this.InputHeightMap);
        Vector2[] InputGradientMap = GetInputValue("InputGradientMap", this.InputGradientMap);

        if (IsInputArrayValid(InputTerrain) && IsInputArrayValid(InputHeightMap) && IsInputArrayValid(InputGradientMap))
        {
            OutputTerrain = (Terrain[]) InputTerrain.Clone();
            SquareArray<Terrain> terrainMapSquare = new SquareArray<Terrain>(OutputTerrain);

            SquareArray<float> heightMapSquare = new SquareArray<float>(InputHeightMap);
            SquareArray<Vector2> gradientMapSquare = new SquareArray<Vector2>(InputGradientMap);

            if (heightMapSquare.SideLength != gradientMapSquare.SideLength ||
                terrainMapSquare.SideLength != gradientMapSquare.SideLength ||
                terrainMapSquare.SideLength != heightMapSquare.SideLength)
            {
                throw new System.Exception($"arrays not equal dimensions");
            }

            for (int x = 0; x < terrainMapSquare.SideLength; x++)
            {
                for (int y = 0; y < terrainMapSquare.SideLength; y++)
                {
                    if (heightMapSquare[x,y] > Threshold || gradientMapSquare[x, y].magnitude > ThresholdGradient)
                    {
                        terrainMapSquare[x, y] = Terrain.Mountain;
                    }
                }
            }
        }
    }
}
