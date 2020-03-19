using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("StaticNodes/FloatArrayNode")]
public class FloatArrayNode : SelfPropagatingNode
{
    [Input] public int Size;
    public int SizeSquared;
    [Output] public float[] Array;

    public override void Flush()
    {
        Array = null;
    }

    public override object GetValue(XNode.NodePort port)
    {
        return Array;
    }

    public override void Recalculate()
    {
        int size = GetInputValue<int>("Size", this.Size);
        SizeSquared = size * size;
        Array = new float[SizeSquared];
    }
}
