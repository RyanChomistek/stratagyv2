using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SendMessage : TargetingOrder
{
    private bool _hasFoundTarget;
    private List<Order> _message;
    private ControlledDivision _visibleTarget;

    public SendMessage(Division controller, int commanderSendingOrderId, List<Order> message, int targetId)
        : base(controller, commanderSendingOrderId, "Send Message", targetId)
    {
        this._visibleTarget = null;
        this._message = message;
    }

    public override void Proceed(ControlledDivision Host)
    {
        if(Host.FindVisibleDivision(RememberedTargetId, out _visibleTarget))
        {
            _visibleTarget.ReceiveOrders(_message);
            _hasFoundTarget = true;
        }
    }

    public override bool TestIfFinished(ControlledDivision Host)
    {
        return _hasFoundTarget;
    }

    public override void End(ControlledDivision Host)
    {
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(Host, CommanderSendingOrderId);
        Host.Controller.GetComponent<Rigidbody>().velocity = Vector3.zero;
        RememberedDivision commander = Host.RememberedDivisions[CommanderSendingOrder.DivisionId];
        Host.ReceiveOrder(new FindDivision(Host, _visibleTarget.DivisionId, commander.DivisionId));
        Host.ReceiveOrder(new MergeDivisions(Host, commander.DivisionId, commander.DivisionId));
    }
}
