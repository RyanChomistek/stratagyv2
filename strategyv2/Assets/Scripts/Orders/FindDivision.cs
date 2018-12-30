using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindDivision : TargetingOrder
{
    bool HasFoundTarget;
    //holds the target division when it is in sight
    //private Division VisibleTarget;
    

    public FindDivision(Division controller, int commanderSendingOrderId, int rememberedTargetId, float thresholdDistance = .5f)
        : base(controller, commanderSendingOrderId, "find division", rememberedTargetId, thresholdDistance)
    {
        //this.VisibleTarget = null;
    }
    
    public override bool TestIfFinished(Division Host)
    {
        //find division does all of its work in end
        return true;
    }

    public override void End(Division Host)
    {
        RememberedDivision rememberedTarget;
        bool found = TryGetRememberedDivisionFromHost(Host, RememberedTargetId, out rememberedTarget);
        if (!found)
        {
            //we dont have a remembered reference yet, wait until we do
            return;
        }

        var destination = rememberedTarget.PredictedPosition;
        Division VisibleTarget;
        if(Host.FindVisibleDivision(rememberedTarget.DivisionId, out VisibleTarget))
        {
            destination = VisibleTarget.Controller.transform.position;
        }


    }
    
    
    /*
    public override void Proceed(Division Host)
    {
        Vector3 currLoc = Host.Controller.transform.position;
        RememberedDivision rememberedTarget;
        bool found = TryGetRememberedDivisionFromHost(Host, RememberedTargetId, out rememberedTarget);

        if(!found)
        {
            //we dont have a remembered reference yet, wait until we do
            return;
        }

        float distanceToFinish = (rememberedTarget.PredictedPosition - currLoc).magnitude;

        //if target is null look for it in the visible divisions
        if (VisibleTarget == null)
        {
            Host.FindVisibleDivision(rememberedTarget.DivisionId, out VisibleTarget);
        }

        //if it isnt null go find em
        if (VisibleTarget != null)
        {
            rememberedTarget = new RememberedDivision(VisibleTarget);
            distanceToFinish = (VisibleTarget.Controller.transform.position - currLoc).magnitude;
        }

        //when this is true then we have caught up to our target
        if (distanceToFinish < _thresholdDistance && !HasFoundTarget)
        {
            HasFoundTarget = true;
            VisibleTarget = null;
        }

        MoveToTarget(Host);
    }

    public override void Pause(Division Host)
    {
        Host.Controller.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
    }

    public void MoveToTarget(Division Host)
    {
        Vector3 currLoc = Host.Controller.transform.position;
        RememberedDivision RememberedTarget = GetRememberedDivisionFromHost(Host, RememberedTargetId);
        Vector3 dir = (RememberedTarget.PredictedPosition - currLoc).normalized;
        Vector3 moveVec = dir * Host.Speed * GameManager.Instance.GameSpeed;
        Host.Controller.GetComponent<Rigidbody>().velocity = moveVec;
    }

    public override bool TestIfFinished(Division Host)
    {
        return HasFoundTarget;
    }

    public override void End(Division Host)
    {
        Host.Controller.GetComponent<Rigidbody>().velocity = Vector3.zero;
    }
    */
}
