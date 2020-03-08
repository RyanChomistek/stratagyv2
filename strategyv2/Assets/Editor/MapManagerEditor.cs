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



        /*
         * if (GUILayout.Button("rerender"))
        {
            mapManager.RenderMap(mapManager.CurrentlyDisplayingMapType);
        }

        if (GUILayout.Button("Show Tiles"))
        {
            mapManager.RenderMap(MapDisplays.Tiles);
        }

        if (GUILayout.Button("show population Map Mode"))
        {
            mapManager.RenderMap(MapDisplays.Population);
        }

        if (GUILayout.Button("show supply Map Mode"))
        {
            mapManager.RenderMap(MapDisplays.Supply);
        }

        if (GUILayout.Button("show mvmt Map Mode"))
        {
            mapManager.RenderMap(MapDisplays.MovementSpeed);
        }

        if (GUILayout.Button("show simple Map Mode"))
        {
            mapManager.RenderMap(MapDisplays.Simple);
        }

        if (GUILayout.Button("show area controll Map Mode"))
        {
            mapManager.RenderMap(MapDisplays.PlayerControlledAreas);
        }

        if (GUILayout.Button(""))
        {
            mapManager.RenderMap(MapDisplays.HeightMap);
        }
        


        if (GUILayout.Button("show vision"))
        {
            //var soldiers = new List<Soldier>() { new Soldier() };
            //myTarget.AttachedDivision.TransferSoldiers(soldiers);
            mapManager.RenderMap(MapDisplays.TilesWithVision);
        }
        */
    }
}
