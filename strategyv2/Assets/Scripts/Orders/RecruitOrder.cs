using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecruitOrder : Order
{
    private float _timeSinceLastRecruit;
    private float _timeBetweenRecruitTicks = 1;

    public RecruitOrder(Division controller, int commanderSendingOrderId) 
        : base(controller, commanderSendingOrderId, "Recruit")
    {
        this.IsBackgroundOrder = true;
    }

    public override void OnClickedInUI(Division Host, PlayerController playerController)
    {
        OrderDisplayManager.instance.ClearOrders();
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(playerController.GeneralDivision.AttachedDivision, CommanderSendingOrderId);
        //Debug.Log($"heartbeat {Host.DivisionId} {CommanderSendingOrderId}");
        CommanderSendingOrder.SendOrderTo(
            new RememberedDivision(Host),
            new RecruitOrder(Host, CommanderSendingOrderId), ref playerController.GeneralDivision.AttachedDivision.RememberedDivisions);
    }

    public override void Proceed(ControlledDivision Host)
    {
        
        _timeSinceLastRecruit += GameManager.Instance.DeltaTime;

        if(_timeSinceLastRecruit > _timeBetweenRecruitTicks)
        {
            Debug.Log("recruit");
            _timeSinceLastRecruit = 0;
            var tile = MapManager.Instance.GetTileFromPosition(Host.Controller.transform.position);
            if (tile.Population > 100)
            {
                for (int i = 0; i < 10; i++)
                {
                    Host.Soldiers.Add(new Soldier());
                }

                tile.Population -= 10;
            }
            
        }
    }
}
