using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : CustomEditorBase
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        //Reference to our script
        MapGenerator levelGen = (MapGenerator)target;

        //Only show the mapsettings UI if we have a reference set up in the editor
        if (levelGen.LayerSettings.Count > 0)
        {
            foreach (var layerSetting in levelGen.LayerSettings)
            {
                if(layerSetting != null)
                {
                    Editor layerSettingEditor = CreateEditor(layerSetting);
                    layerSettingEditor.OnInspectorGUI();
                }
                
            }
        }
    }
}