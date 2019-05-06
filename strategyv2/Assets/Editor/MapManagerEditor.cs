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

        if (GUILayout.Button("Show Tiles"))
        {
            //var soldiers = new List<Soldier>() { new Soldier() };
            //myTarget.AttachedDivision.TransferSoldiers(soldiers);
            mapManager.RenderMapWithTiles();
        }

        if (GUILayout.Button("show population Map Mode"))
        {
            //var soldiers = new List<Soldier>() { new Soldier() };
            //myTarget.AttachedDivision.TransferSoldiers(soldiers);
            mapManager.RenderMapWithKey(x => x.Population);
        }

        if (GUILayout.Button("show supply Map Mode"))
        {
            //var soldiers = new List<Soldier>() { new Soldier() };
            //myTarget.AttachedDivision.TransferSoldiers(soldiers);
            mapManager.RenderMapWithKey(x => x.Supply);
        }

        if (GUILayout.Button("show mvmt Map Mode"))
        {
            //var soldiers = new List<Soldier>() { new Soldier() };
            //myTarget.AttachedDivision.TransferSoldiers(soldiers);
            mapManager.RenderMapWithKey(x => 1 / (float)x.MoveCost);
        }
    }
}
