using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackOrder : TargetingOrder
{
    //public int RememberedTargetId;
    private bool IsFinished = false;

    LocalPlayerController.responseToUI UICallback;

    public AttackOrder(Division controller, int commanderSendingOrderId, int rememberedTargetId, float thresholdDistance = .5f)
        : base(controller, commanderSendingOrderId, "Attack", rememberedTargetId, thresholdDistance)
    {
    }

    public override void Proceed(ControlledDivision Host)
    {
        if (!IsFinished && Host.FindVisibleDivision(RememberedTargetId, out ControlledDivision division))
        {
            var distanceToTarget = (division.Controller.transform.position - Host.Controller.transform.position).magnitude;
            if(distanceToTarget > Host.MaxHitRange)
            {
                IsFinished = true;
                return;
            }

            float totalAttackerDamage = 0;
            for (int i = 0; i < Host.Soldiers.Count; i++)
            {
                Soldier soldier = Host.Soldiers[i];

                if (distanceToTarget > soldier.MinRange && distanceToTarget < soldier.MaxRange)
                {
                    var result = soldier.Attack(ref division, distanceToTarget);
                    totalAttackerDamage += result.DamageToDefender;
                }
            }

            
            bool isDestroyed = division.CheckDamageDone(Host);
            bool isHostDestroyed = Host.CheckDamageDone(division);
            if (isDestroyed)
            {
                RememberedDivision RememberedTarget = GetRememberedDivisionFromHost(Host, RememberedTargetId);
                RememberedTarget.HasBeenDestroyed = true;
                RememberedTarget.TimeStamp = GameManager.Instance.GameTime;
                IsFinished = true;
            }
        }
    }

    public override bool TestIfFinished(ControlledDivision Host)
    {
        return IsFinished;
    }

    public override void OnClickedInUI(Division Host, PlayerController playerController)
    {
        //InputController.Instance.RegisterOnClickCallBack(OnClickReturn);
        UICallback = division => OnUnitSelected(Host, division, playerController);
        LocalPlayerController.Instance.RegisterUnitSelectCallback(UICallback);
    }

    public void OnUnitSelected(Division Host, RememberedDivision division, PlayerController playerController)
    {
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(playerController.GeneralDivision.AttachedDivision, CommanderSendingOrderId);
        LocalPlayerController.Instance.UnRegisterUnitSelectCallback(UICallback);
        OrderDisplayManager.Instance.ClearOrders();

        var orders = new List<Order>() {
            new FindDivision(Host, CommanderSendingOrder.DivisionId, division.DivisionId),
            new AttackOrder(Host, CommanderSendingOrder.DivisionId, division.DivisionId, Host.MaxHitRange)
        };

        CommanderSendingOrder.SendOrdersTo(new RememberedDivision(Host), orders, ref playerController.GeneralDivision.AttachedDivision.RememberedDivisions);
    }
    
}
