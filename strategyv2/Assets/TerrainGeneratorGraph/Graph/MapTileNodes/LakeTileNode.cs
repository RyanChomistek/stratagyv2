using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class LakeTileNode : TerrainNode
{
    [Input] public float[] WaterMap;
    [Input] public Vector2[] gradientMap;
    //[Input] Terrain[,] TerrainMapTiles;
}
