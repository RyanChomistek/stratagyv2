using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneOrder : MultiOrder
{
    public Zone AssignedZone;
    LocalPlayerController.ZoneSelectDelegate UICallback;

    public ZoneOrder(Division controller, int commanderSendingOrderId, Zone zone)
        : base(controller, commanderSendingOrderId, "Zone", new List<Order>())
    {
        AssignedZone = zone;
        MoveToZone(controller);
    }

    private void MoveToZone(Division Host)
    {
        if (AssignedZone != null)
        {
            this.SubOrders.Add(new Move(Host, CommanderSendingOrderId, AssignedZone.BoundingBoxes[0].center, Host.MaxSightDistance));
        }
    }

    public override void OnClickedInUI(Division Host, PlayerController playerController)
    {
        UICallback = zoneDisplay => OnZoneSelected(Host, LocalPlayerController.Instance, zoneDisplay.DisplayedZone);
        //select zone
        LocalPlayerController.Instance.RegisterZoneSelectCallback(UICallback);
    }
    
    public void OnZoneSelected(Division Host, PlayerController playerController, Zone zone)
    {
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(playerController.GeneralDivision.AttachedDivision, CommanderSendingOrderId);
        LocalPlayerController.Instance.UnRegisterZoneSelectCallback(UICallback);
        OrderDisplayManager.Instance.ClearOrders();

        CommanderSendingOrder.SendOrderTo(new RememberedDivision(Host),
            new ZoneOrder(Host, CommanderSendingOrder.DivisionId, zone), ref playerController.GeneralDivision.AttachedDivision.RememberedDivisions);
    }
}
