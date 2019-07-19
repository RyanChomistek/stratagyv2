using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIControlledDivision : ControlledDivision
{
    public AIControlledDivision(int teamId = -1, DivisionController controller = null)
        : base(teamId, controller)
    {
        ReceiveOrder(new AIOrder(this, DivisionId));
    }

    //just randomly moves for now
    //public override void OnEmptyOrder()
    //{
        /*
        //move to random spot
        var finish = Controller.transform.position + new Vector3(Random.value * 10 - 5, Random.value * 10 - 5);
        //need to get 
        RememberedDivision rememberedThis = new RememberedDivision(this);
        rememberedThis.SendOrderTo(rememberedThis, new Move(this, DivisionId, finish), ref RememberedDivisions);
        */
    //}
}
