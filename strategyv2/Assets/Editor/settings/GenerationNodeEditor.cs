using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HeightMapGeneration
{
    [CustomEditor(typeof(GenerationNode))]
    public class GenerationNodeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GenerationNode mapLayer = (GenerationNode)target;

            EditorGUILayout.LabelField(mapLayer.GUID.ToString());

            mapLayer.ShowInternalData = EditorGUILayout.Toggle("ShowInternalData", mapLayer.ShowInternalData);
            if (mapLayer.ShowInternalData)
            {
                base.OnInspectorGUI();
            }

            mapLayer.Enabled = EditorGUILayout.Toggle("Enabled", mapLayer.Enabled);
            Algorithm newAlgo = (Algorithm)EditorGUILayout.EnumPopup(new GUIContent("Node Type", ""), mapLayer.Type);
            Type settingsType = NodeUtils.AlgorithmTypeMap[newAlgo];

            if (newAlgo != mapLayer.Type || mapLayer.GUID == "")
            {
                mapLayer.Type = newAlgo;

                if(mapLayer.GUID != "")
                {
                    // Delete the old temp file
                    AssetDatabase.DeleteAsset($"Assets/Saves/GenerationTempFiles/{mapLayer.GUID.ToString()}.asset");
                }

                // Make a new one
                var settings = CreateInstance(settingsType);
                mapLayer.GUID = System.Guid.NewGuid().ToString();
                AssetDatabase.CreateAsset(settings, $"Assets/Saves/GenerationTempFiles/{mapLayer.GUID.ToString()}.asset");
            }

            mapLayer.Type = newAlgo;

            Editor settingsEditor = CreateEditor(mapLayer.GetSettings());
            if (settingsEditor != null)
                settingsEditor.OnInspectorGUI();
        }
    }
}