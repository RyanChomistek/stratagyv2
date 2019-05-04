using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
[CreateAssetMenu(fileName = "NewTerrainTile", menuName = "TerrainTile", order = 0)]
public class TerrainTile : ScriptableObject
{
    public float Supply = 1;
    public float Population = 1;
    public float MovementSpeedModifier = 1;
    public Terrain TerrainType;
    [Tooltip("The Tile to draw (use a RuleTile for best results)")]
    public TileBase tile;

    public TerrainTile(TerrainTile other)
    {
        this.Supply = other.Supply;
        this.Population = other.Population;
        this.MovementSpeedModifier = other.MovementSpeedModifier;
        this.TerrainType = other.TerrainType;
        this.tile = other.tile;
    }
}
