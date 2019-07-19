using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICommanderOrder : AIOrder
{
    public AICommanderOrder(Division controller, int commanderSendingOrderId, string name) 
        : base(controller, commanderSendingOrderId, name)
    {
    }

    public override void Start(ControlledDivision Host)
    {
        //look for enemies with some units
        //recruit with others

        //once enemies found generate zones to set up defencive paremeter, and allocate troops to front line
        //also generate zones for potential advancement or retreat
        base.Start(Host);
    }
}
