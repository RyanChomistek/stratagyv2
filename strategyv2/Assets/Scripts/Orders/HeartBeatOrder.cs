using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartBeatOrder : TargetingOrder
{
    private float SecondsPerHeartbeat;
    private float TimeSinceLastHeartbeat;

    public HeartBeatOrder(Division controller, int commanderSendingOrderId, int rememberedTargetId, float secondsPerHeartbeat = 2f)
        : base(controller, commanderSendingOrderId, "heart beat", rememberedTargetId)
    {
        this.SecondsPerHeartbeat = secondsPerHeartbeat;
        this.IsBackgroundOrder = true;
    }

    public override void Proceed(ControlledDivision Host)
    {
        base.Proceed(Host);
        TimeSinceLastHeartbeat += GameManager.Instance.DeltaTime;
        if(TimeSinceLastHeartbeat > SecondsPerHeartbeat)
        {
            SendHeartBeat(Host);
            TimeSinceLastHeartbeat = 0;
        }
    }

    private void SendHeartBeat(ControlledDivision Host)
    {
        DivisionController HeartBeatMessenger;
        if (Host.TryCreateNewDivision(out HeartBeatMessenger))
        {
            HeartBeatMessenger.name = "Heatbeat messenger";

            RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(Host, CommanderSendingOrderId);
            Debug.Log($" heartbeat {Host.DivisionId} {RememberedTargetId}");
            HeartBeatMessenger.AttachedDivision.ReceiveOrders(
                new List<Order>()
                {
                new FindDivision(
                    HeartBeatMessenger.AttachedDivision,
                    Host.DivisionId,
                    RememberedTargetId),
                new SendMessage(
                    HeartBeatMessenger.AttachedDivision,
                    Host.DivisionId,
                    new List<Order>(),
                    RememberedTargetId,
                    Host.DivisionId)
                });
        }
    }

    public override bool TestIfFinished(ControlledDivision Host)
    {
        return false;
    }

    public override void OnClickedInUI(Division Host, PlayerController playerController)
    {
        OrderDisplayManager.instance.ClearOrders();
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(playerController.GeneralDivision.AttachedDivision, CommanderSendingOrderId);
        //Debug.Log($"heartbeat {Host.DivisionId} {CommanderSendingOrderId}");
        CommanderSendingOrder.SendOrderTo(
            new RememberedDivision(Host),
            new HeartBeatOrder(Host, CommanderSendingOrderId, CommanderSendingOrderId), ref playerController.GeneralDivision.AttachedDivision.RememberedDivisions);
    }
}
