using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TickingOrder : Order
{
    private float _timeSinceLastTick;
    private float _timeBetweenTicks = 1;

    public TickingOrder(Division controller, int commanderSendingOrderId, string name = "ticking", float timeBetweenTicks = 1)
        : base(controller, commanderSendingOrderId, name)
    {
        this.IsBackgroundOrder = true;
    }

    public override void Proceed(ControlledDivision Host)
    {
        _timeSinceLastTick += GameManager.DeltaTime;

        if (_timeSinceLastTick > _timeBetweenTicks)
        {
            _timeSinceLastTick = 0;
            OnTick(Host);
        }
    }

    public virtual void OnTick(ControlledDivision Host)
    {

    }
}
