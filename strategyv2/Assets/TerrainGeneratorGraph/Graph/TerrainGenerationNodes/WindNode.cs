using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("TerrainNodes/Wind")]
public class WindNode : TerrainNode
{
    [Input] public float[] HeightMap = null;
    [Input] public Terrain[] TerrainMap = null;
    [Input] public float currHeight = .75f;
    [Input] public int lookDistance = 5;
    [Input] public float pushDirWeight = .1f;
    [Input] public float moistureStart = .2f;
    [Input] public float waterTileMoistureWeight = .1f;
    [Input] public float moistureLoss = .01f;
    
    [Output] public Vector2[] WindStrength = null;
    [Output] public float[] Moisture = null;

    public override void Flush()
    {
        HeightMap = null;
        WindStrength = null;
        TerrainMap = null;
        Moisture = null;
    }

    public override void Recalculate()
    {
        // Call base to get inputs
        base.Recalculate();

        SquareArray<float> heightMapSA = new SquareArray<float>(HeightMap);
        SquareArray<Terrain> terrainMapSA = new SquareArray<Terrain>(TerrainMap);

        SquareArray<Vector2> windStrengthSA = new SquareArray<Vector2>(heightMapSA.SideLength);
        SquareArray<float> moistureSA = new SquareArray<float>(heightMapSA.SideLength);

        Vector2[] startPoses=  { 
            new Vector2(0,0), 
            new Vector2(0,0), 
            new Vector2(heightMapSA.SideLength-1,0), 
            new Vector2(0,heightMapSA.SideLength-1), 
        };

        Vector2[] CountDirs = {
            new Vector2(1,0),
            new Vector2(0,1),
            new Vector2(0,1),
            new Vector2(1,0),
        };

        Vector2[] StartDirs =  {
            new Vector2(0,1),
            new Vector2(1,0),
            new Vector2(-1,0),
            new Vector2(0,-1),
        };

        for(int iStartPos = 0; iStartPos < startPoses.Length; iStartPos++)
        {
            for (int i = 0; i < heightMapSA.SideLength; i++)
            {
                Vector2 currPos = startPoses[iStartPos] + CountDirs[iStartPos] * (float)i;
                Vector2 windDirCurr = StartDirs[iStartPos];

                int numSteps = 0;

                float moistureCurr = moistureStart;

                while (heightMapSA.InBounds(currPos) && windDirCurr.magnitude > .1f && numSteps < 100)
                {
                    if (heightMapSA[currPos] > currHeight)
                    {
                        break;
                    }

                    windStrengthSA[currPos] += windDirCurr;

                    //if (terrainMapSA[currPos] != Terrain.Water)
                    //    moistureSA[currPos] += moistureCurr;

                    Vector2 pushDir = new Vector2();

                    // loop though all adjacent land and get distances to the heights
                    for (int dx = -lookDistance; dx < lookDistance; dx++)
                    {
                        for (int dy = -lookDistance; dy < lookDistance; dy++)
                        {
                            Vector2 lookDelta = new Vector3(dx, dy);

                            // reject things that are not in the circle
                            if (lookDelta.magnitude > lookDistance)
                            {
                                continue;
                            }

                            Vector2 lookPos = new Vector3(currPos.x + dx, currPos.y + dy);

                            if (!heightMapSA.InBounds(lookPos))
                            {
                                continue;
                            }

                            float weight = Mathf.Abs(currHeight - heightMapSA[new Vector2(currPos.x + dx, currPos.y + dy)]);
                            weight = 1 / weight;
                            pushDir += weight * (-lookDelta);

                            if (terrainMapSA[lookPos] == Terrain.Water)
                            {
                                moistureCurr += waterTileMoistureWeight * weight;
                            }
                            else
                            {
                                float moistureWeight = 1 / (lookDelta.magnitude + 1);
                                moistureSA[lookPos] += moistureCurr * moistureWeight;
                            }
                        }
                    }

                    

                    moistureCurr -= moistureLoss;
                    moistureCurr = Mathf.Clamp(moistureCurr, 0, 1);

                    windDirCurr += pushDir.normalized * pushDirWeight;
                    windDirCurr = windDirCurr.normalized;
                    currPos += windDirCurr;
                    numSteps++;
                }
            }
        }

        WindStrength = windStrengthSA.Array;
        Moisture = moistureSA.Array;
    }
}
