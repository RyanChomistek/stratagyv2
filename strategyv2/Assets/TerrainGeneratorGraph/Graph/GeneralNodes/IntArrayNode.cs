using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("StaticNodes/IntArrayNode")]
public class IntArrayNode : SelfPropagatingNode
{
    [Input] public int Size;
    [Input] public int DefaultValue = 0;
    public int SizeSquared;
    [Output] public int[] Array;

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
        int size = GetInputValue("Size", this.Size);
        int DefaultValue = GetInputValue("DefaultValue", this.DefaultValue);
        SizeSquared = size * size;
        Array = new int[SizeSquared];

        for(int i = 0; i < Array.Length; i++)
        {
            Array[i] = DefaultValue;
        }
    }
}