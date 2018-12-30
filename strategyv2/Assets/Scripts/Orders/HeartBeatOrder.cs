using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartBeatOrder : TargetingOrder
{
    private float SecondsPerHeartbeat;
    private float TimeSinceLastHeartbeat;

    public HeartBeatOrder(Division controller, int commanderSendingOrderId, int rememberedTargetId, float secondsPerHeartbeat = 1f)
        : base(controller, commanderSendingOrderId, "heart beat", rememberedTargetId)
    {
        this.SecondsPerHeartbeat = secondsPerHeartbeat;
        this.IsBackgroundOrder = true;
    }

    public override void Proceed(Division Host)
    {
        base.Proceed(Host);
        TimeSinceLastHeartbeat += GameManager.Instance.DeltaTime;
        if(TimeSinceLastHeartbeat > SecondsPerHeartbeat)
        {
            SendHeartBeat(Host);
            TimeSinceLastHeartbeat = 0;
        }
    }

    private void SendHeartBeat(Division Host)
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
                    RememberedTargetId)
                });
        }
    }

    public override bool TestIfFinished(Division Host)
    {
        return false;
    }

    public override void OnClickedInUI(Division Host)
    {
        OrderDisplayManager.instance.ClearOrders();
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(Host, CommanderSendingOrderId);
        //Debug.Log($"heartbeat {Host.DivisionId} {CommanderSendingOrderId}");
        CommanderSendingOrder.SendOrderTo(
            new RememberedDivision(Host),
            new HeartBeatOrder(Host, CommanderSendingOrderId, CommanderSendingOrderId));
    }
}
