﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        base.Start(Host);
        OnEnemiesSeenCallback = enemies => OnEnemiesSeen(Host, enemies);
        Host.RegisterOnEnemiesSeenCallback(OnEnemiesSeenCallback);
    }

    public override void OnEmptyOrder(ControlledDivision Host)
    {
        base.OnEmptyOrder(Host);

        //resupply if we need to
        if(Host.DivisionModifiers.ContainsKey(typeof(LowSupplyModifier)))
        {
            Resupply(Host);
        }
        else
        {
            //recruit if we have nothing better to do
            Recruit(Host);
        }
        
    }

    public void Resupply(ControlledDivision Host)
    {
        //find closest supply dump
        Func<MapTerrainTile, bool> findSupplyTile = tile => {
            return tile.Supply > 100;
        };

        //find closest supply
        if (Host.TryFindKnownTileMatchingPred(findSupplyTile, out Vector3 foundPosition))
        {
            //Debug.Log($"resupply position : {foundPosition}");
            this.ReceiveOrder(Host, new Move(Host, Host.DivisionId, foundPosition));
            // add the supply to Host 
            Host.ReceiveOrder(new GatherSuppliesOrder(Host, Host.DivisionId));
            this.ReceiveOrder(Host, new WaitOrder(Host, Host.DivisionId, 2));
        }
        else
        {
            RandomMove(Host);
        }
    }

    public void Recruit(ControlledDivision Host)
    {
        Func<MapTerrainTile, bool> findPopTile = tile => {
            return tile.Population > 100;
        };

        //Debug.Log($"trying to recruit");
        //find closest populations
        if (Host.TryFindKnownTileMatchingPred(findPopTile, out Vector3 foundPosition))
        {
            //Debug.Log($"resupply position : {foundPosition}");
            this.ReceiveOrder(Host, new Move(Host, Host.DivisionId, foundPosition));
            // add the recruit to Host 
            Host.ReceiveOrder(new RecruitOrder(Host, Host.DivisionId));
            this.ReceiveOrder(Host, new WaitOrder(Host, Host.DivisionId, 2));
        }
        else
        {
            RandomMove(Host);
        }
    }

    public void RandomMove(ControlledDivision Host)
    {
        Vector3 pos = UnityEngine.Random.insideUnitCircle * UnityEngine.Random.Range(2,10);
        pos.z = 0;
        pos += Host.Position;
        pos = MapManager.Instance.ClampPositionToInBounds(pos);
        StartOrder(Host, new Move(Host, Host.DivisionId, pos));
    }

    public void OnEnemiesSeen(ControlledDivision Host, List<ControlledDivision> enemies)
    {
        //Debug.Log("ENEMY!!!!!!!!!!!!!!!");

        if(Division.PredictCombatResult(Host, enemies.ConvertAll(x => (Division) x)) > Host.CommandingOfficer.EngagementThreshold)
        {
            //Debug.Log("ATTACK");
            StartOrder(Host, new EngageOrder(Host,Host.DivisionId, enemies[0].DivisionId));
        }
        else
        {
            //Debug.Log("RUN AWAY");
            Vector3 enemiesCentoid = Vector3.zero;

            foreach(var enemy in enemies)
            {
                enemiesCentoid += enemy.Position;
            }

            enemiesCentoid /= enemies.Count;

            Vector3 delta = (Host.Position - enemiesCentoid) * Host.CommandingOfficer.RunAwayDistance;
            Vector3 retreatPos = Host.Position + delta;
            StartOrder(Host, new Move(Host, Host.DivisionId, MapManager.Instance.ClampPositionToInBounds(retreatPos)));
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
