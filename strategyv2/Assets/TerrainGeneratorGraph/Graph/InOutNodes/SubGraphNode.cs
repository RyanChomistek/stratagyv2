using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateNodeMenu("SubGraph")]
public class SubGraphNode : TerrainNode
{
    public TerrainGeneratorGraph SubGraph;

    public override void Flush()
    {
        if(SubGraph.FlushAfterRecalc)
            SubGraph.FlushNodeData();
    }

    public override void Recalculate()
    {
        var subGraphInputNode = SubGraph.SubGraphInputNode;
        if (subGraphInputNode == null)
        {
            IsError = true;
            return;
        }

        subGraphInputNode.SetSource(this);

        // Recalc the sub graph
        SubGraph.RecalculateFullGraphNoFlush();
    }

    public override object GetValue(XNode.NodePort port)
    {
        OutputNode subGraphOutputNode = SubGraph.SubGraphOutputNode;
        return subGraphOutputNode.GetValue(port);
    }
}
