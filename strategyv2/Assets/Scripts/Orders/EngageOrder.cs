using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngageOrder : MultiOrder
{
    private int RememberedTargetId;

    LocalPlayerController.responseToUI UICallback;

    public EngageOrder(Division controller, int commanderSendingOrderId, int rememberedTargetId)
        : base(controller, commanderSendingOrderId, "Engage", new List<Order>())
    {
        this.RememberedTargetId = rememberedTargetId;

        AddFindTarget(controller);
    }

    private void AddFindTarget(Division Host)
    {
        this.SubOrders.Add(new FindDivision(Host, CommanderSendingOrderId, RememberedTargetId, Host.MaxSightDistance));
    }

    protected override void StartNextOrder(Division Host)
    {
        base.StartNextOrder(Host);

        if(Host.FindVisibleDivision(RememberedTargetId, out Division division))
        {
            //if the target is within range attack
            Debug.Log("visible");
            Debug.Log(SubOrders.Count);
        }
        else
        {
            //else go find him again
            Debug.Log("not visible");
            AddFindTarget(Host);
        }
    }

    public override void OnClickedInUI(Division Host)
    {
        //InputController.Instance.RegisterOnClickCallBack(OnClickReturn);
        UICallback = division => OnUnitSelected(Host, division);
        LocalPlayerController.Instance.RegisterUnitSelectCallback(UICallback);
    }

    public void OnUnitSelected(Division Host, RememberedDivision division)
    {
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(Host, CommanderSendingOrderId);
        LocalPlayerController.Instance.UnRegisterUnitSelectCallback(UICallback);
        OrderDisplayManager.instance.ClearOrders();
        /*
        var orders = new List<Order>() {
            new FindDivision(Host, CommanderSendingOrder.DivisionId, division.DivisionId),
            new AttackOrder(Host, CommanderSendingOrder.DivisionId, division.DivisionId)
        };

        CommanderSendingOrder.SendOrdersTo(new RememberedDivision(Host), orders);
        */
        CommanderSendingOrder.SendOrderTo(new RememberedDivision(Host), 
            new EngageOrder(Host, CommanderSendingOrder.DivisionId, division.DivisionId));
    }
}


