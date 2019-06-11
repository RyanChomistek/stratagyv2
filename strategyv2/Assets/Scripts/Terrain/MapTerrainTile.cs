using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class MapTerrainTile
{
    public float Supply = 1;
    private float _initialSupply = 1;
    [SerializeField]
    private float _supplyGrowthRate = .1f;
    public float Population = 1;
    private float _initialPopulation = 1;
    [SerializeField]
    private float _PopulationGrowthRate = .1f;
    public uint MoveCost = 1;
    public Terrain TerrainType;
    [Tooltip("The Tile to draw (use a RuleTile for best results)")]
    public TileBase tile;
    public Color SimpleDisplayColor;

    public MapTerrainTile(MapTerrainTile other)
    {
        this.Supply = other.Supply;
        this._initialSupply = other._initialSupply;
        this._supplyGrowthRate = other._supplyGrowthRate;

        this.Population = other.Population;
        this._initialPopulation = other._initialPopulation;
        this._PopulationGrowthRate = other._PopulationGrowthRate;

        this.MoveCost = other.MoveCost;
        this.TerrainType = other.TerrainType;
        this.tile = other.tile;
        this.SimpleDisplayColor = other.SimpleDisplayColor;
    }

    public MapTerrainTile(TerrainTileSettings other)
    {
        this.Supply = other.tile.Supply;
        this._initialSupply = this.Supply;
        this._supplyGrowthRate = other.tile._supplyGrowthRate;

        this.Population = other.tile.Population;
        this._initialPopulation = this.Population;
        this._PopulationGrowthRate = other.tile._PopulationGrowthRate;

        this.MoveCost = other.tile.MoveCost;
        this.TerrainType = other.tile.TerrainType;
        this.tile = other.tile.tile;
        this.SimpleDisplayColor = other.tile.SimpleDisplayColor;
    }

    public void Update(float gameTime)
    {
        //derivitive of continous growth function A = K * A0 * e^(kt)
        Population += _PopulationGrowthRate * _initialPopulation * Mathf.Exp(_PopulationGrowthRate * gameTime);
        Supply += _supplyGrowthRate * _initialSupply * Mathf.Exp(_supplyGrowthRate * gameTime);
    }
}

[System.Serializable]
[CreateAssetMenu(fileName = "NewTerrainTile", menuName = "TerrainTile", order = 0)]
public class TerrainTileSettings : ScriptableObject
{
    [SerializeField]
    public MapTerrainTile tile;
}