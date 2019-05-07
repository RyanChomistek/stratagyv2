using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RememberedDivisionController))]
public class RememberedDivisionControllerEditor : CustomEditorBase
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        //DrawDefaultInspector();
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

        if (GUILayout.Button("print Remembered"))
        {
            var subStr = Serializeremembered(myTarget.AttachedDivision);
            Debug.Log(subStr);
        }
    }

    private string Serializeremembered(Division division)
    {
        string str = "";

        foreach (var remembered in division.RememberedDivisions)
        {
            str += remembered;
        }

        return str;
    }
}
