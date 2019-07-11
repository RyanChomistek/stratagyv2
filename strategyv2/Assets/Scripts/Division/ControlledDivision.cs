using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ControlledDivision : Division
{
    public Dictionary<int, RememberedDivision> RememberedDivisions = new Dictionary<int, RememberedDivision>();
    public Dictionary<int, ControlledDivision> VisibleDivisions = new Dictionary<int, ControlledDivision>();

    public bool[,] discoveredMapLocations;

    public Action<ControlledDivision> OnDiscoveredMapChanged;

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
        //Debug.Log($"Destorying division {other}, from {this}");
        other.HasBeenDestroyed = true;
        FindAndRemoveSubordinateById(other.DivisionId, ref RememberedDivisions);
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
        for (int i = 0; i < Soldiers.Count; i++)
        {
            if (damage == 0)
            {
                break;
            }

            var soldier = Soldiers[i];
            float damageToSoldier = Mathf.Min(damage, soldier.Health);
            soldier.Health -= damageToSoldier;
            damage -= damageToSoldier;

            if (soldier.Health <= 0)
            {
                Soldiers.RemoveAt(i);
                i--;
            }
        }

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
        for (int i = 0; i < Soldiers.Count; i++)
        {
            var soldier = Soldiers[i];
            if (soldier.Health <= 0)
            {
                Soldiers.RemoveAt(i);
                i--;
            }
        }

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
        foreach (var controller in visibleControllers)
        {
            VisibleDivisions.Add(controller.AttachedDivision.DivisionId, controller.AttachedDivision);
            FixCommanders(controller.AttachedDivision);
        }

        RefreshRememberedDivisionsFromVisibleDivisions();
    }

    private void FixCommanders(ControlledDivision other)
    {
        //if they are on seperate command trees join them
        if(this.Commander == this.DivisionId && other.Commander == other.DivisionId)
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
            if (VisibleDivisions[key].Controller.Controller.TeamId == Controller.Controller.TeamId)
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
    }

    public void AddAutoRunBackgroundOrders()
    {
        //add background orders which should continously run
        RememberedDivision CommanderSendingOrder = new RememberedDivision(this);
        CommanderSendingOrder.SendOrderTo(
            new RememberedDivision(this),
            new UseSuppliesOrder(this, DivisionId), ref RememberedDivisions);
    }

    public void DoOrders()
    {
        if(OngoingOrder.Canceled)
        {
            OngoingOrder.End(this);
            TryStartNextOrder();
        }
        else if (GameManager.Instance.IsPaused)
        {
            OngoingOrder.Pause(this);
        }
        else if (OngoingOrder.GetType() != typeof(EmptyOrder))
        {
            //if we are finished stop
            if (OngoingOrder.TestIfFinished(this))
            {
                OngoingOrder.End(this);
                OngoingOrder = new EmptyOrder();
                OnChange();
            }
            else
            {
                ContinueOrder();
            }
        }
        //grab a new order
        else if (TryStartNextOrder())
        {}
    }

    private bool TryStartNextOrder()
    {
        if (OrderQueue.Count > 0)
        {
            OngoingOrder = OrderQueue[0];
            OrderQueue.RemoveAt(0);
            OngoingOrder.Start(this);
            ContinueOrder();
            OnChange();
            return true;
        }

        if(!(OngoingOrder is EmptyOrder))
            OngoingOrder = new EmptyOrder();

        OnEmptyOrder();
        return false;
    }

    //in a normal controlled division this will do nothing, but the ai controller will override
    virtual protected void OnEmptyOrder()
    {
    }

    public void DoBackgroundOrders()
    {
        if (GameManager.Instance.IsPaused)
        {
            BackgroundOrderList.ForEach(x => x.Pause(this));
        }
        else
        {
            for (int i = 0; i < BackgroundOrderList.Count; i++)
            {
                var order = BackgroundOrderList[i];

                if(order.Canceled)
                {
                    order.End(this);
                    BackgroundOrderList.RemoveAt(i);
                    i--;
                    OnChange();
                }

                if (!order.HasStarted)
                {
                    order.Start(this);
                    OnChange();
                }

                order.Proceed(this);

                if (order.TestIfFinished(this))
                {
                    order.End(this);
                    BackgroundOrderList.RemoveAt(i);
                    i--;
                    OnChange();
                }
            }
        }
    }

    private void ContinueOrder()
    {
        OngoingOrder.Proceed(this);
    }

    public void ReceiveOrder(Order order)
    {
        if (order.IsBackgroundOrder)
        {
            BackgroundOrderList.Add(order);
        }
        else
        {
            OrderQueue.Add(order);
        }

        OnChange();
    }

    public void ReceiveOrders(List<Order> orders)
    {
        foreach (Order order in orders)
        {
            ReceiveOrder(order);
        }

        OnChange();
    }
    
    private void CancelOrder(Order order, HashSet<int> orderIdsToCancel)
    {
        if (order.IsCancelable && orderIdsToCancel.Contains(order.orderId))
        {
            order.Canceled = true;
        }
    }

    private void CancelOrders(List<Order> orders, HashSet<int> orderIdsToCancel)
    {
        for (int i = 0; i < orders.Count; i++)
        {
            var order = BackgroundOrderList[i];
            CancelOrder(order, orderIdsToCancel);
        }
    }

    public void CancelOrders(HashSet<int> orderIdsToCancel)
    {
        CancelOrders(OrderQueue, orderIdsToCancel);
        CancelOrders(BackgroundOrderList, orderIdsToCancel);
        CancelOrder(OngoingOrder, orderIdsToCancel);
        DoOrders();
    }

    public void SendMessenger(RememberedDivision to, RememberedDivision endTarget, List<Order> orders)
    {
        //create a new division
        DivisionController messenger = CreateNewDivision();
        messenger.name = "messenger";
        //todo discover location of to
        messenger.AttachedDivision.ReceiveOrder(new FindDivision(messenger.AttachedDivision, DivisionId, to.DivisionId));
        messenger.AttachedDivision.ReceiveOrder(new SendMessage(messenger.AttachedDivision, DivisionId, orders, to.DivisionId, endTarget.DivisionId));
    }

    #endregion
}
