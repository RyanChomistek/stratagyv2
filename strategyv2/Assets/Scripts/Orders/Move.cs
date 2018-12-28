﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Move : Order {
    public Vector3 finish;
    InputController.OnClick OnClickReturnDel;

    public Move(Division controller, RememberedDivision commanderSendingOrder, Vector3 finish)
    {
        this.CommanderSendingOrder = commanderSendingOrder;
        this.Host = controller;
        this.finish = finish;
        this.name = "Move";
        OnClickReturnDel = OnClickReturn;
    }

    public override void Start()
    {
        Debug.Log("move start " + Host.Name);
        MoveToTarget();
    }

    public void MoveToTarget()
    {
        Vector3 currLoc = Host.Controller.transform.position;
        Vector3 dir = (finish - currLoc).normalized;
        Vector3 moveVec = dir * Host.Speed;
        //set start moving twords finish
        Host.Controller.GetComponent<Rigidbody>().velocity = moveVec * GameManager.Instance.GameSpeed;
    }

    public override void Pause()
    {
        Host.Controller.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
    }

    public override void End()
    {
        Debug.Log("move end");
        Host.Controller.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
    }

    public override void OnClickedInUI()
    {
        InputController.Instance.RegisterOnClickCallBack(OnClickReturnDel);
    }

    public void OnClickReturn(Vector3 mousePos)
    {
        finish = new Vector3(mousePos.x, mousePos.y);
        InputController.Instance.UnregisterOnClickCallBack(OnClickReturnDel);
        //clear ui
        OrderDisplayManager.instance.ClearOrders();
        //need to get 

        CommanderSendingOrder.SendOrderTo(new RememberedDivision(Host), new Move(Host, CommanderSendingOrder, finish));
    }

    public override void Proceed()
    {
        Vector3 currLoc = Host.Controller.transform.position;
        Vector3 dir = (finish - currLoc).normalized;
        Vector3 moveVec = dir * Host.Speed;
        //set start moving twords finish
        Host.Controller.GetComponent<Rigidbody>().velocity = moveVec * GameManager.Instance.GameSpeed;
    }

    public override bool TestIfFinished()
    {
        Vector3 currLoc = Host.Controller.transform.position;
        float distanceToFinish = (finish - currLoc).magnitude;
        if (distanceToFinish < .5f)
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

        float maxTime = distance.magnitude / Host.Speed;
        var deltaTime = GameManager.Instance.GameTime - rememberedDivision.TimeStamp;

        if(deltaTime > maxTime)
        {
            return finish;
        }

        Vector3 predPosition = dir * Host.Speed * deltaTime;
        return predPosition;
    }

    public override string ToString()
    {
        return $"moving to {finish} at vel {Host.Controller.GetComponent<Rigidbody>().velocity}";
    }
}
