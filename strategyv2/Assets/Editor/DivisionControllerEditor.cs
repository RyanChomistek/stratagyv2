using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(DivisionController))]
public class DivisionControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        DivisionController myTarget = (DivisionController)target;

        //myTarget.experience = EditorGUILayout.IntField("Experience", myTarget.experience);
        //EditorGUILayout.LabelField("Level", myTarget.Level.ToString());

        if (GUILayout.Button("Add Soldier"))
        {
            var soldiers = new List<Soldier>() { new Soldier() };
            soldiers[0].Count = 5;
            myTarget.AttachedDivision.TransferSoldiers(soldiers);
        }
    }
}