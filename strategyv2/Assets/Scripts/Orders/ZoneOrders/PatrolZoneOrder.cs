﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolZoneOrder : ZoneOrder
{
    public PatrolZoneOrder(Division controller, int commanderSendingOrderId, Zone zone)
        : base(controller, commanderSendingOrderId, zone, "Patrol Zone ")
    {
    }

    protected override void StartNextOrder(ControlledDivision Host)
    {
        base.StartNextOrder(Host);

        //pick a random point in the zone 
        Vector3 nextPoint = AssignedZone.GetRandomPoint();
        //go there
        this.SubOrders.Add(new Move(Host, CommanderSendingOrderId, nextPoint, .5f));
    }

    public override void OnZoneSelected(Division Host, PlayerController playerController, Zone zone)
    {
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(playerController.GeneralDivision.AttachedDivision, CommanderSendingOrderId);
        LocalPlayerController.Instance.UnRegisterZoneSelectCallback(UICallback);
        OrderDisplayManager.Instance.ClearOrders();

        CommanderSendingOrder.SendOrderTo(new RememberedDivision(Host),
            new PatrolZoneOrder(Host, CommanderSendingOrder.DivisionId, zone), ref playerController.GeneralDivision.AttachedDivision.RememberedDivisions);
    }
}