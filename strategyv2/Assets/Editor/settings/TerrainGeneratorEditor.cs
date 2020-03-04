using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HeightMapGeneration
{
    //Custom UI for our class
    [CustomEditor(typeof(TerrainGenerator))]
    public class TerrainGeneratorEditor : CustomEditorBase
    {
        void GuiLine(int i_height = 1)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, i_height);
            rect.height = i_height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            TerrainGenerator terrainGen = (TerrainGenerator)target;

            //Only show the mapsettings UI if we have a reference set up in the editor
            if (terrainGen.GenerationNodes.Count > 0)
            {
                foreach (var node in terrainGen.GenerationNodes)
                {
                    GuiLine(2);
                    if (node != null)
                    {
                        node.Enabled = EditorGUILayout.Toggle("Enabled", node.Enabled);
                        Editor nodeSettingEditor = CreateEditor(node.GetSettings());
                        nodeSettingEditor.OnInspectorGUI();
                    }
                }
            }
        }
    }
}
