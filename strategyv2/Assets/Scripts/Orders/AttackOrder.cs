using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackOrder : TargetingOrder
{
    public int RememberedTargetId;
    private bool IsFinished = false;

    LocalPlayerController.responseToUI UICallback;

    public AttackOrder(Division controller, int commanderSendingOrderId, int rememberedTargetId)
        : base(controller, commanderSendingOrderId, "Attack", rememberedTargetId)
    {
    }

    public override void Proceed(Division Host)
    {
        RememberedDivision RememberedTarget = GetRememberedDivisionFromHost(Host, RememberedTargetId);
        float distanceToTarget = (RememberedTarget.Position - Host.Controller.transform.position).magnitude;
        List<Soldier> soldiersWhoCanAttack = new List<Soldier>();
        foreach(Soldier soldier in Host.Soldiers)
        {
            if(distanceToTarget > soldier.MinRange && distanceToTarget < soldier.MaxRange)
            {
                soldiersWhoCanAttack.Add(soldier);
            }
        }

        float totalDamage = 0;
        
        foreach(Soldier soldier in soldiersWhoCanAttack)
        {
            totalDamage += soldier.HitStrength;
        }

        totalDamage *= GameManager.Instance.DeltaTime;

        bool isDestroyed = RememberedTarget.Controller.AttachedDivision.TakeDamage(totalDamage, Host);
        if(isDestroyed)
        {
            RememberedTarget.HasBeenDestroyed = true;
            RememberedTarget.TimeStamp = GameManager.Instance.GameTime;
            IsFinished = true;
        }
    }

    public override bool TestIfFinished(Division Host)
    {
        return IsFinished;
    }

    /*
    public override void RefreshRememberedDivisions(Dictionary<int, RememberedDivision> divisions)
    {
        RememberedTarget = divisions[RememberedTarget.DivisionId];
    }
    */

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

        var orders = new List<Order>() {
            new FindDivision(Host, CommanderSendingOrder.DivisionId, division.DivisionId),
            new AttackOrder(Host, CommanderSendingOrder.DivisionId, division.DivisionId)
        };

        CommanderSendingOrder.SendOrdersTo(new RememberedDivision(Host), orders);
    }
    
}
