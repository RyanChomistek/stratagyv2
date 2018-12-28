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
            myTarget.AttachedDivision.TransferSoldiers(soldiers);
        }

        if (GUILayout.Button("print subordinates"))
        {
            var subStr = SerializeSubordinates(myTarget.AttachedDivision);
            Debug.Log(subStr);
        }
    }

    private string SerializeSubordinates(Division division)
    {
        string str = division.ToString();

        foreach(var sub in division.Subordinates)
        {
            str += SerializeSubordinates(sub.Value);
        }

        return str;
    }
}