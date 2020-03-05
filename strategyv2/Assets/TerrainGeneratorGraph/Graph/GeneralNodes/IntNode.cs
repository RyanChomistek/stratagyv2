using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntNode : SelfPropagatingNode
{
    [Input] public int i;
    [Output] public int o;

    public override object GetValue(XNode.NodePort port)
    {
        return i;
    }
}