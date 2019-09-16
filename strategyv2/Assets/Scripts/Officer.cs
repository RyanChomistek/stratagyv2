using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct TrainableParameter
{
    public float Value;
    public float Min, Max;
    public float AverageMutationAmount;

    public void MutateValue(float mutationFactor)
    {
        float localMutationFactor = Random.Range(-mutationFactor, mutationFactor);
        Value = Value + AverageMutationAmount * localMutationFactor;
        Value = Mathf.Clamp(Value, Min, Max);
    }

    public void Randomize()
    {
        Value = Random.Range(Min, Max);
    }

    public override string ToString()
    {
        return "" + Value;
    }
}

public class Officer : Soldier
{
    /// <summary>
    /// how far away this runit will runaway when threatend
    /// </summary>
    public TrainableParameter RunAwayDistance = new TrainableParameter()
    {
        Min = 1,
        Max = 20,
        AverageMutationAmount = 1
    };

    /// <summary>
    /// how low he will let his supply get before resupplying
    /// </summary>
    public TrainableParameter ResupplyPerSoldierThreshold = new TrainableParameter()
    {
        Min = SupplyModifier.LowSupplyPerSoldier * 2,
        Max = SupplyModifier.HighSupplyPerSoldier,
        AverageMutationAmount = 1
    };

    /// <summary>
    /// the number of units at which point the division will split in two
    /// </summary>
    public TrainableParameter SoldierCntSplitThreshold = new TrainableParameter()
    {
        Min = 25,
        Max = 500,
        AverageMutationAmount = 5
    };

    /// <summary>
    /// the percentage of soldiers which will be split into the new unit
    /// </summary>
    public TrainableParameter PercentOfSoldiersToSplit = new TrainableParameter()
    {
        Min = .25f,
        Max = .75f,
        AverageMutationAmount = .05f
    };

    /// <summary>
    /// the number of soldiers this division will send with messengers
    /// </summary>
    public TrainableParameter MessengerDivisionSoldierCnt = new TrainableParameter()
    {
        Min = 1,
        Max = 10,
        AverageMutationAmount = 1
    };

    /// <summary>
    /// the number of soldiers this division will send with scouts
    /// </summary>
    public TrainableParameter ScoutDivisionSoldierCnt = new TrainableParameter()
    {
        Min = 1,
        Max = 10,
        AverageMutationAmount = 1
    };

    /// <summary>
    /// how risky the officer is with attacking
    /// between 0 and 1, sets the threshold for how high of a probablility he needs before attacking
    /// ex. if the probability of winning is .6 and EngagementThreshold is .7 he will not attack
    /// </summary>
    public TrainableParameter EngagementThreshold = new TrainableParameter()
    {
        Min = .25f,
        Max = .75f,
        AverageMutationAmount = .05f
    };

    /// <summary>
    /// sets how many supplys this division uses per tick, the more supplies used the faster the unit will travel
    /// </summary>
    public TrainableParameter SupplyUsage = new TrainableParameter()
    {
        Min = .5f,
        Max = 2f,
        AverageMutationAmount = .1f
    };

    /// <summary>
    /// how far away we think that enemy units will come to assist, and how far away we will send for help if we need it
    /// </summary>
    public TrainableParameter AidDistance = new TrainableParameter()
    {
        Min = 5,
        Max = 10,
        AverageMutationAmount = 1
    };

    /// <summary>
    /// deterines how fast we mutate, 0 - 1
    /// </summary>
    public TrainableParameter MutationFactor = new TrainableParameter()
    {
        Min = .5f,
        Max = 1.5f,
        AverageMutationAmount = .05f
    };

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
    /// copy
    /// </summary>
    /// <param name="other"></param>
    public Officer(Officer other)
        : base(other)
    {
        RunAwayDistance = other.RunAwayDistance;
        ResupplyPerSoldierThreshold = other.ResupplyPerSoldierThreshold;
        SoldierCntSplitThreshold = other.SoldierCntSplitThreshold;
        PercentOfSoldiersToSplit = other.PercentOfSoldiersToSplit;
        MessengerDivisionSoldierCnt = other.MessengerDivisionSoldierCnt;
        ScoutDivisionSoldierCnt = other.ScoutDivisionSoldierCnt;
        EngagementThreshold = other.EngagementThreshold;
        SupplyUsage = other.SupplyUsage;
        AidDistance = other.AidDistance;
        MutationFactor = other.MutationFactor;
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
        PercentOfSoldiersToSplit = parent.PercentOfSoldiersToSplit;
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
        RunAwayDistance.MutateValue(MutationFactor.Value);
        ResupplyPerSoldierThreshold.MutateValue(MutationFactor.Value);

        SoldierCntSplitThreshold.MutateValue(MutationFactor.Value);
        PercentOfSoldiersToSplit.MutateValue(MutationFactor.Value);
        MessengerDivisionSoldierCnt.MutateValue(MutationFactor.Value);
        ScoutDivisionSoldierCnt.MutateValue(MutationFactor.Value);
        EngagementThreshold.MutateValue(MutationFactor.Value);
        SupplyUsage.MutateValue(MutationFactor.Value);
        AidDistance.MutateValue(MutationFactor.Value);
        MutationFactor.MutateValue(MutationFactor.Value);
    }

    private void RandomizeValues()
    {
        RunAwayDistance.Randomize();
        ResupplyPerSoldierThreshold.Randomize();
        SoldierCntSplitThreshold.Randomize();
        PercentOfSoldiersToSplit.Randomize();
        MessengerDivisionSoldierCnt.Randomize();
        ScoutDivisionSoldierCnt.Randomize();
        EngagementThreshold.Randomize();
        SupplyUsage.Randomize();
        AidDistance.Randomize();
        MutationFactor.Randomize();
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
