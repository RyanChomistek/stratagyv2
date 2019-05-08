﻿using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(DivisionController))]
public class DivisionControllerEditor : CustomEditorBase
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        //DrawDefaultInspector();
        DivisionController myTarget = (DivisionController)target;

        //myTarget.experience = EditorGUILayout.IntField("Experience", myTarget.experience);
        //EditorGUILayout.LabelField("Level", myTarget.Level.ToString());

        if (GUILayout.Button("Add Soldier"))
        {
            var soldiers = new List<Soldier>() { new Soldier() };
            myTarget.AttachedDivision.TransferSoldiers(soldiers);
        }

        if (GUILayout.Button("print subordinates"))
        {
            var subStr = myTarget.AttachedDivision.SerializeSubordinates(myTarget.AttachedDivision, myTarget.AttachedDivision);
            Debug.Log(subStr);
        }

        if (GUILayout.Button("print Remembered"))
        {
            var subStr = Serializeremembered(myTarget.AttachedDivision);
            Debug.Log(subStr);
        }

        if (GUILayout.Button("Destroy"))
        {
            myTarget.AttachedDivision.DestroyDivision(myTarget.AttachedDivision);
        }

        if (GUILayout.Button("Refresh from Visible"))
        {
            myTarget.RefreshVisibleDivisions();
        }
    }



    private string Serializeremembered(ControlledDivision division)
    {
        string str = "";

        foreach (var remembered in division.RememberedDivisions)
        {
            str += $"<{remembered}> \n";
        }

        return str;
    }
}