using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIControlledDivision : ControlledDivision
{
    [SerializeField]
    private bool _enableAI = true;

    public AIControlledDivision(int teamId = -1, DivisionController controller = null, bool enableAI = true)
        : base(teamId, controller)
    {
        _enableAI = enableAI;
        if(_enableAI)
        {
            ReceiveOrder(new AIOrder(this, DivisionId));
        }
    }
}
