using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FindDivision : Order
{
    bool HasFoundTarget;
    //holds the target division when it is in sight
    public Division VisibleTarget;
    public RememberedDivision RememberedTarget;

    private float _thresholdDistance = .5f;

    public FindDivision(Division controller, RememberedDivision commanderSendingOrder, RememberedDivision rememberedTarget, float thresholdDistance = .5f)
    {
        this.CommanderSendingOrder = commanderSendingOrder;
        this.Host = controller;
        this.RememberedTarget = rememberedTarget;
        this.VisibleTarget = null;
        this._thresholdDistance = thresholdDistance;
    }

    public override void Proceed()
    {
        Vector3 currLoc = Host.Controller.transform.position;
        float distanceToFinish = (RememberedTarget.PredictedPosition - currLoc).magnitude;

        //if target is null look for it in the visible divisions
        if (VisibleTarget == null)
        {
            Host.FindVisibleDivision(RememberedTarget.DivisionId, out VisibleTarget);
        }

        //if it isnt null go find em
        if (VisibleTarget != null)
        {
            RememberedTarget = new RememberedDivision(VisibleTarget);
            distanceToFinish = (VisibleTarget.Controller.transform.position - currLoc).magnitude;
        }

        //when this is true then we have caught up to our target
        if (distanceToFinish < _thresholdDistance && !HasFoundTarget)
        {
            HasFoundTarget = true;
            VisibleTarget = null;
        }

        MoveToTarget();
    }

    public void MoveToTarget()
    {
        Vector3 currLoc = Host.Controller.transform.position;
        Vector3 dir = (RememberedTarget.PredictedPosition - currLoc).normalized;
        Vector3 moveVec = dir * Host.Speed;
        Host.Controller.GetComponent<Rigidbody>().velocity = moveVec;
    }

    public override bool TestIfFinished()
    {
        return HasFoundTarget;
    }

    public override void End()
    {
        Host.Controller.GetComponent<Rigidbody>().velocity = Vector3.zero;
    }
}
