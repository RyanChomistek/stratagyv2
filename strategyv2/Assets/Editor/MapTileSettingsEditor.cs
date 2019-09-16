using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

[CustomEditor(typeof(MapTileSettings))]
public class MapTileSettingsEditor : CustomEditorBase
{
    public override void OnInspectorGUI()
    {
        MapTileSettings tileSettings = (MapTileSettings)target;

        tileSettings.Layer = (MapLayer)EditorGUILayout.EnumPopup(new GUIContent("Tile Type", "Type of this tile"), tileSettings.Layer);
        serializedObject.Update();

        if (tileSettings.Layer == MapLayer.Terrain)
        {
            var terrainProp = serializedObject.FindProperty(nameof(tileSettings.TerrainMapTileSettings));
            this.HandleProperty(terrainProp);
        }
        else
        {
            var terrainProp = serializedObject.FindProperty(nameof(tileSettings.ImprovementMapTileSettings));
            this.HandleProperty(terrainProp);
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(tileSettings);
    }
}
 