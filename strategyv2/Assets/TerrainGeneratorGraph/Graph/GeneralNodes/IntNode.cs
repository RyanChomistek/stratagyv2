using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("StaticNodes/IntNode")]
public class IntNode : SelfPropagatingNode
{
    [Input] public int i;
    [Output] public int o;

    public override void Flush()
    {
        
    }

    public override object GetValue(XNode.NodePort port)
    {
        return i;
    }

    public override void Recalculate()
    {
        
    }
}