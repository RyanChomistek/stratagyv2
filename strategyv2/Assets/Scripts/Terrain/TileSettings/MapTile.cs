using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class MapTile
{
    public volatile float Supply = 1;
    public float MaxSupply = 200;
    [HideInInspector]
    public float InitialSupply = 1;
    [SerializeField]
    public float SupplyGrowthRate = .1f;
    [SerializeField]
    public float SupplySpreadRate = .1f;

    public int MoveCost = 1;

    public volatile float Population = 1;
    public float MaxPopulation = 200;
    [HideInInspector]
    public float InitialPopulation = 1;
    [SerializeField]
    public float PopulationGrowthRate = .1f;
    [SerializeField]
    public float PopulationSpreadRate = .1f;
    public Color SimpleDisplayColor;
    public Color SecondaryDisplayColor;
    public Color SedimentColor;

    [Tooltip("can this tile be improved")]
    public bool Improvable = true;

    public MapTile()
    { }

    public MapTile(MapTile other)
    {
        this.Supply = Random.Range(1, other.Supply);
        this.MaxSupply = other.MaxSupply;
        this.InitialSupply = this.Supply;
        this.SupplyGrowthRate = other.SupplyGrowthRate;
        this.SupplySpreadRate = other.SupplySpreadRate;

        this.MoveCost = other.MoveCost;

        this.Population = Random.Range(1, other.Population);
        this.MaxPopulation = other.MaxPopulation;
        this.InitialPopulation = this.Population;
        this.PopulationGrowthRate = other.PopulationGrowthRate;
        this.PopulationSpreadRate = other.PopulationSpreadRate;

        this.Improvable = other.Improvable;
        this.SimpleDisplayColor = other.SimpleDisplayColor;
        this.SecondaryDisplayColor = other.SecondaryDisplayColor;
        this.SedimentColor = other.SedimentColor;
    }
}
