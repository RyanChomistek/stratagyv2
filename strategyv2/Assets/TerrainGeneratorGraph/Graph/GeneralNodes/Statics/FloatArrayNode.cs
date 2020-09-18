using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("StaticNodes/FloatArrayNode")]
public class FloatArrayNode : SelfPropagatingNode
{
    [Input] public int Size;
    public int SizeSquared;
    [Output] public float[] Array;

    public bool InitializeValue = false;
    public int InitialValue = 0;

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
        if(InitializeValue)
        {
            for(int i = 0; i < Array.Length; i++)
            {
                Array[i] = InitialValue;
            }
        }
    }
}
