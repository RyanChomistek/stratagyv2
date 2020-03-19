using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("")]
public class ForLoopNode : SelfPropagatingNode
{
    [Input] public int Iterations = 1;
    [Input] public TerrainGeneratorGraph InputArray = null;

    [Output] public float[] OutputArray = null;

    public override void Flush()
    {
        throw new System.NotImplementedException();
    }

    public override object GetValue(XNode.NodePort port)
    {
        return null;
    }

    public override void Recalculate()
    {
        throw new System.NotImplementedException();
    }
}
