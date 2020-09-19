using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("In-Out/Improvement/ImprovementInput")]
public class ImprovementGraphInput : InputNode
{
    [Output] public float[] TileHeightMap = null;
    [Output] public float[] VertexHeightMap = null;
    [Output] public Vector2[] GradientTileMap = null;
    [Output] public Vector2[] LayeredGradientMap = null;
    [Output] public int TileMapSize = -1;

    public override void Flush()
    {
        FlushInputs(GetType());
    }

    public override void Recalculate()
    {
        SetLocals(typeof(ImprovementGraphInput));
        Debug.Log("here");
        //ShowProps(this, 0, 2);
    }
}
