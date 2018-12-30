using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Order
{
    public int HostId;
    public int CommanderSendingOrderId;
    public string name;

    public bool HasStarted;
    public bool IsBackgroundOrder;

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
        this.name = name;
    }

    public virtual void Start(Division Host) { HasStarted = true; }
    public virtual void Pause(Division Host) { }
    public virtual void End(Division Host) { }
    public virtual void OnClickedInUI(Division Host) { }
    public virtual void Proceed(Division Host) { }
    public virtual bool TestIfFinished(Division Host) { return false; }

    public virtual Vector3 GetPredictedPosition(RememberedDivision rememberedDivision)
    {
        var deltaTime = GameManager.Instance.GameTime - rememberedDivision.TimeStamp;
        return rememberedDivision.Position + rememberedDivision.Velocity * deltaTime;
    }

    protected RememberedDivision GetRememberedDivisionFromHost(Division Host, int id)
    {
        return Host.RememberedDivisions[id];
    }

    protected bool TryGetRememberedDivisionFromHost(Division Host, int id, out RememberedDivision rememberedDivision)
    {
        return Host.RememberedDivisions.TryGetValue(id, out rememberedDivision);
    }
    //public virtual void RefreshRememberedDivisions(Dictionary<int, RememberedDivision> divisions) { }
}
