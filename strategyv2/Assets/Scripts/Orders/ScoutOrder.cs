using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoutOrder : Order
{
    public Vector3 finish;
    private InputController.OnClick UICallback;

    public ScoutOrder(Division controller, int commanderSendingOrderId, Vector3 finish)
        : base(controller, commanderSendingOrderId, "Scout")
    {
        this.finish = finish;
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
        OrderDisplayManager.instance.ClearOrders();
        var scout = Host.CreateNewDivision();
        scout.name = "scout";

        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(playerController.GeneralDivision.AttachedDivision, CommanderSendingOrderId);

        scout.AttachedDivision.ReceiveOrder(new Move(scout.AttachedDivision, CommanderSendingOrder.DivisionId, finish));
        scout.AttachedDivision.ReceiveOrder(new FindDivision(scout.AttachedDivision, CommanderSendingOrder.DivisionId, Host.DivisionId));
        scout.AttachedDivision.ReceiveOrder(new MergeDivisions(scout.AttachedDivision, Host.DivisionId, Host.DivisionId));
    }
}
