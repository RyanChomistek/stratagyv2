using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNodeEditor;

[CustomNodeEditor(typeof(RecalculateButtonNode))]
public class RecalculateButtonNodeEditor : NodeEditor
{
    public override void OnBodyGUI()
    {
        base.OnBodyGUI();

        if (GUILayout.Button("Recalculate"))
        {
            //var soldiers = new List<Soldier>() { new Soldier() };
            //myTarget.AttachedDivision.TransferSoldiers(soldiers);
            //mapManager.GenerateMap();
            (target.graph as TerrainGeneratorGraph).ForceRecalculateFullGraph();
        }
    }
}
