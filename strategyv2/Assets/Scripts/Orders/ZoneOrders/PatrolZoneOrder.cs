using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolZoneOrder : ZoneOrder
{
    public PatrolZoneOrder(Division controller, int commanderSendingOrderId, Zone zone)
        : base(controller, commanderSendingOrderId, zone, "Patrol Zone ")
    {
    }

    protected override bool TryStartNextOrder(ControlledDivision Host)
    {
        base.TryStartNextOrder(Host);
        if (Host.Zones.TryGetValue(AssignedZoneId, out Zone AssignedZone))
        {
            //pick a random point in the zone 
            Vector3 nextPoint = AssignedZone.GetRandomPoint();
            //go there
            this.OrderQueue.Add(new Move(Host, CommanderSendingOrderId, nextPoint, .5f));
        }
        else
        {
            Debug.Log("patroll canceled because invalid zone");
            Canceled = true;
            return false;
        }

        return true;
    }

    public override void OnZoneSelected(Division Host, PlayerController playerController, Zone zone)
    {
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(playerController.GeneralDivision.AttachedDivision, CommanderSendingOrderId);
        LocalPlayerController.Instance.UnRegisterZoneSelectCallback(UICallback);
        OrderDisplayManager.Instance.ClearOrders();
        Debug.Log("zone" + " " + zone);
        Debug.Log(Host.Zones.Count);
        CommanderSendingOrder.SendOrderTo(new RememberedDivision(Host),
            new PatrolZoneOrder(Host, CommanderSendingOrder.DivisionId, zone), ref playerController.GeneralDivision.AttachedDivision.RememberedDivisions);
    }
}
