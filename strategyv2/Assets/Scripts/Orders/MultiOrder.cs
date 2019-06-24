using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiOrder : Order
{
    protected List<Order> SubOrders;
    protected Order OngoingOrder;

    private string _baseName;

    public MultiOrder(Division controller, int commanderSendingOrderId, string name, List<Order> subOrders)
        : base(controller, commanderSendingOrderId, name)
    {
        if (subOrders != null)
            this.SubOrders = new List<Order>(subOrders);
        else
            this.SubOrders = new List<Order>();

        OngoingOrder = null;
        _baseName = name;
    }

    public override void Start(ControlledDivision Host)
    {
        base.Start(Host);
        StartNextOrder(Host);
    }

    protected virtual void StartNextOrder(ControlledDivision Host)
    {
        if (SubOrders.Count > 0)
        {
            OngoingOrder = SubOrders[0];
            SubOrders.RemoveAt(0);
            OngoingOrder.Start(Host);
            name = $"{_baseName} {OngoingOrder.name}";
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

    public override void Proceed(ControlledDivision Host)
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

    public override void Pause(ControlledDivision Host)
    {
        OngoingOrder?.Pause(Host);
    }

    public override void End(ControlledDivision Host)
    {
        OngoingOrder?.End(Host);
    }


    public override bool TestIfFinished(ControlledDivision Host)
    {
        //Debug.Log((SubOrders.Count == 0) +" "+ (OngoingOrder == null) + " " + name);
        return SubOrders.Count == 0 && OngoingOrder == null;
    }
}
