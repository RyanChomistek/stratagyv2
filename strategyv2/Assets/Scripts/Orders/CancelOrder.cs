using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CancelOrder : Order
{
    public HashSet<int> OrderIdsToCancel;
    public bool FinishedCanceling;

    public CancelOrder(Division controller, int commanderSendingOrderId, HashSet<int> orderIdsToCancel)
        : base(controller, commanderSendingOrderId, "Cancel")
    {
        OrderIdsToCancel = orderIdsToCancel;
        FinishedCanceling = false;
        IsBackgroundOrder = true;
    }

    public override void Start(ControlledDivision Host)
    {
        FinishedCanceling = true;
        Host.CancelOrders(OrderIdsToCancel);
    }
    

    public override bool TestIfFinished(ControlledDivision Host)
    {
        return FinishedCanceling;
    }
}
