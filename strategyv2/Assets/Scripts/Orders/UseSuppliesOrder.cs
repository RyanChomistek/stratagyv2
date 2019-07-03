using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UseSuppliesOrder : TickingOrder
{
    public UseSuppliesOrder(Division controller, int commanderSendingOrderId)
        : base(controller, commanderSendingOrderId, "Use Supplies", 1)
    {
        this.IsBackgroundOrder = true;
        this.IsCancelable = false;
    }

    public override void OnTick(ControlledDivision Host)
    {
        foreach (var soldier in Host.Soldiers)
        {
            soldier.UseSupply();
        }

        Host.RecalculateAggrigateValues();


    }
}
