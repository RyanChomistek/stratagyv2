using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ControlledDivision : Division
{
    public Dictionary<int, RememberedDivision> RememberedDivisions = new Dictionary<int, RememberedDivision>();
    public Dictionary<int, ControlledDivision> VisibleDivisions = new Dictionary<int, ControlledDivision>();

    public Action<ControlledDivision> OnDiscoveredMapChanged;

    public Action<List<ControlledDivision>> OnEnemiesSeen;

    public override Vector3 Position { get { return Controller.transform.position; } }

    public ControlledDivision(Division division, DivisionController controller = null)
        : base(division, division.Controller)
    {
        SetupOrders();
        discoveredMapLocations = MapManager.Instance.GetMapMask();
    }

    public ControlledDivision(int teamId, DivisionController controller = null)
        : base(teamId, controller)
    {
        SetupOrders();
        discoveredMapLocations = MapManager.Instance.GetMapMask();
    }
    
    public void DestroyDivision(ControlledDivision other)
    {
        string str = "";
        foreach (var kvp in VisibleDivisions)
        {
            str += kvp.Key +", ";
        }

        Debug.Log($"Destorying division {other}, from {this}, notifying {str}");
        other.HasBeenDestroyed = true;
        FindAndRemoveSubordinateById(other.DivisionId, ref RememberedDivisions);
        RememberedDivisions[other.DivisionId] = new RememberedDivision(other);
        other.NotifyAllVisibleDivisionsOfDestruction();
        GameObject.Destroy(other.Controller.gameObject);
    }

    public void NotifyAllVisibleDivisionsOfDestruction()
    {
        foreach (var kvp in VisibleDivisions)
        {
            if(kvp.Value.RememberedDivisions.ContainsKey(DivisionId))
            {
                kvp.Value.RememberedDivisions[DivisionId].HasBeenDestroyed = true;
                kvp.Value.RememberedDivisions[DivisionId].TimeStamp = GameManager.Instance.GameTime;
            }
            else
            {
                kvp.Value.RememberedDivisions[DivisionId] = new RememberedDivision(this);
            }
        }
    }
    
    //change remembered version of self with recalculated values
    public override void RecalculateAggrigateValues()
    {
        base.RecalculateAggrigateValues();
        UpdateRememberedDivision(new RememberedDivision(this));
        OnChange();
    }

    public void AbsorbDivision(ControlledDivision other)
    {
        TransferSoldiers(other.Soldiers.ToList());
        DestroyDivision(other);
    }

    public bool TakeDamage(float damage, ControlledDivision from)
    {
        base.TakeDamage(damage, from);

        if (Soldiers.Count == 0)
        {
            from.DestroyDivision(this);
            return true;
        }

        RecalculateAggrigateValues();
        return false;
    }

    public bool CheckDamageDone(ControlledDivision from)
    {
        base.CheckDamageDone(from);

        if (Soldiers.Count == 0)
        {
            from.DestroyDivision(this);
            return true;
        }

        RecalculateAggrigateValues();
        return false;
    }

    public void RefreshDiscoveredTiles()
    {
        int sightDistance = Mathf.RoundToInt(MaxSightDistance);
        Vector2 controllerPosition = Controller.transform.position;
        Vector3Int controllerPositionRounded = new Vector3Int(MapManager.RoundVector(Controller.transform.position).x, MapManager.RoundVector(Controller.transform.position).y, 0);

        for (int x = -sightDistance - 1; x <= sightDistance + 1; x++)
        {
            for (int y = -sightDistance - 1; y <= sightDistance + 1; y++)
            {
                var position = new Vector3Int(x, y, 0) + controllerPositionRounded;
                var inVision = (new Vector2(position.x, position.y) - controllerPosition).magnitude < Controller.AttachedDivision.MaxSightDistance;

                if (inVision && MapManager.InBounds(discoveredMapLocations, position.x, position.y))
                {
                    discoveredMapLocations[position.x, position.y] = true;
                }
            }
        }
    }

    public void ShareMapInformation(ControlledDivision other)
    {
        for (int x = 0; x <= discoveredMapLocations.GetUpperBound(0); x++)
        {
            for (int y = 0; y <= discoveredMapLocations.GetUpperBound(1); y++)
            {
                bool info = discoveredMapLocations[x, y] | other.discoveredMapLocations[x, y];
                discoveredMapLocations[x, y] = info;
                other.discoveredMapLocations[x, y] = info;
            }
        }

        OnDiscoveredMapChanged?.Invoke(this);
        other.OnDiscoveredMapChanged?.Invoke(other);
    }

    public bool TryGetVisibleOrRememberedDivisionFromId(int divisionId, out Division division)
    {
        division = null;

        if (VisibleDivisions.TryGetValue(divisionId, out ControlledDivision controlledDivision))
        {
            division = controlledDivision;
            return true;
        }
        else if (RememberedDivisions.TryGetValue(divisionId, out RememberedDivision rememberedDivision))
        {
            division = rememberedDivision;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void RefreshVisibleDivisions(List<DivisionController> visibleControllers)
    {
        VisibleDivisions.Clear();
        List<ControlledDivision> enemies = new List<ControlledDivision>();
        foreach (var controller in visibleControllers)
        {
            VisibleDivisions.Add(controller.AttachedDivision.DivisionId, controller.AttachedDivision);
            FixCommanders(controller.AttachedDivision);
            if(!AreSameTeam(this, controller.AttachedDivision))
            {
                enemies.Add(controller.AttachedDivision);
            }
        }

        RefreshRememberedDivisionsFromVisibleDivisions();

        if(enemies.Count > 0)
        {
            OnEnemiesSeen?.Invoke(enemies);
        }
    }

    private void FixCommanders(ControlledDivision other)
    {
        //if they are on seperate command trees join them
        if(AreSameTeam(this,other) && this.Commander == this.DivisionId && other.Commander == other.DivisionId)
        {
            if(this.NumSoldiers > other.NumSoldiers)
            {
                other.Commander = this.DivisionId;
                this.AddSubordinate(new RememberedDivision(other));
            }
            else
            {
                this.Commander = other.DivisionId;
                other.AddSubordinate(new RememberedDivision(this));
            }
        }
    }

    public void RefreshRememberedDivisionsFromVisibleDivisions()
    {
        UpdateRememberedDivision(new RememberedDivision(this));

        foreach (var key in VisibleDivisions.Keys)
        {
            UpdateRememberedDivision(new RememberedDivision(VisibleDivisions[key]));

            //only share info with same team
            if (AreSameTeam(VisibleDivisions[key], this))
            {
                var rememberedDivisionKeys = VisibleDivisions[key].RememberedDivisions.Keys.ToList();
                //foreach (var kvp in VisibleDivisions[key].RememberedDivisions)
                for (int i = 0; i < rememberedDivisionKeys.Count; i++)
                {
                    int divisionId = rememberedDivisionKeys[i];
                    RememberedDivision otherDiv = VisibleDivisions[key].RememberedDivisions[divisionId];
                    UpdateRememberedDivision(otherDiv);
                }
            }
        }

        //RefreshSubordinates(RememberedDivisions);
        //RefreshOrdersRememberedDivisions(RememberedDivisions);
    }

    public void UpdateRememberedDivision(RememberedDivision otherDiv)
    {
        int divisionId = otherDiv.DivisionId;

        if (RememberedDivisions.ContainsKey(divisionId))
        {
            var ourDiv = RememberedDivisions[divisionId];
            var latestDiv = otherDiv.TimeStamp > ourDiv.TimeStamp ? otherDiv : ourDiv;
            RememberedDivisions[divisionId] = latestDiv;
        }
        else
        {
            RememberedDivisions.Add(divisionId, otherDiv);
        }

        if (divisionId == DivisionId)
        {
            return;
        }

        FixSubordinates(otherDiv, ref RememberedDivisions);
    }

    public bool FindVisibleDivision(int divisionID, out ControlledDivision division)
    {
        division = null;
        return VisibleDivisions.TryGetValue(divisionID, out division);
    }

    #region order stuff

    public override void SetupOrders()
    {
        //add possible orders
        PossibleOrders = new List<Order>();
        this.PossibleOrders.Add(new Move(this, -1, new Vector3()));
        this.PossibleOrders.Add(new SplitDivision(this, -1, null));
        this.PossibleOrders.Add(new ScoutOrder(this, -1, new Vector3()));
        this.PossibleOrders.Add(new HeartBeatOrder(this, -1, -1));
        this.PossibleOrders.Add(new EngageOrder(this, -1, -1));
        this.PossibleOrders.Add(new RecruitOrder(this, -1));
        this.PossibleOrders.Add(new GatherSuppliesOrder(this, -1));
        this.PossibleOrders.Add(new ZoneOrder(this, -1, null));
        this.PossibleOrders.Add(new PatrolZoneOrder(this, -1, null));
    }

    public void AddAutoRunBackgroundOrders()
    {
        //add background orders which should continously run
        RememberedDivision CommanderSendingOrder = new RememberedDivision(this);
        CommanderSendingOrder.SendOrderTo(
            new RememberedDivision(this),
            new UseSuppliesOrder(this, DivisionId), ref RememberedDivisions);
    }

    public void ReceiveOrder(Order order)
    {
        OrderSystem.ReceiveOrder(this, order);
    }

    public void ReceiveOrders(List<Order> orders)
    {
        foreach (Order order in orders)
        {
            ReceiveOrder(order);
        }

        OnChange();
    }

    public void CancelOrder(int orderId)
    {
        CancelOrders(new HashSet<int>() { orderId });
    }

    public void CancelOrders(HashSet<int> orderIdsToCancel)
    {
        OrderSystem.CancelOrders(this, orderIdsToCancel);
    }

    public void SendMessenger(RememberedDivision to, RememberedDivision endTarget, List<Order> orders)
    {
        //create a new division
        DivisionController messenger = CreateNewDivision((int) CommandingOfficer.MessengerDivisionSoldierCnt.Value);
        messenger.AttachedDivision.IsMessenger = true;
        messenger.name = "messenger";
        messenger.AttachedDivision.ReceiveOrder(new FindDivision(messenger.AttachedDivision, DivisionId, to.DivisionId));
        messenger.AttachedDivision.ReceiveOrder(new SendMessage(messenger.AttachedDivision, DivisionId, orders, to.DivisionId, endTarget.DivisionId));
    }

    public void SendMessengerToAllSubordinates(List<Order> orders, bool ignoreMessengers = true)
    {
        //copy so that when we make messengers they dont mess up the loop
        HashSet<int> subordinates_copy = new HashSet<int>(Subordinates);
        //send out messengers to all subordinates
        foreach (var subordinateId in subordinates_copy)
        {
            if (RememberedDivisions.TryGetValue(subordinateId, out RememberedDivision subordinate))
            {
                //if we are ignoring messengers skip
                if((ignoreMessengers && subordinate.IsMessenger) || subordinate.HasBeenDestroyed)
                {
                    continue;
                }

                SendMessenger(subordinate, subordinate, orders);
            }
        }
    }

    #endregion

    /// <summary>
    /// converts the zones map to a list
    /// </summary>
    /// <returns></returns>
    public List<IZone> ConvertZonesToList()
    {
        List<IZone> zonesList = new List<IZone>();
        foreach(var zone in Zones)
        {
            zonesList.Add(zone.Value);
        }

        return zonesList;
    }

    public override bool MergeZones(List<IZone> newZones)
    {
        bool changed = base.MergeZones(newZones);
        if(changed)
        {
            SendMessengerToAllSubordinates(new List<Order>() { new ShareZoneInfoOrder(this, this.DivisionId, ConvertZonesToList()) }, true );
        }

        Debug.Log($"{Name} zone size after sending {Zones.Count}");

        return changed;
    }

    public override void AddZone(IZone zone)
    {
        base.AddZone(zone);
        
        SendMessengerToAllSubordinates(new List<Order>() { new ShareZoneInfoOrder(this, this.DivisionId, ConvertZonesToList()) }, true);
        Debug.Log($"Adding zone {Name} {zone.Id} | {Zones.Count}");
    }

    public void RegisterOnEnemiesSeenCallback(Action<List<ControlledDivision>> callback)
    {
        OnEnemiesSeen += callback;
    }

    public void UnRegisterOnEnemiesSeenCallback(Action<List<ControlledDivision>> callback)
    {
        OnEnemiesSeen -= callback;
    }
}
