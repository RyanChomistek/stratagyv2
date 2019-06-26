using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitOrder : Order
{
    private float TimeRemaining;

    public WaitOrder(Division controller, int commanderSendingOrderId, float time)
        : base(controller, commanderSendingOrderId, "wait")
    {
        this.TimeRemaining = time;
    }

    public override void Proceed(ControlledDivision Host)
    {
        TimeRemaining -= GameManager.DeltaTime;
    }

    public override bool TestIfFinished(ControlledDivision Host)
    {
        return TimeRemaining > 0;
    }
}
