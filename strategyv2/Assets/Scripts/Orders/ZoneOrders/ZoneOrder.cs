using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneOrder : MultiOrder
{
    public int AssignedZoneId;
    protected LocalPlayerController.ZoneSelectDelegate UICallback;

    public ZoneOrder(Division controller, int commanderSendingOrderId, Zone zone, string name = "Zone")
        : base(controller, commanderSendingOrderId, name, new List<Order>())
    {
        if (zone != null)
        {
            AssignedZoneId = zone.id;
            MoveToZone(controller);
        }
    }

    private void MoveToZone(Division Host)
    {
        Debug.Log($"{Host.Name} moving to zone {AssignedZoneId}");

        

        if (Host.Zones.TryGetValue(AssignedZoneId, out Zone AssignedZone))
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
    
    public virtual void OnZoneSelected(Division Host, PlayerController playerController, Zone zone)
    {
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(playerController.GeneralDivision.AttachedDivision, CommanderSendingOrderId);
        LocalPlayerController.Instance.UnRegisterZoneSelectCallback(UICallback);
        OrderDisplayManager.Instance.ClearOrders();
        Debug.Log("zone" + " " +zone);
        Debug.Log($"{Host.Zones.Count}");
        CommanderSendingOrder.SendOrderTo(new RememberedDivision(Host),
            new ZoneOrder(Host, CommanderSendingOrder.DivisionId, zone), ref playerController.GeneralDivision.AttachedDivision.RememberedDivisions);
    }
}
