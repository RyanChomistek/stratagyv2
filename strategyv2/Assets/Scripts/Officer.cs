using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Officer : Soldier
{
    /// <summary>
    /// how far away this runit will runaway when threatend
    /// </summary>
    public float RunAwayDistance;

    /// <summary>
    /// how low he will let his supply get before resupplying
    /// </summary>
    public float ResupplyPerSoldierThreshold;

    /// <summary>
    /// the number of units at which point the division will split in two
    /// </summary>
    public float SoldierCntSplitThreshold;

    /// <summary>
    /// the number of soldiers this division will send with messengers
    /// </summary>
    public float MessengerDivisionSoldierCnt;

    /// <summary>
    /// the number of soldiers this division will send with scouts
    /// </summary>
    public float ScoutDivisionSoldierCnt;

    /// <summary>
    /// how risky the officer is with attacking
    /// between 0 and 1, sets the threshold for how high of a probablility he needs before attacking
    /// ex. if the probability of winning is .6 and EngagementThreshold is .7 he will not attack
    /// </summary>
    public float EngagementThreshold;

    /// <summary>
    /// sets how many supplys this division uses per tick, the more supplies used the faster the unit will travel
    /// </summary>
    public float SupplyUsage;

    /// <summary>
    /// how far away we think that enemy units will come to assist, and how far away we will send for help if we need it
    /// </summary>
    public float AidDistance;

    /// <summary>
    /// deterines how fast we mutate, 0 - 1
    /// </summary>
    public float MutationFactor;

    public Officer()
        : base()
    {
        Type = SoldierType.Officer;
        RandomizeValues();
    }

    /// <summary>
    /// used for promotion
    /// </summary>
    /// <param name="other"></param>
    public Officer(Soldier other)
        : base(other)
    {
        Type = SoldierType.Officer;
        RandomizeValues();
    }

    /// <summary>
    /// creates a new officer, and uses parent as a base for mutating the values
    /// </summary>
    /// <param name="parent"></param>
    public Officer(Soldier soldierBeingPromoted, Officer parent)
        : base(soldierBeingPromoted)
    {
        Type = SoldierType.Officer;
        
        RunAwayDistance = parent.RunAwayDistance;
        ResupplyPerSoldierThreshold = parent.ResupplyPerSoldierThreshold;
        SoldierCntSplitThreshold = parent.SoldierCntSplitThreshold;
        MessengerDivisionSoldierCnt = parent.MessengerDivisionSoldierCnt;
        ScoutDivisionSoldierCnt = parent.ScoutDivisionSoldierCnt;
        EngagementThreshold = parent.EngagementThreshold;
        SupplyUsage = parent.SupplyUsage;
        AidDistance = parent.AidDistance;
        MutationFactor = parent.MutationFactor;

        MutateValues();
    }

    public void MutateValues()
    {
        MutateValue(ref RunAwayDistance, 1, 1, 20);
        MutateValue(ref ResupplyPerSoldierThreshold, 1, SupplyModifier.LowSupplyPerSoldier, SupplyModifier.HighSupplyPerSoldier);
        MutateValue(ref SoldierCntSplitThreshold, 5, 25, 300);
        MutateValue(ref MessengerDivisionSoldierCnt, 1, 1, 10);
        MutateValue(ref ScoutDivisionSoldierCnt, 1, 1, 10);
        MutateValue(ref EngagementThreshold, .05f, .25f, .75f);
        MutateValue(ref SupplyUsage, .1f, .5f, 2f);
        MutateValue(ref AidDistance, 1, 5, 10);
        MutateValue(ref MutationFactor, .05f, 0, 2);
    }

    public void MutateValue(ref float value, float deltaBase, float min, float max)
    {
        float localMutationFactor = Random.Range(-MutationFactor, MutationFactor);
        value = value + deltaBase * localMutationFactor;
        value = Mathf.Clamp(value, min, max);
    }

    private void RandomizeValues()
    {
        RunAwayDistance = Random.Range(2, 10);
        ResupplyPerSoldierThreshold = Random.Range(SupplyModifier.LowSupplyPerSoldier, SupplyModifier.HighSupplyPerSoldier);
        SoldierCntSplitThreshold = Random.Range(25, 300);
        MessengerDivisionSoldierCnt = Random.Range(1, 10);
        ScoutDivisionSoldierCnt = Random.Range(1, 10);
        EngagementThreshold = Random.Range(.25f, .75f);
        SupplyUsage = Random.Range(.5f, 2f);
        AidDistance = Random.Range(5, 10);
        MutationFactor = Random.Range(.05f, .1f);
    }

    public static Officer PromoteSoldier(Soldier soldier)
    {
        var officer = new Officer(soldier);
        return officer;
    }

    public override string ToString()
    {
        return base.ToString() + $"[RunAwayDistance {RunAwayDistance}, ResupplyPerSoldierThreshold {ResupplyPerSoldierThreshold}, " +
            $"SoldierCntSplitThreshold {SoldierCntSplitThreshold}, messenger division size{MessengerDivisionSoldierCnt}, ScoutDivisionSoldierCnt {ScoutDivisionSoldierCnt}, " +
            $"EngagementThreshold {EngagementThreshold}, SupplyUsage {SupplyUsage}, AidDistance {AidDistance}, MutationFactor {MutationFactor}]";
    }
}
