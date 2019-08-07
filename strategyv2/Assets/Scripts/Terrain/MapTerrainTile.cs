using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class MapTerrainTile
{
    public float Supply = 1;
    public float MaxSupply = 200;
    private float _initialSupply = 1;
    [SerializeField]
    private float _supplyGrowthRate = .1f;
    [SerializeField]
    private float _supplySpreadRate = .1f;

    public float Population = 1;
    public float MaxPopulation = 200;
    private float _initialPopulation = 1;
    [SerializeField]
    private float _populationGrowthRate = .1f;
    [SerializeField]
    private float _populationSpreadRate = .1f;

    [System.NonSerialized]
    private List<MapTerrainTile> _adjacentTiles;
    [System.NonSerialized]
    public MapTerrainTile Improvement;

    public uint MoveCost = 1;
    public Terrain TerrainType;
    public float Height;
    public Vector2 HeightGradient;

    [Tooltip("can this tile be improved")]
    public bool Improvable = true;
    [Tooltip("The Tile to draw (use a RuleTile for best results)")]

    public TileBase tile;
    public Color SimpleDisplayColor;

    public MapTerrainTile(TerrainTileSettings other, float height, Vector2 heightGradient)
    {
        this.Supply = other.tile.Supply;
        this.MaxSupply = other.tile.MaxSupply;
        this._initialSupply = this.Supply;
        this._supplyGrowthRate = other.tile._supplyGrowthRate;
        this._supplySpreadRate = other.tile._supplySpreadRate;

        this.Population = other.tile.Population;
        this.MaxPopulation = other.tile.MaxPopulation;
        this._initialPopulation = this.Population;
        this._populationGrowthRate = other.tile._populationGrowthRate;
        this._populationSpreadRate = other.tile._populationSpreadRate;

        this.MoveCost = other.tile.MoveCost;
        this.TerrainType = other.tile.TerrainType;
        this.tile = other.tile.tile;
        this.SimpleDisplayColor = other.tile.SimpleDisplayColor;

        this.Height = height;
        this.HeightGradient = heightGradient;
    }

    public void Update(float gameTime)
    {
        //derivitive of continous growth function A = K * A0 * e^(kt)
        AddWithCap(ref Population, _populationGrowthRate * _initialPopulation * Mathf.Exp(_populationGrowthRate * gameTime), MaxPopulation);
        AddWithCap(ref Supply, _supplyGrowthRate * _initialSupply * Mathf.Exp(_supplyGrowthRate * gameTime), MaxSupply);

        foreach(var tile in _adjacentTiles)
        {
            if(tile.Population != Population)
            {
                Spread(ref Population, ref tile.Population, _populationSpreadRate, MaxPopulation, tile.MaxPopulation);
                Spread(ref Supply, ref tile.Supply, _supplySpreadRate, MaxSupply, tile.MaxSupply);
            }
        }
    }

    private void Spread(ref float value, ref float otherValue, float rate, float cap, float otherCap)
    {
        var average = (otherValue + value) / 2;
        var deltaOther = (average - otherValue) * rate * GameManager.DeltaTime;
        var deltaThis = (average - value) * rate * GameManager.DeltaTime;

        //first add the average pop to each
        float excess = AddWithCap(ref otherValue, deltaOther, otherCap);
        excess += AddWithCap(ref value, deltaThis, cap);

        //then add excess to each
        excess = AddWithCap(ref otherValue, excess, otherCap);
        excess += AddWithCap(ref value, excess, cap);
    }

    public void SetAdjacentTiles(List<MapTerrainTile> adjacentTiles)
    {
        _adjacentTiles = new List<MapTerrainTile>(adjacentTiles);
    }

    //will fill the amount to the cap and return excess
    public float AddWithCap(ref float currentAmount, float amountToAdd, float cap)
    {
        currentAmount += amountToAdd;
        
        if (currentAmount >= cap)
        {
            var excess = currentAmount - cap;
            currentAmount = cap;
            return excess;
        }

        return 0;
    }

    public override string ToString()
    {

        return $" {TerrainType.ToString()} supply : {Supply}/{MaxSupply}  Population : {Population}/{MaxPopulation}  height {Height}, {HeightGradient}";
    }
}