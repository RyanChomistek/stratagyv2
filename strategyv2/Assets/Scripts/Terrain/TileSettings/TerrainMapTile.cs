using System.Collections;
using System.Collections.Generic;
using System.Threading;
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

    public static Mutex mut = new Mutex();

    public TerrainMapTile(TerrainMapTile other, float height, Vector2 heightGradient)
        : base(other)
    {
        this.TerrainType = other.TerrainType;

        this.Height = height;
        this.HeightGradient = heightGradient;
        this.AdjacentTiles = other.AdjacentTiles;
        this.Improvement = other.Improvement;
    }

    /// <summary>
    /// THIS FUNCTION MUST BE THREAD SAFE
    /// </summary>
    /// <param name="gameTime"></param>
    public void Update(float gameTime)
    {
        //Debug.Log($"{TerrainType.ToString()} {Population} {PopulationGrowthRate} {InitialPopulation} {Mathf.Exp(PopulationGrowthRate * gameTime)} {MaxPopulation}");
        //derivitive of continous growth function A = K * A0 * e^(kt)
        AddWithCap(Population, PopulationGrowthRate * InitialPopulation * Mathf.Exp(PopulationGrowthRate * gameTime), MaxPopulation, out float populationAddResult);
        Population = populationAddResult;

        AddWithCap(Supply, SupplyGrowthRate * InitialSupply * Mathf.Exp(SupplyGrowthRate * gameTime), MaxSupply, out float supplyAddResult);
        Supply = supplyAddResult;

        foreach (var otherTile in AdjacentTiles)
        {
            if (otherTile.Population != Population)
            {
                Spread(Population, otherTile.Population, gameTime, PopulationSpreadRate, MaxPopulation, otherTile.MaxPopulation, out float resultPopulation, out float resultOtherPopulation);
                Population = resultPopulation;
                otherTile.Population = resultOtherPopulation;

                Spread(Supply, otherTile.Supply, gameTime, SupplySpreadRate, MaxSupply, otherTile.MaxSupply, out float resultSupply, out float resultOtherSupply);
                Supply = resultSupply;
                otherTile.Supply = resultOtherSupply;
            }
        }

    }

    private void Spread(float value, float otherValue, float gameTime, float rate, float cap, float otherCap, out float resultValue, out float resultOtherValue)
    {
        resultValue = value;
        resultOtherValue = otherValue;

        var average = (resultOtherValue + resultValue) / 2;
        var deltaOther = (average - resultOtherValue) * rate * gameTime;
        var deltaThis = (average - resultValue) * rate * gameTime;

        //first add the average pop to each
        float excess = AddWithCap(resultOtherValue, deltaOther, otherCap, out resultOtherValue);
        excess += AddWithCap(resultValue, deltaThis, cap, out resultValue);

        //then add excess to each
        excess = AddWithCap(resultOtherValue, excess, otherCap, out resultOtherValue);
        excess += AddWithCap(resultValue, excess, cap, out resultValue);
    }

    //will fill the amount to the cap and return excess
    public float AddWithCap(float currentAmount, float amountToAdd, float cap, out float result)
    {
        result = currentAmount;
        result += amountToAdd;

        if (result >= cap)
        {
            var excess = result - cap;
            result = cap;
            return excess;
        }

        return 0;
    }

    public void SetAdjacentTiles(List<TerrainMapTile> adjacentTiles)
    {
        AdjacentTiles = new List<TerrainMapTile>(adjacentTiles);
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
