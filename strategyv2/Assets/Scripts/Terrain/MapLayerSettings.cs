using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum LayerFillAlgorithm
{
    Solid, RandomWalk, Square, PerlinNoise, RandomWalkBlocking, HeightRange, FollowGradient, FollowAlongGradient, AdjacentTiles, Droplets
}

[System.Serializable]
public enum Terrain
{
    Empty, Grass, Road, Water, Farm, Town, Hill, Forest
}


public enum MapLayer
{
    Terrain, Improvement
}


[System.Serializable]
[CreateAssetMenu(fileName = "NewMapLayerSettings", menuName = "Map Layer Settings", order = 0)]
public class MapLayerSettings : ScriptableObject
{
    public Terrain terrain;
    public LayerFillAlgorithm algorithm;
    public MapLayer Layer;
    [Tooltip("The Tile to draw (use a RuleTile for best results)")]
    public TileBase tile;
    public bool useLayeredGradients = true;
    public bool randomSeed;
    public float seed;
    public int iterations = 1;
    public int radius = 1;
    public TerrainTileSettings terrainTile;
    public bool IsEnabled = true;
    public float PerlinNoiseScale = 2.5f;
    public float PerlinNoiseThreshold = .5f;

    //for height range
    public float MinHeight;
    public float MaxHeight;

    //for gradient follow
    public float MinStartHeight;
    public float MinStopHeight;
    public float MaxWidth;
    public float WidthChangeThrotle;

    public float Width;

    public float MinThreshold;
    public float SpawnChance;

    public float MaxGradient;

    public float PercentCovered;
}


//Custom UI for our class
[CustomEditor(typeof(MapLayerSettings))]
public class MapLayerSettings_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        MapLayerSettings mapLayer = (MapLayerSettings)target;
        GUI.changed = false;
        EditorGUILayout.LabelField(mapLayer.name, EditorStyles.boldLabel);

        mapLayer.algorithm = (LayerFillAlgorithm)EditorGUILayout.EnumPopup(new GUIContent("Generation Method", "The generation method we want to use to generate the map"), mapLayer.algorithm);
        mapLayer.Layer = (MapLayer)EditorGUILayout.EnumPopup(new GUIContent("Map Layer", "The layer on the map"), mapLayer.Layer);
        mapLayer.terrain = (Terrain)EditorGUILayout.EnumPopup(new GUIContent("Terrain type", ""), mapLayer.terrain);
        mapLayer.useLayeredGradients = EditorGUILayout.Toggle("useLayeredGradients", mapLayer.useLayeredGradients);
        mapLayer.randomSeed = EditorGUILayout.Toggle("Random Seed", mapLayer.randomSeed);
        mapLayer.tile = EditorGUILayout.ObjectField("", mapLayer.tile, typeof(TileBase), false) as TileBase;
        mapLayer.terrainTile = EditorGUILayout.ObjectField("", mapLayer.terrainTile, typeof(TerrainTileSettings), false) as TerrainTileSettings;
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
            case LayerFillAlgorithm.Droplets:
                mapLayer.PercentCovered = EditorGUILayout.FloatField("PercentCovered", mapLayer.PercentCovered);
                break;
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        AssetDatabase.SaveAssets();

        if (GUI.changed)
            EditorUtility.SetDirty(mapLayer);
    }
}

