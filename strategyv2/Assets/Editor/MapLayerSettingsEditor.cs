using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//Custom UI for our class
[CustomEditor(typeof(MapLayerSettings))]
public class MapLayerSettings_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        MapLayerSettings mapLayer = (MapLayerSettings)target;
        GUI.changed = false;
        EditorGUILayout.LabelField(mapLayer.name, EditorStyles.boldLabel);

        if (mapLayer.MapTile.Layer == MapLayer.Terrain)
        {
            mapLayer.terrain = (Terrain)EditorGUILayout.EnumPopup(new GUIContent("Terrain type", ""), mapLayer.terrain);
        }
        else
        {
            mapLayer.Improvement = (Improvement)EditorGUILayout.EnumPopup(new GUIContent("Improvement type", ""), mapLayer.Improvement);
        }

        mapLayer.algorithm = (LayerFillAlgorithm)EditorGUILayout.EnumPopup(new GUIContent("Generation Method", "The generation method we want to use to generate the map"), mapLayer.algorithm);

        mapLayer.useLayeredGradients = EditorGUILayout.Toggle("useLayeredGradients", mapLayer.useLayeredGradients);
        mapLayer.randomSeed = EditorGUILayout.Toggle("Random Seed", mapLayer.randomSeed);
        mapLayer.MapTile = EditorGUILayout.ObjectField("", mapLayer.MapTile, typeof(MapTileSettings), false) as MapTileSettings;
        mapLayer.IsEnabled = EditorGUILayout.Toggle("Enable", mapLayer.IsEnabled);

        mapLayer.iterations = EditorGUILayout.IntField("Iterations", mapLayer.iterations);
        //Only appear if we have the random seed set to false
        if (!mapLayer.randomSeed)
        {
            mapLayer.seed = EditorGUILayout.FloatField("Seed", mapLayer.seed);
        }

        //Shows different options depending on what algorithm is selected
        switch (mapLayer.algorithm)
        {
            case LayerFillAlgorithm.Solid:
                //No additional Variables
                break;
            case LayerFillAlgorithm.RandomWalk:
            case LayerFillAlgorithm.RandomWalkBlocking:
                mapLayer.iterations = EditorGUILayout.IntField("Iterations", mapLayer.iterations);
                mapLayer.radius = EditorGUILayout.IntField("Radius", mapLayer.radius);
                break;
            case LayerFillAlgorithm.Square:
                mapLayer.radius = EditorGUILayout.IntField("Size", mapLayer.radius);
                break;
            case LayerFillAlgorithm.PerlinNoise:
                mapLayer.PerlinNoiseScale = EditorGUILayout.FloatField("Perlin Noise Scale", mapLayer.PerlinNoiseScale);
                mapLayer.PerlinNoiseThreshold = EditorGUILayout.FloatField("Perlin Noise Threshold", mapLayer.PerlinNoiseThreshold);
                mapLayer.MaxGradient = EditorGUILayout.FloatField("MaxGradient", mapLayer.MaxGradient);
                break;
            case LayerFillAlgorithm.HeightRange:
                mapLayer.MinHeight = EditorGUILayout.FloatField("MinHeight", mapLayer.MinHeight);
                mapLayer.MaxHeight = EditorGUILayout.FloatField("MaxHeight", mapLayer.MaxHeight);
                break;
            case LayerFillAlgorithm.FollowGradient:
                mapLayer.MinStartHeight = EditorGUILayout.FloatField("minStartHeight", mapLayer.MinStartHeight);
                mapLayer.MinStopHeight = EditorGUILayout.FloatField("MinStopHeight", mapLayer.MinStopHeight);
                mapLayer.MaxWidth = EditorGUILayout.FloatField("MaxWidth", mapLayer.MaxWidth);
                mapLayer.WidthChangeThrotle = EditorGUILayout.FloatField("WidthChangeThrotle", mapLayer.WidthChangeThrotle);
                break;
            case LayerFillAlgorithm.FollowAlongGradient:
                mapLayer.Width = EditorGUILayout.FloatField("Width", mapLayer.Width);
                break;
            case LayerFillAlgorithm.AdjacentTiles:
                mapLayer.MinThreshold = EditorGUILayout.FloatField("MinThreshold", mapLayer.MinThreshold);
                mapLayer.MaxGradient = EditorGUILayout.FloatField("MaxGradient", mapLayer.MaxGradient);
                mapLayer.SpawnChance = EditorGUILayout.FloatField("SpawnChance", mapLayer.SpawnChance);
                mapLayer.radius = EditorGUILayout.IntField("Radius", mapLayer.radius);
                break;
            case LayerFillAlgorithm.Lake:
                mapLayer.WaterPercentThreshold = EditorGUILayout.FloatField("WaterPercentThreshold", mapLayer.WaterPercentThreshold);
                mapLayer.MaxWaterDepth = EditorGUILayout.FloatField("maxWaterDepth", mapLayer.MaxWaterDepth);
                mapLayer.MaxWaterGradient = EditorGUILayout.FloatField("MaxWaterGradient", mapLayer.MaxWaterGradient);
                break;
            case LayerFillAlgorithm.Mountain:
                mapLayer.MinHeight = EditorGUILayout.FloatField("MinHeight", mapLayer.MinHeight);
                mapLayer.MaxHeight = EditorGUILayout.FloatField("MaxHeight", mapLayer.MaxHeight);
                mapLayer.MinGradient = EditorGUILayout.FloatField("MinGradient", mapLayer.MinGradient);
                mapLayer.MaxGradient = EditorGUILayout.FloatField("MaxGradient", mapLayer.MaxGradient);
                break;
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        //AssetDatabase.SaveAssets();

        //if (GUI.changed)
        //    EditorUtility.SetDirty(mapLayer);
    }
}