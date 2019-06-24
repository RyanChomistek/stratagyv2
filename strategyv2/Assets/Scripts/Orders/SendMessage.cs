using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SendMessage : TargetingOrder
{
    private bool _hasFoundTarget;
    private List<Order> _message;
    private ControlledDivision _visibleTarget;

    //this is the actuall target, we will set targetId to the commanders and go down the chain until we find this id
    private int _endTargetId;

    public SendMessage(Division controller, int commanderSendingOrderId, List<Order> message, int targetId, int endTargetId)
        : base(controller, commanderSendingOrderId, "Send Message", targetId)
    {
        this._visibleTarget = null;
        this._message = message;
        this._endTargetId = endTargetId;
    }

    public override void Proceed(ControlledDivision Host)
    {
        if(Host.FindVisibleDivision(RememberedTargetId, out _visibleTarget))
        {
            if (RememberedTargetId == _endTargetId)
            {
                _visibleTarget.ReceiveOrders(_message);
            }
            else
            {
                _visibleTarget.SendOrdersTo(GetRememberedDivisionFromHost(_visibleTarget, _endTargetId),
                _message, ref _visibleTarget.RememberedDivisions);
            }

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
        RememberedDivision commander = Host.RememberedDivisions[CommanderSendingOrder.DivisionId];
        Host.ReceiveOrder(new FindDivision(Host, _visibleTarget.DivisionId, commander.DivisionId));
        Host.ReceiveOrder(new MergeDivisions(Host, commander.DivisionId, commander.DivisionId));
    }
}
