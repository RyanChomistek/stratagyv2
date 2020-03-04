using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum LayerFillAlgorithm
{
    Solid, RandomWalk, Square, PerlinNoise, RandomWalkBlocking, HeightRange, FollowGradient, FollowAlongGradient, AdjacentTiles, Lake, River, Mountain
}

[System.Serializable]
public enum Terrain
{
    Empty, Grass, Water, Mountain
}

public enum Improvement
{
    Empty, Road, Farm, Town, Forest
}


public enum MapLayer
{
    Terrain, Improvement
}


[System.Serializable]
[CreateAssetMenu(fileName = "NewMapLayerSettings", menuName = "settings/Map Layer Settings", order = 0)]
public class MapLayerSettings : ScriptableObject
{
    public Terrain terrain;
    public Improvement Improvement;
    public LayerFillAlgorithm algorithm;
    public bool useLayeredGradients = true;
    public bool randomSeed;
    public float seed;
    public int iterations = 1;
    public int radius = 1;
    public MapTileSettings MapTile;
    public bool IsEnabled = true;
    public float PerlinNoiseScale = 2.5f;
    public float PerlinNoiseThreshold = .5f;

    //for height range
    public float MinHeight;
    public float MaxHeight;

    public float MinGradient;
    public float MaxGradient;

    //for gradient follow
    public float MinStartHeight;
    public float MinStopHeight;
    public float MaxWidth;
    public float WidthChangeThrotle;

    public float Width;

    public float MinThreshold;
    public float SpawnChance;

    // Droplets
    public float WaterPercentThreshold;
    public float MaxWaterDepth;
    public float MaxWaterGradient;


}

