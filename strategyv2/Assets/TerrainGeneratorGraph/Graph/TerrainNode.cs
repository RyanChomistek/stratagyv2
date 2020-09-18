using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;

[CreateNodeMenu("")]
public abstract class TerrainNode : SelfPropagatingNode
{
    public override void RecalculateNextNode(SelfPropagatingNode propogatingNode)
    {
        base.RecalculateNextNode(propogatingNode);
    }
}
