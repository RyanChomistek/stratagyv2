using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecruitOrder : TickingOrder
{
    public RecruitOrder(Division controller, int commanderSendingOrderId) 
        : base(controller, commanderSendingOrderId, "Recruit", 1)
    {
        this.IsBackgroundOrder = true;
    }

    public override void OnClickedInUI(Division Host, PlayerController playerController)
    {
        OrderDisplayManager.Instance.ClearOrders();
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(playerController.GeneralDivision.AttachedDivision, CommanderSendingOrderId);
        //Debug.Log($"heartbeat {Host.DivisionId} {CommanderSendingOrderId}");
        CommanderSendingOrder.SendOrderTo(
            new RememberedDivision(Host),
            new RecruitOrder(Host, CommanderSendingOrderId), ref playerController.GeneralDivision.AttachedDivision.RememberedDivisions);
    }

    public override void OnTick(ControlledDivision Host)
    {
        var tile = MapManager.Instance.GetTileFromPosition(Host.Controller.transform.position);

        int popThreshold = 50;

        if (tile.Population > popThreshold)
        {
            for (int i = 0; i < 10; i++)
            {
                Host.Soldiers.Add(new Soldier());
            }

            tile.Population -= popThreshold;

            var tile2 = MapManager.Instance.GetTileFromPosition(Host.Controller.transform.position);
        }
    }
}
