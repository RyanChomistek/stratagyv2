using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackOrder : Order
{
    public RememberedDivision RememberedTarget;
    private bool IsFinished = false;
    public AttackOrder(Division controller, RememberedDivision commanderSendingOrder, RememberedDivision rememberedTarget)
    {
        this.CommanderSendingOrder = commanderSendingOrder;
        this.Host = controller;
        this.RememberedTarget = rememberedTarget;
        this.name = "Attack";
    }

    public override void Proceed()
    {
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

    public override bool TestIfFinished()
    {
        return IsFinished;//RememberedTarget.HasBeenDestroyed;
    }

    public override void RefreshRememberedDivisions(Dictionary<int, RememberedDivision> divisions)
    {
        RememberedTarget = divisions[RememberedTarget.DivisionId];
    }

    public override void OnClickedInUI()
    {
        //InputController.Instance.RegisterOnClickCallBack(OnClickReturn);
        LocalPlayerController.Instance.RegisterUnitSelectCallback(OnUnitSelected);
    }

    public void OnUnitSelected(RememberedDivision division)
    {
        Debug.Log(division);
        LocalPlayerController.Instance.UnRegisterUnitSelectCallback(OnUnitSelected);
        OrderDisplayManager.instance.ClearOrders();

        var orders = new List<Order>() {
            new FindDivision(Host, CommanderSendingOrder, division),
            new AttackOrder(Host, CommanderSendingOrder, division)
        };

        CommanderSendingOrder.SendOrdersTo(new RememberedDivision(Host), orders);
    }
    
}
