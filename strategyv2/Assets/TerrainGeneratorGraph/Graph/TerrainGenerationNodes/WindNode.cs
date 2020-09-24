using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("TerrainNodes/Wind")]
public class WindNode : TerrainNode
{
    [Input] public float[] HeightMap = null;
    [Input] public Terrain[] TerrainMap = null;
    [Input] public float startHeight = .75f;
    [Input] public int lookDistance = 5;
    [Input] public int numSteps = 100;
    [Input] public float pushDirWeight = .1f;
    [Input] public float windStartStrength = 3f;
    [Input] public float pressureChangeStrength = 1f;
    [Input] public float moistureStart = .2f;
    [Input] public float waterTileMoistureWeight = .1f;
    [Input] public float moistureLoss = .01f;
    
    [Output] public Vector3[] WindStrength = null;
    [Output] public float[] Moisture = null;
    [Output] public float[] SimpleMoisture = null;

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

        SquareArray<Vector3> windStrengthSA = new SquareArray<Vector3>(heightMapSA.SideLength);
        SquareArray<float> moistureSA = new SquareArray<float>(heightMapSA.SideLength);
        SquareArray<float> simpleMoistureSA = new SquareArray<float>(heightMapSA.SideLength);

        Vector3[] startPoses=  {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(heightMapSA.SideLength-1, 0, 0),
            new Vector3(0, 0, heightMapSA.SideLength-1),
        };

        Vector3[] CountDirs = {
            new Vector3(1, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 0),
        };

        Vector3[] StartDirs =  {
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, -1),
        };

        int numFails = 0;

        ArrayUtilityFunctions.ForMTOneDimension(startPoses.Length, (int iStartPos) => {
            ArrayUtilityFunctions.ForMTOneDimension(heightMapSA.SideLength, (int i) =>
            {
                Vector3 currPos = startPoses[iStartPos] + CountDirs[iStartPos] * (float)i;
                currPos.y = startHeight;
                Vector3 windDirCurr = StartDirs[iStartPos] * windStartStrength;

                int numStepsCurr = 0;
                float moistureCurr = this.Graph.GetRandValue(0, moistureStart);
                while (heightMapSA.InBounds(currPos) && windDirCurr.magnitude > .1f && numStepsCurr < numSteps)
                {
                    if (heightMapSA[currPos] > currPos.y)
                    {
                        numFails++;
                        break;
                    }

                    windStrengthSA[currPos] = Vector3.one * currPos.y; //+= windDirCurr;
                    simpleMoistureSA[currPos] += moistureCurr;
                    Vector3 pushDir = new Vector3();

                    // loop though all adjacent land and get distances to the heights
                    for (int dx = -lookDistance; dx <= lookDistance; dx++)
                    {
                        for (int dz = -lookDistance; dz <= lookDistance; dz++)
                        {
                            Vector3 lookDelta = new Vector3(dx, 0, dz);

                            // reject things that are not in the circle
                            if (lookDelta.magnitude > lookDistance)
                            {
                                continue;
                            }

                            Vector2 lookPos = new Vector2(currPos.x + dx, currPos.z + dz);

                            if (!heightMapSA.InBounds(lookPos))
                            {
                                continue;
                            }

                            float heightDelta = heightMapSA[lookPos] - currPos.y;
                            float weight = Mathf.Exp(-(1 / (float)lookDistance) * (heightDelta * heightDelta));

                            if (lookDelta != Vector3.zero)
                            {
                                //lookDelta.y =  heightDelta * .0001f;
                                pushDir += weight * (-lookDelta);
                            }

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

                    Vector3 newPos = currPos + windDirCurr.normalized;
                    if (heightMapSA.InBounds(newPos))
                    {
                        // increasing the distance to the gound should cause air pressure to go down

                        float airDeltaNew = newPos.y - heightMapSA[newPos];
                        float airDeltaOld = currPos.y - heightMapSA[currPos];
                        float deltaAirPressure = airDeltaNew - airDeltaOld;
                        newPos.y -= deltaAirPressure * pressureChangeStrength;
                    }

                    currPos = newPos;
                    numStepsCurr++;
                }
                
            }, startPoses.Length);
        }, startPoses.Length);

        WindStrength = windStrengthSA.Array;
        Moisture = moistureSA.Array;
        SimpleMoisture = simpleMoistureSA.Array;
    }
}
