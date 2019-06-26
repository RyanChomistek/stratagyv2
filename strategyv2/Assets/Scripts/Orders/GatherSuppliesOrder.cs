using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GatherSuppliesOrder : TickingOrder
{
    public GatherSuppliesOrder(Division controller, int commanderSendingOrderId)
        : base(controller, commanderSendingOrderId, "Gather Supplies", 1)
    {
        this.IsBackgroundOrder = true;
    }

    public override void OnClickedInUI(Division Host, PlayerController playerController)
    {
        OrderDisplayManager.Instance.ClearOrders();
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(playerController.GeneralDivision.AttachedDivision, CommanderSendingOrderId);
        CommanderSendingOrder.SendOrderTo(
            new RememberedDivision(Host),
            new GatherSuppliesOrder(Host, CommanderSendingOrderId), ref playerController.GeneralDivision.AttachedDivision.RememberedDivisions);
    }

    public override void OnTick(ControlledDivision Host)
    {
        var tile = MapManager.Instance.GetTileFromPosition(Host.Controller.transform.position);
        if (tile.Supply > 100)
        {
            foreach(var soldier in Host.Soldiers)
            {
                soldier.GatherSupplies(tile);
            }
        }
    }
}
