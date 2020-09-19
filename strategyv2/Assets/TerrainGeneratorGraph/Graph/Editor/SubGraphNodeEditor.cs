using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNodeEditor;
using UnityEditor;
using System.Linq;

[CustomNodeEditor(typeof(SubGraphNode))]
public class SubGraphNodeEditor : SelfPropagatingNodeEditor
{
    bool HasFetchedInput = false;
    DateTime lastUpdate = DateTime.Now;

    public override void OnBodyGUI()
    {
        base.OnBodyGUI();
        if (HasFetchedInput && (DateTime.Now.Subtract(lastUpdate).TotalSeconds < 1))
        {
            return;
        }

        lastUpdate = DateTime.Now;

        SubGraphNode subGraphNode = target as SubGraphNode;
        if (subGraphNode.SubGraph == null)
            return;

        var subGraphInputNode = subGraphNode.SubGraph.SubGraphInputNode;

        if (subGraphInputNode == null)
            return;

        Dictionary<string, bool> inputPortStillExists = new Dictionary<string, bool>();

        foreach(var port in subGraphNode.DynamicInputs)
        {
            inputPortStillExists[port.fieldName] = false;
        }

        foreach (var output in subGraphInputNode.Outputs)
        {
            inputPortStillExists[output.fieldName] = true;
            if (!subGraphNode.HasPort(output.fieldName))
                subGraphNode.AddDynamicInput(output.ValueType, fieldName: output.fieldName);
        }

        foreach(var kvp in inputPortStillExists)
        {
            if(!kvp.Value)
            {
                subGraphNode.RemoveDynamicPort(kvp.Key);
            }
        }

        Dictionary<string, bool> outputPortStillExists = new Dictionary<string, bool>();
        var subGraphOutputNode = subGraphNode.SubGraph.SubGraphOutputNode;
        foreach (var port in subGraphNode.DynamicOutputs)
        {
            outputPortStillExists[port.fieldName] = false;
        }

        foreach (var input in subGraphOutputNode.Inputs)
        {
            outputPortStillExists[input.fieldName] = true;
            if(!subGraphNode.HasPort(input.fieldName))
                subGraphNode.AddDynamicOutput(input.ValueType, fieldName: input.fieldName);
        }

        foreach (var kvp in outputPortStillExists)
        {
            if (!kvp.Value)
            {
                subGraphNode.RemoveDynamicPort(kvp.Key);
            }
        }

        HasFetchedInput = true;
    }
}
