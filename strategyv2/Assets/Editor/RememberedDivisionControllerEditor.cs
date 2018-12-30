using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RememberedDivisionController))]
public class RememberedDivisionControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        RememberedDivisionController myTarget = (RememberedDivisionController)target;

        if (GUILayout.Button("print division details"))
        {
            Debug.Log(myTarget.AttachedDivision.ToString());
        }

        if (GUILayout.Button("print order details"))
        {
            Debug.Log(myTarget.AttachedDivision.OngoingOrder.ToString());
        }

        if (GUILayout.Button("print predicted location"))
        {
            Debug.Log(myTarget.RememberedAttachedDivision.PredictedPosition);
        }
    }
}
