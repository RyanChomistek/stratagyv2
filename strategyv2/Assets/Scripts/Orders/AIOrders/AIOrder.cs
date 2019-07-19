using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIOrder : MultiOrder
{
    private System.Action<List<ControlledDivision>> OnEnemiesSeenCallback;

    public AIOrder(Division controller, int commanderSendingOrderId, string name = "AI Order")
        : base(controller, commanderSendingOrderId, name, new List<Order>(), false)
    {
        IsBackgroundOrder = true;
    }

    public override void Start(ControlledDivision Host)
    {
        //generate zones
        //startup background orders to look for enemies/may just make this a controlled division thing with a callback
        Debug.Log("AI START");
        base.Start(Host);
        OnEnemiesSeenCallback = enemies => OnEnemiesSeen(Host, enemies);
        Host.RegisterOnEnemiesSeenCallback(OnEnemiesSeenCallback);
    }

    public override void OnEmptyOrder(ControlledDivision Host)
    {
        base.OnEmptyOrder(Host);

        //Move randomly
        Vector3 pos = Random.insideUnitCircle * 2;
        pos.z = 0;
        pos += Host.Position;
        StartOrder(Host, new Move(Host, Host.DivisionId, pos));
    }

    public void OnEnemiesSeen(ControlledDivision Host, List<ControlledDivision> enemies)
    {
        Debug.Log("ENEMY!!!!!!!!!!!!!!!");

        if(Division.PredictCombatResult(Host, enemies.ConvertAll(x => (Division) x)) > .5f)
        {
            Debug.Log("ATTACK");
            StartOrder(Host, new EngageOrder(Host,Host.DivisionId, enemies[0].DivisionId));
        }
        else
        {
            Debug.Log("RUN AWAY");
        }
        //descide whether to attack or not

        //attack if we can win and it doesnt put us out of position

        //run if we cant win also send messengers to nearby units to call for help
    }

    public virtual void GenerateZones(ControlledDivision Host)
    {
        //make zones for things

        //for commander units use zones to bound where they can move and stuff

        //maybe use zones to trigger behaviors, like have a close zone to trigger a defensive behavior
    }

    public override void End(ControlledDivision Host)
    {
        base.End(Host);
        Host.UnRegisterOnEnemiesSeenCallback(OnEnemiesSeenCallback);
        Debug.Log("AI END");
    }
}
