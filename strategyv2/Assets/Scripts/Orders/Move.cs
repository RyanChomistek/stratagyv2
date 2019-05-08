using Pathfinding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Move : Order {
    public Vector3 finish;
    private float _thresholdDistance;
    InputController.OnClick UICallback;

    public Move(Division controller, int commanderSendingOrderId, Vector3 finish, float thresholdDistance = .5f)
        : base(controller, commanderSendingOrderId, "Move")
    {
        this.finish = finish;
        this._thresholdDistance = thresholdDistance;
    }

    public override void Start(ControlledDivision Host)
    {
        //Debug.Log("move start " + Host.Name);
        MoveToTarget(Host);
        base.Start(Host);
    }

    public void MoveToTarget(ControlledDivision Host)
    {
        /*
        Vector3 currLoc = Host.Controller.transform.position;
        Vector3 dir = (finish - currLoc).normalized;
        Vector3 moveVec = dir * Host.Speed;
        //set start moving twords finish
        Host.Controller.GetComponent<Rigidbody>().velocity = moveVec * GameManager.Instance.GameSpeed;
        */
        IAstarAI ai = Host.Controller.GetComponent<IAstarAI>();
        ai.canMove = true;
        ai.destination = finish;
        Host.RecalculateAggrigateValues();
        ai.maxSpeed = Host.Speed;
    }

    public override void Pause(ControlledDivision Host)
    {
        //Host.Controller.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        IAstarAI ai = Host.Controller.GetComponent<IAstarAI>();
        ai.canMove = false;
    }

    public override void End(ControlledDivision Host)
    {
        //Debug.Log("move end");
        Host.Controller.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
    }

    public override void OnClickedInUI(Division Host, PlayerController playerController)
    {
        UICallback = mousePos => OnClickReturn(mousePos, Host, playerController);
        InputController.Instance.RegisterOnClickCallBack(UICallback);
    }

    public void OnClickReturn(Vector3 mousePos, Division Host, PlayerController playerController)
    {
        finish = new Vector3(mousePos.x, mousePos.y);
        InputController.Instance.UnregisterOnClickCallBack(UICallback);
        //clear ui
        OrderDisplayManager.instance.ClearOrders();
        //need to get 
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(playerController.GeneralDivision.AttachedDivision, CommanderSendingOrderId);
        CommanderSendingOrder.SendOrderTo(new RememberedDivision(Host), new Move(Host, CommanderSendingOrder.DivisionId, finish), ref playerController.GeneralDivision.AttachedDivision.RememberedDivisions);
    }

    public override void Proceed(ControlledDivision Host)
    {
        /*
        Vector3 currLoc = Host.Controller.transform.position;
        Vector3 dir = (finish - currLoc).normalized;
        Vector3 moveVec = dir * Host.Speed;
        //set start moving twords finish
        Host.Controller.GetComponent<Rigidbody>().velocity = moveVec * GameManager.Instance.GameSpeed;
        */
        MoveToTarget(Host);
    }

    public override bool TestIfFinished(ControlledDivision Host)
    {
        Vector3 currLoc = Host.Controller.transform.position;
        float distanceToFinish = (finish - currLoc).magnitude;
        if (distanceToFinish < _thresholdDistance)
        {
            return true;
        }
        return false;
    }

    public override Vector3 GetPredictedPosition(RememberedDivision rememberedDivision)
    {
        Vector3 lastKnownLoc = rememberedDivision.Position;
        
        Vector3 dir = (finish - lastKnownLoc).normalized;
        Vector3 distance = (finish - lastKnownLoc);

        float maxTime = distance.magnitude / rememberedDivision.Speed;
        var deltaTime = GameManager.Instance.GameTime - rememberedDivision.TimeStamp;

        if(deltaTime > maxTime)
        {
            return finish;
        }

        Vector3 predPosition = lastKnownLoc + dir * rememberedDivision.Speed * deltaTime;
        return predPosition;
    }

    public override string ToString()
    {
        return $"moving to {finish}";
    }
}
