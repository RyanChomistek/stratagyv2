using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDivisionController : DivisionController
{
    //wrapper to treat attached division as AI division
    public new AIControlledDivision AttachedDivision { get { return base.AttachedDivision as AIControlledDivision; } set { base.AttachedDivision = value; } }
    
    void Awake()
    {
        InitAwake();

        int numsoldiers = base.AttachedDivision.NumSoldiers;
        Debug.Log(base.AttachedDivision);
        base.AttachedDivision = new AIControlledDivision(base.AttachedDivision.TeamId, this);
        if (GameManager.DEBUG)
        {
            for (int i = 0; i < numsoldiers; i++)
            {
                AttachedDivision.Soldiers.Add(new Soldier());
            }

        }

        AttachedDivision.Init(this);
    }
}
