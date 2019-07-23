using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SupplyModifier : DivisionModifier
{
    public float MovementSpeedModifier = 1f;

    static public float LowSupplyPerSoldier = 1;
    static public float HighSupplyPerSoldier = 8;

    public override DivisionModifier ModifyDivision(Division divisionToModify)
    {
        divisionToModify.Speed *= MovementSpeedModifier;

        float supply = divisionToModify.Supply, numSoldiers = divisionToModify.NumSoldiers;

        if (supply < (LowSupplyPerSoldier * numSoldiers))
        {
            return new LowSupplyModifier();
        }
        else if (supply > (HighSupplyPerSoldier * numSoldiers))
        {
            return new HighSupplyModifier();
        }
        else
        {
            return new SupplyModifier();
        }
    }

    public override string ToString()
    {
        return "neutral supply modifier";
    }
}

public class LowSupplyModifier : SupplyModifier
{
    public LowSupplyModifier()
    {
        MovementSpeedModifier = .5f;
    }

    public override DivisionModifier ModifyDivision(Division divisionToModify)
    {
        return base.ModifyDivision(divisionToModify);
    }

    public override string ToString()
    {
        return "low supply modifier";
    }
}

public class HighSupplyModifier : SupplyModifier
{
    public HighSupplyModifier()
    {
        MovementSpeedModifier = 1.5f;
    }

    public override DivisionModifier ModifyDivision(Division divisionToModify)
    {
        return base.ModifyDivision(divisionToModify);
    }

    public override string ToString()
    {
        return "high supply modifier";
    }
}