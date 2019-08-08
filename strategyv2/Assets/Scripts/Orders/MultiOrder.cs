using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiOrder : Order
{
    public List<Order> OrderQueue;
    public List<Order> BackgroundOrderList = new List<Order>();
    public Order OngoingOrder;
    private bool EndOnEmptyOrder;
    private string _baseName;

    public MultiOrder(Division controller, int commanderSendingOrderId, string name, List<Order> subOrders, bool endOnEmptyOrder = true)
        : base(controller, commanderSendingOrderId, name)
    {
        if (subOrders != null)
            this.OrderQueue = new List<Order>(subOrders);
        else
            this.OrderQueue = new List<Order>();

        OngoingOrder = null;
        _baseName = name;
        EndOnEmptyOrder = endOnEmptyOrder;
    }

    public override void Start(ControlledDivision Host)
    {
        base.Start(Host);
        TryStartNextOrder(Host);
    }

    public override void Proceed(ControlledDivision Host)
    {
        DoOrders(Host);
        DoBackgroundOrders(Host);
    }

    public virtual void ReceiveOrder(ControlledDivision Host, Order order)
    {
        if (order.IsBackgroundOrder)
        {
            //make sure theres no duplicates of this type of order
            foreach(Order backgroundOrder in BackgroundOrderList)
            {
                if(order.GetType() == backgroundOrder.GetType())
                {
                    return;
                }
            }
            var orderType = order.GetType();
            BackgroundOrderList.Add(order);
        }
        else
        {
            OrderQueue.Add(order);
        }

        Host.OnChange();
    }

    public virtual void DoOrders(ControlledDivision Host)
    {
        if (OngoingOrder.Canceled)
        {
            OngoingOrder.End(Host);
            TryStartNextOrder(Host);
        }
        else if (GameManager.Instance.IsPaused)
        {
            OngoingOrder.Pause(Host);
        }
        else if (OngoingOrder.GetType() != typeof(EmptyOrder))
        {
            //if we are finished stop
            if (OngoingOrder.TestIfFinished(Host))
            {
                OngoingOrder.End(Host);
                OngoingOrder = new EmptyOrder();
                Host.OnChange();
            }
            else
            {
                ContinueOrder(Host);
            }
        }
        //grab a new order
        else if (TryStartNextOrder(Host))
        { }
    }

    protected virtual void ContinueOrder(ControlledDivision Host)
    {
        OngoingOrder.Proceed(Host);
    }

    /// <summary>
    /// starts a new order
    /// </summary>
    /// <param name="Host"></param>
    /// <param name="order"></param>
    protected virtual void StartOrder(ControlledDivision Host, Order order)
    {
        OngoingOrder = order;
        OngoingOrder.Start(Host);
        //DOES not use continueOrder to proceed the order, this is because sub classes of multi use the continue order to start new orders and can cause infinite recursion
        OngoingOrder.Proceed(Host);
        Host.OnChange();
        name = $"{_baseName} {OngoingOrder.name}";
    }

    protected virtual bool TryStartNextOrder(ControlledDivision Host)
    {
        if (OrderQueue.Count > 0)
        {
            StartOrder(Host, OrderQueue[0]);
            OrderQueue.RemoveAt(0);
            return true;
        }

        if (!(OngoingOrder is EmptyOrder))
            OngoingOrder = new EmptyOrder();

        OnEmptyOrder(Host);

        return false;
    }

    //in a normal controlled division this will do nothing, but the ai controller will override
    public virtual void OnEmptyOrder(ControlledDivision Host)
    {
        if (EndOnEmptyOrder)
        {
            Canceled = true;
        }
    }

    public virtual void DoBackgroundOrders(ControlledDivision Host)
    {
        if (GameManager.Instance.IsPaused)
        {
            BackgroundOrderList.ForEach(x => x.Pause(Host));
        }
        else
        {
            for (int i = 0; i < BackgroundOrderList.Count; i++)
            {
                var order = BackgroundOrderList[i];

                if (order.Canceled)
                {
                    order.End(Host);
                    BackgroundOrderList.RemoveAt(i);
                    i--;
                    Host.OnChange();
                }

                if (!order.HasStarted)
                {
                    order.Start(Host);
                    Host.OnChange();
                }

                order.Proceed(Host);

                if (order.TestIfFinished(Host))
                {
                    order.End(Host);
                    BackgroundOrderList.RemoveAt(i);
                    i--;
                    Host.OnChange();
                }
            }
        }
    }

    private void CancelOrder(Order order, HashSet<int> orderIdsToCancel)
    {
        if (order.IsCancelable && orderIdsToCancel.Contains(order.orderId))
        {
            order.Canceled = true;
        }
    }

    private void CancelOrders(List<Order> orders, HashSet<int> orderIdsToCancel)
    {
        for (int i = 0; i < orders.Count; i++)
        {
            var order = BackgroundOrderList[i];
            CancelOrder(order, orderIdsToCancel);
        }
    }

    public void CancelOrders(ControlledDivision Host, HashSet<int> orderIdsToCancel)
    {
        CancelOrders(OrderQueue, orderIdsToCancel);
        CancelOrders(BackgroundOrderList, orderIdsToCancel);
        CancelOrder(OngoingOrder, orderIdsToCancel);
        //DoOrders(Host);
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
        base.End(Host);
    }
    
    public override bool TestIfFinished(ControlledDivision Host)
    {
        //Debug.Log((SubOrders.Count == 0) +" "+ (OngoingOrder == null) + " " + name);
        return OrderQueue.Count == 0 && OngoingOrder == null;
    }

    public override string ToString()
    {
        string str = "orders : ";
        str += OngoingOrder?.ToString() + " :: ";
        foreach (Order order in OrderQueue)
        {
            str += order.ToString() + " | ";
        }

        str += "||";

        foreach (Order order in BackgroundOrderList)
        {
            str += order.ToString() + " | ";
        }

        return base.ToString() + $"<{str}>";
    }
}
