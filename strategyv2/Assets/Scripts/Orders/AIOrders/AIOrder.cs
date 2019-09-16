using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIOrder : MultiOrder
{
    private System.Action<List<ControlledDivision>> OnEnemiesSeenCallback;

    private string _currentBehavior = "None";

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

        //look at enemies and see if we can fight
        List<ControlledDivision> enemies = new List<ControlledDivision>();
        foreach (var kvp in Host.VisibleDivisions)
        {
            if (!kvp.Value.HasBeenDestroyed && !Division.AreSameTeam(kvp.Value, Host))
            {
                enemies.Add(kvp.Value);
            }
        }

        if (enemies.Count > 0)
        {
            OnEnemiesSeen(Host, enemies);
        }
        else if (Host.Soldiers.Count > Host.CommandingOfficer.SoldierCntSplitThreshold.Value)
        {
            _currentBehavior = "split";
            this.ReceiveOrder(Host, new SplitDivision(Host, Host.DivisionId, Host.CommandingOfficer.PercentOfSoldiersToSplit.Value));
        }
        else if (Host.Supply < Host.CommandingOfficer.ResupplyPerSoldierThreshold.Value * Host.NumSoldiers)
        {
            _currentBehavior = "Resupply";
            //resupply if we need to
            Resupply(Host);
        }
        else
        {
            _currentBehavior = "Recruit";
            //recruit if we have nothing better to do
            Recruit(Host);
        }
        
    }

    public void Resupply(ControlledDivision Host)
    {
        //find closest supply dump
        Func<TerrainMapTile, bool> findSupplyTile = tile => {
            return tile.Supply > 100;
        };

        //UnityEngine.Profiling.Profiler.BeginSample("find supply");

        //find closest supply
        if (Host.TryFindKnownTileMatchingPred(findSupplyTile, out Vector3 foundPosition))
        {
            //Debug.Log($"resupply position : {foundPosition}");
            this.ReceiveOrder(Host, new Move(Host, Host.DivisionId, foundPosition));
            var supplyOrder = new GatherSuppliesOrder(Host, Host.DivisionId);
            // add the supply to Host 
            Host.ReceiveOrder(supplyOrder);
            this.ReceiveOrder(Host, new WaitOrder(Host, Host.DivisionId, 2, x => {
                x.CancelOrder(supplyOrder.orderId);
            }));
        }
        else
        {
            RandomMove(Host);
        }

        //UnityEngine.Profiling.Profiler.EndSample();
    }

    public void Recruit(ControlledDivision Host)
    {
        Func<TerrainMapTile, bool> findPopTile = tile => {
            return tile.Population > 100;
        };

        //Debug.Log($"trying to recruit");
        //find closest populations
        if (Host.TryFindKnownTileMatchingPred(findPopTile, out Vector3 foundPosition))
        {
            //Debug.Log($"resupply position : {foundPosition}");
            this.ReceiveOrder(Host, new Move(Host, Host.DivisionId, foundPosition));
            // add the recruit to Host 
            var recruitOrder = new RecruitOrder(Host, Host.DivisionId);
            Host.ReceiveOrder(recruitOrder);
            this.ReceiveOrder(Host, new WaitOrder(Host, Host.DivisionId, 2, x => {
                x.CancelOrder(recruitOrder.orderId);
            }));
        }
        else
        {
            RandomMove(Host);
        }
    }

    public void RandomMove(ControlledDivision Host)
    {
        _currentBehavior = $"Random Move";
        Vector3 pos = UnityEngine.Random.insideUnitCircle * UnityEngine.Random.Range(2,10);
        pos.z = 0;
        pos += Host.Position;
        pos = MapManager.Instance.ClampPositionToInBounds(pos);
        StartOrder(Host, new Move(Host, Host.DivisionId, pos));
    }

    public void OnEnemiesSeen(ControlledDivision Host, List<ControlledDivision> enemies)
    {
        //Debug.Log("ENEMY!!!!!!!!!!!!!!!");

        if(Division.PredictCombatResult(Host, enemies.ConvertAll(x => (Division) x)) > Host.CommandingOfficer.EngagementThreshold.Value)
        {
            //Debug.Log("ATTACK");
            _currentBehavior = $"Attack: {enemies[0].DivisionId}";
            StartOrder(Host, new EngageOrder(Host,Host.DivisionId, enemies[0].DivisionId));
        }
        else
        {
            //Debug.Log("RUN AWAY");
            _currentBehavior = $"Running Away";
            Vector3 enemiesCentoid = Vector3.zero;

            foreach(var enemy in enemies)
            {
                enemiesCentoid += enemy.Position;
            }

            enemiesCentoid /= enemies.Count;

            Vector3 delta = (Host.Position - enemiesCentoid) * Host.CommandingOfficer.RunAwayDistance.Value;
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

    public override string ToString()
    {
        return _currentBehavior + "-" + base.ToString();
    }
}
