using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static XNodeEditor.NodeEditor;

[CustomNodeEditor(typeof(VisualizeArrayNode))]
public class VisualizeArrayNodeEditor : SelfPropagatingNodeEditor
{
    private TerrainNode simpleNode;

    public override void OnBodyGUI()
    {
        base.OnBodyGUI();
        VisualizeArrayNode node = target as VisualizeArrayNode;

        if (node.visualization != null)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 100);
            GUI.DrawTexture(rect, node.visualization, ScaleMode.ScaleToFit, true, 1.0F);
        }
    }
}
