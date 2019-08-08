using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class MapTerrainTile
{
    public float Supply = 1;
    public float MaxSupply = 200;
    private float InitialSupply = 1;
    [SerializeField]
    public float SupplyGrowthRate = .1f;
    [SerializeField]
    public float SupplySpreadRate = .1f;

    public float Population = 1;
    public float MaxPopulation = 200;
    public float InitialPopulation = 1;
    [SerializeField]
    public float PopulationGrowthRate = .1f;
    [SerializeField]
    public float PopulationSpreadRate = .1f;

    [System.NonSerialized]
    public List<MapTerrainTile> AdjacentTiles;
    [System.NonSerialized]
    public MapTerrainTile Improvement;

    public int MoveCost = 1;
    public Terrain TerrainType;
    public float Height;
    public Vector2 HeightGradient;

    [Tooltip("can this tile be improved")]
    public bool Improvable = true;
    [Tooltip("The Tile to draw (use a RuleTile for best results)")]

    public TileBase tile;
    public Color SimpleDisplayColor;

    public MapTerrainTile()
    {}

    public MapTerrainTile(MapTerrainTile other, float height, Vector2 heightGradient)
    {
        this.Supply = other.Supply;
        this.MaxSupply = other.MaxSupply;
        this.InitialSupply = this.Supply;
        this.SupplyGrowthRate = other.SupplyGrowthRate;
        this.SupplySpreadRate = other.SupplySpreadRate;

        this.Population = other.Population;
        this.MaxPopulation = other.MaxPopulation;
        this.InitialPopulation = this.Population;
        this.PopulationGrowthRate = other.PopulationGrowthRate;
        this.PopulationSpreadRate = other.PopulationSpreadRate;

        this.MoveCost = other.MoveCost;
        this.TerrainType = other.TerrainType;
        this.tile = other.tile;
        this.SimpleDisplayColor = other.SimpleDisplayColor;

        this.Height = height;
        this.HeightGradient = heightGradient;
        this.AdjacentTiles = other.AdjacentTiles;
        this.Improvement = other.Improvement;
        this.Improvable = other.Improvable;
    }
    
    public void ModifyBaseWithImprovement()
    {
        this.Supply += Improvement.Supply;
        this.MaxSupply += Improvement.MaxSupply;
        this.InitialSupply += Improvement.Supply;
        this.SupplyGrowthRate += Improvement.SupplyGrowthRate;
        this.SupplySpreadRate += Improvement.SupplySpreadRate;

        this.Population += Improvement.Population;
        this.MaxPopulation += Improvement.MaxPopulation;
        this.InitialPopulation += Improvement.Population;
        this.PopulationGrowthRate += Improvement.PopulationGrowthRate;
        this.PopulationSpreadRate += Improvement.PopulationSpreadRate;

        this.MoveCost += Improvement.MoveCost;
    }

    public void Combine()
    {

    }

    public void Update(float gameTime)
    {
        //derivitive of continous growth function A = K * A0 * e^(kt)
        AddWithCap(ref Population, PopulationGrowthRate * InitialPopulation * Mathf.Exp(PopulationGrowthRate * gameTime), MaxPopulation);
        AddWithCap(ref Supply, SupplyGrowthRate * InitialSupply * Mathf.Exp(SupplyGrowthRate * gameTime), MaxSupply);

        foreach(var tile in AdjacentTiles)
        {
            if(tile.Population != Population)
            {
                Spread(ref Population, ref tile.Population, PopulationSpreadRate, MaxPopulation, tile.MaxPopulation);
                Spread(ref Supply, ref tile.Supply, SupplySpreadRate, MaxSupply, tile.MaxSupply);
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
        AdjacentTiles = new List<MapTerrainTile>(adjacentTiles);
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