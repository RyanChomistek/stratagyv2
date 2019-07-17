using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Order
{
    private static int OrderIdCnt = 0;

    public int HostId;
    public int CommanderSendingOrderId;
    public string name;
    public int orderId;
    public bool HasStarted;
    public bool IsBackgroundOrder;
    public bool IsCancelable;
    public bool Canceled;
    public bool HasFinished;

    public Order(Division controller, int commanderSendingOrderId, string name)
    {
        this.CommanderSendingOrderId = commanderSendingOrderId;

        if(controller != null)
        {
            this.HostId = controller.DivisionId;
        }
        else
        {
            this.HostId = -1;
        }
        
        this.HasStarted = false;
        this.IsBackgroundOrder = false;
        this.IsCancelable = true;
        this.name = name;
        this.orderId = OrderIdCnt++;
        this.HasFinished = false;
        this.Canceled = false;
    }

    /// <summary>
    /// this function is called whenever the order's button is clicked on a divisions context menu
    /// </summary>
    public virtual void OnClickedInUI(Division Host, PlayerController playerController) { }

    /// <summary>
    /// when an order starts executing the start will be called
    /// </summary>
    public virtual void Start(ControlledDivision Host) { HasStarted = true; }

    /// <summary>
    /// when the game is paused this will be called
    /// </summary>
    public virtual void Pause(ControlledDivision Host) { }

    /// <summary>
    /// End is called when the order is exited either through a cancel or a normal finish state
    /// </summary>
    public virtual void End(ControlledDivision Host) { HasFinished = true; }

    /// <summary>
    /// proceed is called every frame after the order starts
    /// </summary>
    public virtual void Proceed(ControlledDivision Host) { }

    /// <summary>
    /// if this returns true the order will end
    /// </summary>
    public virtual bool TestIfFinished(ControlledDivision Host) { return false; }

    public virtual Vector3 GetPredictedPosition(RememberedDivision rememberedDivision)
    {
        var deltaTime = GameManager.Instance.GameTime - rememberedDivision.TimeStamp;
        return rememberedDivision.Position + rememberedDivision.Velocity * deltaTime;
    }

    protected RememberedDivision GetRememberedDivisionFromHost(ControlledDivision Host, int id)
    {
        return Host.RememberedDivisions[id];
    }

    protected bool TryGetRememberedDivisionFromHost(ControlledDivision Host, int id, out RememberedDivision rememberedDivision)
    {
        return Host.RememberedDivisions.TryGetValue(id, out rememberedDivision);
    }

    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        Order objAsOrder = obj as Order;
        if (objAsOrder == null) return false;
        else return Equals(objAsOrder);
    }

    public bool Equals(Order other)
    {
        return this.orderId == other.orderId;
    }

    public override int GetHashCode()
    {
        return this.orderId;
    }

    public override string ToString()
    {
        return $"{name} ";
    }
}
