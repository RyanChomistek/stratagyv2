using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("In-Out/Improvement/ImprovementInput")]
public class ImprovementGraphInput : TerrainNode
{
    [Input] public float[] TileHeightMap = null;
    [Input] public float[] VertexHeightMap = null;
    [Input] public float[] TileWaterMap = null;
    [Input] public Vector2[] GradientTileMap = null;
    [Input] public Vector2[] LayeredGradientMap = null;
    [Input] public int TileMapSize = -1;
    public TerrainGeneratorGraph TerrainGraph;

    public override void Flush()
    {
        FlushInputs<ImprovementGraphInput>(GetType());
    }

    public override void Recalculate()
    {
        GetInputs<ImprovementGraphInput>(GetType());
        //ShowProps(this, 0, 2);
    }
}
