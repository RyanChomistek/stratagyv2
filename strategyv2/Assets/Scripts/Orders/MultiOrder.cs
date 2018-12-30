using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiOrder : Order
{
    private List<Order> SubOrders;
    private Order OngoingOrder;

    public MultiOrder(Division controller, int commanderSendingOrderId, string name, List<Order> subOrders)
        : base(controller, commanderSendingOrderId, name)
    {
        SubOrders = new List<Order>(SubOrders);
        OngoingOrder = null;
    }

    public override void Start(Division Host)
    {
        base.Start(Host);
        StartNextOrder(Host);
    }

    private void StartNextOrder(Division Host)
    {
        if (SubOrders.Count > 0)
        {
            OngoingOrder = SubOrders[0];
            SubOrders.RemoveAt(0);
            OngoingOrder.Start(Host);
        }
    }

    public override void Proceed(Division Host)
    {
        OngoingOrder.Proceed(Host);
        if(OngoingOrder.TestIfFinished(Host))
        {
            OngoingOrder.End(Host);
            StartNextOrder(Host);
        }
    }

    public override Vector3 GetPredictedPosition(RememberedDivision rememberedDivision)
    {
        return OngoingOrder.GetPredictedPosition(rememberedDivision);
    }

    public override void Pause(Division Host)
    {
        OngoingOrder.Pause(Host);
    }

    public override bool TestIfFinished(Division Host)
    {
        return SubOrders.Count == 0 && OngoingOrder == null;
    }
}
