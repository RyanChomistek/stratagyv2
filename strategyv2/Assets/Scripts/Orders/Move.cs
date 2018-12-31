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

    public override void Start(Division Host)
    {
        //Debug.Log("move start " + Host.Name);
        MoveToTarget(Host);
        base.Start(Host);
    }

    public void MoveToTarget(Division Host)
    {
        Vector3 currLoc = Host.Controller.transform.position;
        Vector3 dir = (finish - currLoc).normalized;
        Vector3 moveVec = dir * Host.Speed;
        //set start moving twords finish
        Host.Controller.GetComponent<Rigidbody>().velocity = moveVec * GameManager.Instance.GameSpeed;
    }

    public override void Pause(Division Host)
    {
        Host.Controller.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
    }

    public override void End(Division Host)
    {
        Debug.Log("move end");
        Host.Controller.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
    }

    public override void OnClickedInUI(Division Host)
    {
        UICallback = mousePos => OnClickReturn(mousePos, Host);
        InputController.Instance.RegisterOnClickCallBack(UICallback);
    }

    public void OnClickReturn(Vector3 mousePos, Division Host)
    {
        finish = new Vector3(mousePos.x, mousePos.y);
        InputController.Instance.UnregisterOnClickCallBack(UICallback);
        //clear ui
        OrderDisplayManager.instance.ClearOrders();
        //need to get 
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(Host, CommanderSendingOrderId);
        CommanderSendingOrder.SendOrderTo(new RememberedDivision(Host), new Move(Host, CommanderSendingOrder.DivisionId, finish));
    }

    public override void Proceed(Division Host)
    {
        Vector3 currLoc = Host.Controller.transform.position;
        Vector3 dir = (finish - currLoc).normalized;
        Vector3 moveVec = dir * Host.Speed;
        //set start moving twords finish
        Host.Controller.GetComponent<Rigidbody>().velocity = moveVec * GameManager.Instance.GameSpeed;
    }

    public override bool TestIfFinished(Division Host)
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
