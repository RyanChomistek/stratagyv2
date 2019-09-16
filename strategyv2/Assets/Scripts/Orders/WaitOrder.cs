using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitOrder : Order
{
    private float TimeRemaining;
    public Action<ControlledDivision> OnFinish;

    public WaitOrder(Division controller, int commanderSendingOrderId, float time)
        : base(controller, commanderSendingOrderId, "wait")
    {
        this.TimeRemaining = time;
    }

    public WaitOrder(Division controller, int commanderSendingOrderId, float time, Action<ControlledDivision> onFinish)
        : base(controller, commanderSendingOrderId, "wait")
    {
        this.TimeRemaining = time;
        this.OnFinish = onFinish;
    }

    public override void Proceed(ControlledDivision Host)
    {
        TimeRemaining -= GameManager.DeltaTime;
    }

    public override bool TestIfFinished(ControlledDivision Host)
    {
        if(TimeRemaining < 0)
        {
            OnFinish?.Invoke(Host);
            return true;
        }

        return false;
    }
}
