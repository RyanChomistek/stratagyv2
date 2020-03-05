using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntArrayNode : SelfPropagatingNode
{
    [Input] public int Size;
    public int SizeSquared;
    [Output] public int[] Array;
    public override object GetValue(XNode.NodePort port)
    {
        return Array;
    }

    public override void Recalculate()
    {
        int size = GetInputValue("Size", this.Size);
        SizeSquared = size * size;
        Array = new int[SizeSquared];
        base.Recalculate();
    }
}