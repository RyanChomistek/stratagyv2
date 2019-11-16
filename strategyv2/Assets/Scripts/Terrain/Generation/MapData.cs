using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapData
{
    [SerializeField]
    public Terrain[,] terrainMap;
    [SerializeField]
    public Improvement[,] improvmentMap;
    [SerializeField]
    public float[,] RawHeightMap;
    [SerializeField]
    public float[,] HeightMap;
    [NonSerialized]
    public float[,] WaterMap;

    [SerializeField]
    public Vector2[,] GradientMap;
    [SerializeField]
    public Vector2[,] LayeredGradientMap;
}
