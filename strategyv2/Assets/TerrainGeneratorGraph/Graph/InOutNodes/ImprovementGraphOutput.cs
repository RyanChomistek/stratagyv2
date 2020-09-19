using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("In-Out/Improvement/ImprovementOutput")]
public class ImprovementGraphOutput : OutputNode
{
    [Input] public Terrain[] InputTerrain = null;
    [Input] public Improvement[] OutputImprovements = null;
    [Input] public List<RoadPath> OutputRoadPaths = null;
    [Input] public float WaterHight = .25f;
    [Input] public float[] InputWaterMap = null;
}
