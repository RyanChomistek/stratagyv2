using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngageOrder : MultiOrder
{
    private int RememberedTargetId;

    LocalPlayerController.UnitSelectDelegate UICallback;

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

    protected override void StartNextOrder(ControlledDivision Host)
    {
        base.StartNextOrder(Host);

        var rememberedTarget = GetRememberedDivisionFromHost(Host, RememberedTargetId);

        //if the target has been destroyed do nothing and to end the order
        if(rememberedTarget.HasBeenDestroyed)
        {
            OngoingOrder = null;
            return;
        }

        if (Host.FindVisibleDivision(RememberedTargetId, out ControlledDivision division))
        {
            var distanceToTarget = (division.Controller.transform.position - Host.Controller.transform.position).magnitude;

            //need to move closer
            if (distanceToTarget > Host.MaxHitRange)
            {
                Debug.Log(Host.MaxHitRange * .5f);
                this.SubOrders.Add(new Move(Host, CommanderSendingOrderId, division.Position, Host.MaxHitRange * .5f));
            }
            else
            {
                //if the target is within range attack
                this.SubOrders.Add(new AttackOrder(Host, CommanderSendingOrderId, division.DivisionId, Host.MaxHitRange));
            }
        }
        else
        {
            //else go find him again
            Debug.Log("not visible");
            AddFindTarget(Host);
        }
    }

    public override void OnClickedInUI(Division Host, PlayerController playerController)
    {
        UICallback = division => {
            //make sure we dont click the same unit
            if(division.Equals(Host))
            {
                return;
            }

            OnUnitSelected(Host, division, playerController);
            };
        LocalPlayerController.Instance.RegisterUnitSelectCallback(UICallback);
    }

    public void OnUnitSelected(Division Host, RememberedDivision division, PlayerController playerController)
    {
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(playerController.GeneralDivision.AttachedDivision, CommanderSendingOrderId);
        LocalPlayerController.Instance.UnRegisterUnitSelectCallback(UICallback);
        OrderDisplayManager.Instance.ClearOrders();

        CommanderSendingOrder.SendOrderTo(new RememberedDivision(Host), 
            new EngageOrder(Host, CommanderSendingOrder.DivisionId, division.DivisionId), ref playerController.GeneralDivision.AttachedDivision.RememberedDivisions);
    }
}


