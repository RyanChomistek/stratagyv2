using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

[CustomNodeEditor(typeof(TerrainNode))]
public class TerrainNodeEditor : SelfPropagatingNodeEditor
{
    private TerrainNode simpleNode;

    public override void OnBodyGUI()
    {
        base.OnBodyGUI();
    }
}
