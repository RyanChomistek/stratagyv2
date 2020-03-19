using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("")]
public class StaticVariableNode<T> : SelfPropagatingNode
{
    [Input] public T i;
    [Output] public T o;

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
