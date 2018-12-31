using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiOrder : Order
{
    protected List<Order> SubOrders;
    protected Order OngoingOrder;

    public MultiOrder(Division controller, int commanderSendingOrderId, string name, List<Order> subOrders)
        : base(controller, commanderSendingOrderId, name)
    {
        if (subOrders != null)
            this.SubOrders = new List<Order>(subOrders);
        else
            this.SubOrders = new List<Order>();

        OngoingOrder = null;
    }

    public override void Start(Division Host)
    {
        base.Start(Host);
        StartNextOrder(Host);
    }

    protected virtual void StartNextOrder(Division Host)
    {
        if (SubOrders.Count > 0)
        {
            OngoingOrder = SubOrders[0];
            SubOrders.RemoveAt(0);
            OngoingOrder.Start(Host);
        }
        /*
        else
        {
            if(OngoingOrder != null)
            {
                OngoingOrder.End(Host);
            }

            
        }
        */
    }

    public override void Proceed(Division Host)
    {
        OngoingOrder.Proceed(Host);
        if(OngoingOrder.TestIfFinished(Host))
        {
            OngoingOrder.End(Host);
            OngoingOrder = null;
            StartNextOrder(Host);
        }
    }

    public override Vector3 GetPredictedPosition(RememberedDivision rememberedDivision)
    {
        if(OngoingOrder == null)
        {
            return base.GetPredictedPosition(rememberedDivision);
        }

        return OngoingOrder.GetPredictedPosition(rememberedDivision);
    }

    public override void Pause(Division Host)
    {
        OngoingOrder?.Pause(Host);
    }

    public override void End(Division Host)
    {
        OngoingOrder?.End(Host);
    }


    public override bool TestIfFinished(Division Host)
    {
        //Debug.Log((SubOrders.Count == 0) +" "+ (OngoingOrder == null) + " " + name);
        return SubOrders.Count == 0 && OngoingOrder == null;
    }
}
