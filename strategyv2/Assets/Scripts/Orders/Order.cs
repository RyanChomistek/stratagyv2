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

    public virtual void OnClickedInUI(Division Host, PlayerController playerController) { }

    public virtual void Start(ControlledDivision Host) { HasStarted = true; }
    public virtual void Pause(ControlledDivision Host) { }
    public virtual void End(ControlledDivision Host) { HasFinished = true; }
    public virtual void Proceed(ControlledDivision Host) { }
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
}
