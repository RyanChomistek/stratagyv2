using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNodeEditor;

[CustomNodeEditor(typeof(SelfPropagatingNode))]
public class SelfPropagatingNodeEditor : NodeEditor
{
    private SelfPropagatingNode simpleNode;

    private float timeSinceDirty;

    bool IsWaitingToRecalc = false;
    DateTime StartTime;

    public override void OnBodyGUI()
    {
        SelfPropagatingNode node = target as SelfPropagatingNode;
        if (!node.Graph.FlushAfterRecalc)
        {
            if(GUILayout.Button("Recalculate"))
            {
                node.Graph.RecalculateFromNode(node);
            }
        }

        if(node.IsError)
        {
            var style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.red;
            style.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("Error", style);
        }

        GUILayout.Label($"Run Time(ms): {node.RunTime}");

        base.OnBodyGUI();
    }
}
