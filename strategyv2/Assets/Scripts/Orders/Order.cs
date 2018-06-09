using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Order
{
    public Division Host;
    public RememberedDivision CommanderSendingOrder;
    public string name;
    public virtual void Start() { }
    public virtual void Pause() { }
    public virtual void End() { }
    public virtual void OnClickedInUI() { }
    public virtual void Proceed() { }
    public virtual bool TestIfFinished() { return false; }
}
