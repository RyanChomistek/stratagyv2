using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Division {
    protected static int DivisionCounter = 0;
    public int DivisionId;
    public string Name;
    [HideInInspector]
    public RememberedDivision Commander;
    public List<Soldier> Soldiers;
    public Dictionary<int, RememberedDivision> Subordinates = new Dictionary<int, RememberedDivision>();

    public List<Order> OrderQueue;
    public List<Order> PossibleOrders;
    public Order OngoingOrder = null;

    public float Speed = 10;
    public float MaxSightDistance = 0;

    public Dictionary<int, Division> VisibleDivisions = new Dictionary<int, Division>();
    //public List<Division> DebugVisibleDivisions = new List<Division>();
    public Dictionary<int, RememberedDivision> RememberedDivisions = new Dictionary<int, RememberedDivision>();
    //public List<RememberedDivision> DebugRememberedDivisions = new List<RememberedDivision>();
    public DivisionController Controller;

    public delegate void RefreshDelegate(Division division);
    private RefreshDelegate refresh;
    //Dictionary<int,RefreshDelegate> OnChangeRefresh = new Dictionary<int, RefreshDelegate>();

    #region constructors

    public Division(RememberedDivision commander, List<Order> orders,
    List<Soldier> soldiers, List<Order> possibleOrders, Dictionary<int, RememberedDivision> subordinates,
    Dictionary<int, RememberedDivision> rememberedDivisions,
    DivisionController controller = null)
    {
        this.Commander = commander;

        this.PossibleOrders = new List<Order>(possibleOrders);
        this.Subordinates = new Dictionary<int, RememberedDivision>(subordinates);
        this.OrderQueue = new List<Order>(orders);
        this.Soldiers = new List<Soldier>(soldiers);
        
        this.RememberedDivisions = new Dictionary<int, RememberedDivision> (rememberedDivisions);

        this.DivisionId = DivisionCounter;
        DivisionCounter++;

        this.Name = "Division " + DivisionId;
        this.Controller = controller;
        this.OngoingOrder = new EmptyOrder();
        SetupOrders();
    }

    public Division(Division division, DivisionController controller = null)
    {
        this.Commander = division.Commander;

        this.PossibleOrders = new List<Order>(division.PossibleOrders);
        this.Subordinates = new Dictionary<int, RememberedDivision>(division.Subordinates);
        this.OrderQueue = new List<Order>();
        this.Soldiers = new List<Soldier>(division.Soldiers);
        
        this.RememberedDivisions = new Dictionary<int, RememberedDivision>(division.RememberedDivisions);

        this.DivisionId = division.DivisionId;

        this.Name = "Division " + DivisionId;
        this.Controller = controller;
        this.OngoingOrder = division.OngoingOrder;
        SetupOrders();
        RecalculateAggrigateValues();
    }

    //use for creating a new division from inside a new controller
    public Division(DivisionController controller = null)
    {
        this.Commander = null;

        this.PossibleOrders = new List<Order>();
        this.Subordinates = new Dictionary<int, RememberedDivision>();
        this.OrderQueue = new List<Order>();
        this.Soldiers = new List<Soldier>();
        
        this.RememberedDivisions = new Dictionary<int, RememberedDivision>();

        this.DivisionId = DivisionCounter;
        DivisionCounter++;

        this.Name = "Division " + DivisionId;
        this.Controller = controller;
        this.OngoingOrder = new EmptyOrder();
        SetupOrders();
    }

    #endregion

    #region order stuff

    public virtual void SetupOrders()
    {
        this.PossibleOrders.Add(new Move(this, null, new Vector3()));
        this.PossibleOrders.Add(new SplitDivision(this, null));
    }

    public virtual void DoOrders()
    {
        if (GameManager.Instance.IsPaused)
        {
            OngoingOrder.Pause();
        }
        else if (OngoingOrder.GetType() != typeof(EmptyOrder))
        {
            //if we are finished stop
            if (OngoingOrder.TestIfFinished())
            {
                OngoingOrder.End();
                OngoingOrder = new EmptyOrder();
                OnChange();
            }
            else
            {
                ContinueOrder();
            }
        }
        //grab a new order
        else if (OrderQueue.Count > 0)
        {
            OngoingOrder = OrderQueue[0];
            OrderQueue.RemoveAt(0);
            OngoingOrder.Start();
            ContinueOrder();
            OnChange();
        }
    }

    public void AddRefreshDelegate(RefreshDelegate refreshDelegate)
    {
        refresh += refreshDelegate;
        OnChange();
    }

    public void OnChange()
    {
        if(refresh != null)
        {
            refresh(this);
        }
    }

    private void ContinueOrder()
    {
        OngoingOrder.Proceed();
    }

    public void ReceiveOrder(Order order)
    {
        OrderQueue.Add(order);
        OnChange();
    }

    public void ReceiveOrders(List<Order> orders)
    {
        foreach (Order order in orders)
        {
            OrderQueue.Add(order);
        }
        OnChange();
    }

    #endregion

    #region soldier transfer

    public void TransferSoldiers(List<Soldier> troops)
    {
        Soldiers.AddRange(troops);
        troops.Clear();
        OnChange();
    }

    public void AddSubordinate(RememberedDivision division)
    {
        Subordinates.Add(division.DivisionId, division);
        OnChange();
    }

    public void RemoveSubordinate(RememberedDivision division)
    {
        Subordinates.Remove(division.DivisionId);
        OnChange();
    }

    public void FindAndRemoveSubordinateById(int divisionId)
    {
        if(Subordinates.ContainsKey(divisionId))
        {
            Division other = Subordinates[divisionId];
            //promote the guys under him
            foreach (var subordinate in other.Subordinates)
            {
                subordinate.Value.Commander = new RememberedDivision(this);
                AddSubordinate(subordinate.Value);
                subordinate.Value.OnChange();
            }

            Subordinates.Remove(divisionId);
        }
        else
        {
            foreach(var sub in Subordinates)
            {
                sub.Value.FindAndRemoveSubordinateById(divisionId);
            }
        }
    }

    public void AbsorbDivision(Division other)
    {
        TransferSoldiers(other.Soldiers);
        FindAndRemoveSubordinateById(other.DivisionId);
        //find other in my subordinates
        /*
        //kick him out of his commanders subordinate list
        RememberedDivision parent = other.Commander;

        if (parent != null)
        {
            parent.RemoveSubordinate(new RememberedDivision(other));
        }
        */

        RememberedDivisions[other.DivisionId].HasBeenDestroyed = true;
        GameObject.Destroy(other.Controller.gameObject);
        OnChange();
    }

    public Dictionary<SoldierType, List<Soldier>> SplitSoldiersIntoTypes()
    {
        Dictionary<SoldierType, List<Soldier>> soldiersSplit = new Dictionary<SoldierType, List<Soldier>>();

        foreach (Soldier soldier in Soldiers)
        {
            List<Soldier> soldiersInThisType;
            if (soldiersSplit.TryGetValue(soldier.Type, out soldiersInThisType))
            {
                soldiersInThisType.Add(soldier);
            }
            else
            {
                soldiersInThisType = new List<Soldier>();
                soldiersInThisType.Add(soldier);
                soldiersSplit.Add(soldier.Type, soldiersInThisType);
            }
        }

        return soldiersSplit;
    }

    public virtual Division CreateChild(List<Soldier> soldiersForChild, DivisionController newController)
    {
        newController.transform.position = this.Controller.transform.position;
        newController.transform.rotation = this.Controller.transform.rotation;

        Division newDivision = new Division(newController)
        {
            Soldiers = soldiersForChild
        };

        AddSubordinate(new RememberedDivision(newDivision));
        newDivision.Commander = new RememberedDivision(this);
        OnChange();
        newDivision.OnChange();
        newController.AttachedDivision = newDivision;
        return newDivision;
    }

    #endregion

    public void RecalculateAggrigateValues()
    {
        MaxSightDistance = 0;
        int cnt = 0;
        foreach(Soldier soldier in Soldiers)
        {
            MaxSightDistance += soldier.SightDistance;
            cnt++;
        }

        MaxSightDistance = MaxSightDistance / cnt;
    }

    public void RefreshVisibleDivisions(List<DivisionController> visibleControllers)
    {
        VisibleDivisions.Clear();
        foreach(var controller in visibleControllers)
        {
            VisibleDivisions.Add(controller.AttachedDivision.DivisionId, controller.AttachedDivision);
        }
    }

    public void RefreshRememberedDivisionsFromVisibleDivisions()
    {
        foreach(var key in VisibleDivisions.Keys)
        {
            RememberedDivisions[key] = new RememberedDivision(VisibleDivisions[key]);
            foreach(var kvp in RememberedDivisions[key].RememberedDivisions)
            {
                int divisionId = kvp.Key;
                RememberedDivision otherDiv = kvp.Value;

                if(RememberedDivisions.ContainsKey(divisionId))
                {
                    var currentDiv = RememberedDivisions[divisionId];
                    var latestDiv = otherDiv.TimeStamp > currentDiv.TimeStamp ? otherDiv : currentDiv;
                    RememberedDivisions[divisionId] = latestDiv;
                }
            }
        }

        RefreshSubordinates(RememberedDivisions);
        //DebugRememberedDivisions = RememberedDivisions.Values.ToList();
        //DebugVisibleDivisions = VisibleDivisions.Values.ToList();
    }

    public void RefreshSubordinates(Dictionary<int, RememberedDivision> divisions)
    {
        var divisionIds = Subordinates.Keys.ToList();
        for (int i = 0; i < divisionIds.Count; i++)
        {
            int divisionId = divisionIds[i];
            if(divisions.ContainsKey(divisionId))
            {
                Subordinates[divisionId] = divisions[divisionId];
            }
            else
            {
                divisions.Add(divisionId, Subordinates[divisionId]);
            }

            Subordinates[divisionId].RefreshSubordinates(divisions);
        }
    }

    public bool FindVisibleDivision(int divisionID, out Division division)
    {
        division = null;
        return VisibleDivisions.TryGetValue(divisionID, out division);
    }

    public List<RememberedDivision> FindDivisionInSubordinates(RememberedDivision start, RememberedDivision end, List<RememberedDivision> prev_)
    {
        List<RememberedDivision> prev = new List<RememberedDivision>(prev_);
        //RememberedDivision rememberedStart = start.GenerateRememberedDivision();
        prev.Add(start);
        if (start.DivisionId == end.DivisionId)
        {
            return prev;
        }

        foreach (var divisionKVP in start.Subordinates)
        {
            RememberedDivision division = divisionKVP.Value;
            List<RememberedDivision> temp = FindDivisionInSubordinates(division, end, prev);
            if (temp != null)
            {
                return temp;
            }
        }

        return null;
    }

    protected Soldier PopSoldier()
    {
        Soldier soldier = Soldiers[Soldiers.Count - 1];
        Soldiers.Remove(soldier);
        
        return soldier;
    }

    //ONLY CALL ON DIVISIONS NOT REMEMBERED DIVISOIONS
    public void SendMessenger(RememberedDivision to, Order order)
    {
        //create a new division
        //todo make the messenger descesion smart
        List<Soldier> soldiersToGive = new List<Soldier>();
        soldiersToGive.Add(PopSoldier());
        DivisionController messenger = Controller.CreateChild(soldiersToGive);

        //give it a move order to go to the division
        List<Order> orders = new List<Order>();
        orders.Add(order);
        //todo discover location of to
        messenger.AttachedDivision.ReceiveOrder(new FindDivision(messenger.AttachedDivision, new RememberedDivision(this), to));
        messenger.AttachedDivision.ReceiveOrder(new SendMessage(messenger.AttachedDivision, new RememberedDivision(this), orders, to));
        //messenger.ReceiveOrder(new FindDivision(messenger, this, messenger));
        // messenger.ReceiveOrder
    }

    public override string ToString()
    {
        return $"({DivisionId}, {Controller})";
    }
}
