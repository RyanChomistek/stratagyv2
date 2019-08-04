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
        Officer officer = Host.CommandingOfficer;
        foreach (var soldier in Host.Soldiers)
        {
            soldier.UseSupply(officer);
        }

        if(Host.DivisionModifiers.ContainsKey(typeof(LowSupplyModifier)))
        {
            Host.TakeDamage(1, Host);
        }

        Host.RecalculateAggrigateValues();


    }
}
