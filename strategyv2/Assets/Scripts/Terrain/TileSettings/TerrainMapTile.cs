using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainMapTile : MapTile
{
    [System.NonSerialized]
    public List<TerrainMapTile> AdjacentTiles;
    [System.NonSerialized]
    public ImprovementMapTile Improvement;

    public Terrain TerrainType;
    public float Height;
    public Vector2 HeightGradient;

    public TerrainMapTile(TerrainMapTile other, float height, Vector2 heightGradient)
        : base(other)
    {
        this.TerrainType = other.TerrainType;

        this.Height = height;
        this.HeightGradient = heightGradient;
        this.AdjacentTiles = other.AdjacentTiles;
        this.Improvement = other.Improvement;
    }

    public void Update(float gameTime)
    {
        //derivitive of continous growth function A = K * A0 * e^(kt)
        AddWithCap(ref Population, PopulationGrowthRate * InitialPopulation * Mathf.Exp(PopulationGrowthRate * gameTime), MaxPopulation);
        AddWithCap(ref Supply, SupplyGrowthRate * InitialSupply * Mathf.Exp(SupplyGrowthRate * gameTime), MaxSupply);

        foreach (var tile in AdjacentTiles)
        {
            if (tile.Population != Population)
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

    public void SetAdjacentTiles(List<TerrainMapTile> adjacentTiles)
    {
        AdjacentTiles = new List<TerrainMapTile>(adjacentTiles);
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

    public override string ToString()
    {
        return $" {TerrainType.ToString()} supply : {Supply}/{MaxSupply}  Population : {Population}/{MaxPopulation}  height {Height}, {HeightGradient}";
    }
}
