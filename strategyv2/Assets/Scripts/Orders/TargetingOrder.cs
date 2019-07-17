using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetingOrder : Order
{
    protected int RememberedTargetId;
    protected float _thresholdDistance = .5f;

    public TargetingOrder(Division controller, int commanderSendingOrderId, string name, int rememberedTargetId, float thresholdDistance = .5f)
        : base(controller, commanderSendingOrderId, name)
    {
        RememberedTargetId = rememberedTargetId;
        _thresholdDistance = thresholdDistance;
    }

    public override string ToString()
    {
        return base.ToString() + $"{RememberedTargetId}";
    }
}
