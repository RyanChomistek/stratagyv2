using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IModifier
{
    //modifies the object and returns the current state of the modifier, 
    //for example a low supply modifier might see that the supply on the object has risen sifficiently to change the modifier to be come a normalSupplyModifier
    IModifier Modify(object ObjToModify);
}


public class DivisionModifier : IModifier
{
    public IModifier Modify(object ObjToModify)
    {
        return ModifyDivision(ObjToModify as Division);
    }

    public virtual DivisionModifier ModifyDivision(Division divisionToModify)
    {
        return this;
    }

    public override string ToString()
    {
        return "division modifier";
    }
}

public class SoldierModifier : IModifier
{
    public IModifier Modify(object ObjToModify)
    {
        return ModifySoldier(ObjToModify as Soldier);
    }

    public virtual SoldierModifier ModifySoldier(Soldier soldierToModify)
    {
        return this;
    }

    public override string ToString()
    {
        return "Soldier Modifier";
    }
}