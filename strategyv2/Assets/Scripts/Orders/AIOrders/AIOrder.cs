using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIOrder : MultiOrder
{
    public AIOrder(Division controller, int commanderSendingOrderId, string name = "AI Order")
        : base(controller, commanderSendingOrderId, name, new List<Order>(), false)
    {
    }

    public override void Start(ControlledDivision Host)
    {
        //generate zones
        //startup background orders to look for enemies/may just make this a controlled division thing with a callback
        base.Start(Host);
    }

    public void OnEnemySeen(ControlledDivision Host, ControlledDivision enemy)
    {
        //descide whether to attack or not

        //attack if we can win and it doesnt put us out of position

        //run if we cant win also send messengers to nearby units to call for help
    }

    public virtual void GenerateZones(ControlledDivision Host)
    {
        //make zones for things

        //for commander units use zones to bound where they can move and stuff

        //maybe use zones to trigger behaviors, like have a close zone to trigger a defensive behavior
    }
}
