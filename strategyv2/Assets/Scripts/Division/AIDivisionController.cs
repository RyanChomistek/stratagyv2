using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDivisionController : DivisionController
{
    //wrapper to treat attached division as AI division
    public new AIControlledDivision AttachedDivision { get { return base.AttachedDivision as AIControlledDivision; } set { base.AttachedDivision = value; } }

    [SerializeField]
    private bool _enableAI = true;

    void Awake()
    {
        InitAwake();
        base.AttachedDivision = new AIControlledDivision(base.AttachedDivision.TeamId, this, _enableAI);
    }
}
