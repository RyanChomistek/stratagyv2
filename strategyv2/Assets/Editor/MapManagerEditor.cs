using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapManager))]
public class MapManagerEditor : CustomEditorBase
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        //Reference to our script
        MapManager mapManager = (MapManager)target;

        if (GUILayout.Button("Generate Map"))
        {
            //var soldiers = new List<Soldier>() { new Soldier() };
            //myTarget.AttachedDivision.TransferSoldiers(soldiers);
            mapManager.GenerateMap();
        }

        if (GUILayout.Button("Generate Mesh"))
        {
            //var soldiers = new List<Soldier>() { new Soldier() };
            //myTarget.AttachedDivision.TransferSoldiers(soldiers);
            mapManager.ReconstructMesh();
        }
    }
}
