using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoutOrder : Order
{
    public Vector3 finish;

    public ScoutOrder(Division controller, RememberedDivision commanderSendingOrder, Vector3 finish)
    {
        this.CommanderSendingOrder = commanderSendingOrder;
        this.Host = controller;
        this.finish = finish;
        this.name = "Scout";
    }

    public override void OnClickedInUI()
    {
        InputController.Instance.RegisterOnClickCallBack(OnClickReturn);
    }

    public void OnClickReturn(Vector3 mousePos)
    {
        finish = new Vector3(mousePos.x, mousePos.y);
        InputController.Instance.UnregisterOnClickCallBack(OnClickReturn);
        OrderDisplayManager.instance.ClearOrders();
        var scout = Host.CreateNewDivision();
        scout.name = "scout";
        scout.AttachedDivision.ReceiveOrder(new Move(scout.AttachedDivision, CommanderSendingOrder, finish));
        scout.AttachedDivision.ReceiveOrder(new FindDivision(scout.AttachedDivision, CommanderSendingOrder, new RememberedDivision(Host)));
        scout.AttachedDivision.ReceiveOrder(new MergeDivisions(scout.AttachedDivision, new RememberedDivision(Host)));
    }
}
