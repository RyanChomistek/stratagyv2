﻿using System.Collections;
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

    public override void Proceed(Division Host)
    {
        TimeRemaining -= GameManager.Instance.DeltaTime;
    }

    public override bool TestIfFinished(Division Host)
    {
        return TimeRemaining > 0;
    }
}