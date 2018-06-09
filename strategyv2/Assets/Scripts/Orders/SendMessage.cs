using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SendMessage : Order {
    bool hasFoundTarget;
    List<Order> message;
    Division VisibleTarget;
    public RememberedDivision RememberedTarget;

    public SendMessage(Division controller, RememberedDivision commanderSendingOrder, List<Order> message, RememberedDivision target)
    {
        this.CommanderSendingOrder = commanderSendingOrder;
        this.Host = controller;
        this.RememberedTarget = target;
        this.VisibleTarget = null;
        this.message = message;
    }

    public override void Proceed()
    {
        if(Host.FindVisibleDivision(RememberedTarget.DivisionId, out VisibleTarget))
        {
            VisibleTarget.ReceiveOrders(message);
            hasFoundTarget = true;
        }
    }

    public override bool TestIfFinished()
    {
        return hasFoundTarget;
    }

    public override void End()
    {
        Host.Controller.GetComponent<Rigidbody>().velocity = Vector3.zero;
        RememberedDivision commander = Host.RememberedDivisions[CommanderSendingOrder.DivisionId];
        Host.ReceiveOrder(new FindDivision(Host, new RememberedDivision(VisibleTarget), commander));
        Host.ReceiveOrder(new MergeDivisions(Host, commander));
    }
}
