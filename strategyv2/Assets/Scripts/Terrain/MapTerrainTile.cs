using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class MapTerrainTile
{
    public float Supply = 1;
    public float Population = 1;
    public uint MoveCost = 1;
    public Terrain TerrainType;
    [Tooltip("The Tile to draw (use a RuleTile for best results)")]
    public TileBase tile;

    public MapTerrainTile(MapTerrainTile other)
    {
        this.Supply = other.Supply;
        this.Population = other.Population;
        this.MoveCost = other.MoveCost;
        this.TerrainType = other.TerrainType;
        this.tile = other.tile;
    }

    public MapTerrainTile(TerrainTileSettings other)
    {
        this.Supply = other.tile.Supply;
        this.Population = other.tile.Population;
        this.MoveCost = other.tile.MoveCost;
        this.TerrainType = other.tile.TerrainType;
        this.tile = other.tile.tile;
    }
}

[System.Serializable]
[CreateAssetMenu(fileName = "NewTerrainTile", menuName = "TerrainTile", order = 0)]
public class TerrainTileSettings : ScriptableObject
{
    [SerializeField]
    public MapTerrainTile tile;
}