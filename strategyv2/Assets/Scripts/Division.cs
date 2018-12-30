using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InsufficientSoldierCountException : System.Exception{
}

[System.Serializable]
public class Division {
    protected static int DivisionCounter = 0;
    public int DivisionId;
    public string Name;
    public int Commander;
    public List<Soldier> Soldiers;
    public HashSet<int> Subordinates = new HashSet<int>();
    
    public List<Order> OrderQueue;
    public List<Order> BackgroundOrderList;
    public List<Order> PossibleOrders;
    public Order OngoingOrder = null;

    public float Speed = 10;
    public float MaxSightDistance = 0;
    public float TotalHealth = 0;
    public int NumSoldiers = 0;
    public float DamageOutput = 0;

    public Dictionary<int, Division> VisibleDivisions = new Dictionary<int, Division>();
    public Dictionary<int, RememberedDivision> RememberedDivisions = new Dictionary<int, RememberedDivision>();
    public DivisionController Controller;

    public delegate void RefreshDelegate(Division division);
    private RefreshDelegate refresh;

    #region constructors
    public Division(Division division, DivisionController controller = null)
    {
        this.Commander = division.Commander;

        this.PossibleOrders = new List<Order>(division.PossibleOrders);
        this.Subordinates = new HashSet<int>(division.Subordinates);

        this.OrderQueue = new List<Order>();
        this.BackgroundOrderList = new List<Order>();
        this.OngoingOrder = division.OngoingOrder;

        this.Soldiers = new List<Soldier>(division.Soldiers);
        
        this.RememberedDivisions = new Dictionary<int, RememberedDivision>(division.RememberedDivisions);

        this.DivisionId = division.DivisionId;

        this.Name = "Division " + DivisionId;
        this.Controller = controller;
        SetupOrders();
        RecalculateAggrigateValues();
    }

    //use for creating a new division from inside a new controller
    public Division(DivisionController controller = null)
    {
        this.Commander = -1;

        this.PossibleOrders = new List<Order>();
        this.Subordinates = new HashSet<int>();
        this.OrderQueue = new List<Order>();
        this.BackgroundOrderList = new List<Order>();
        this.Soldiers = new List<Soldier>();

        this.RememberedDivisions = new Dictionary<int, RememberedDivision>();
        SetupOrders();

        Init(controller);
    }

    public void Init(DivisionController controller = null)
    {
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
        PossibleOrders = new List<Order>();
        this.PossibleOrders.Add(new Move(this, -1, new Vector3()));
        this.PossibleOrders.Add(new SplitDivision(this, -1, null));
        this.PossibleOrders.Add(new ScoutOrder(this, -1, new Vector3()));
        this.PossibleOrders.Add(new AttackOrder(this, -1, -1));
        this.PossibleOrders.Add(new HeartBeatOrder(this, -1, -1));
    }

    public virtual void DoOrders()
    {
        if (GameManager.Instance.IsPaused)
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
        else if (OrderQueue.Count > 0)
        {
            OngoingOrder = OrderQueue[0];
            OrderQueue.RemoveAt(0);
            OngoingOrder.Start(this);
            ContinueOrder();
            OnChange();
        }
    }

    public void DoBackgroundOrders()
    {
        if (GameManager.Instance.IsPaused)
        {
            BackgroundOrderList.ForEach(x => x.Pause(this));
        }
        else
        {
            for(int i = 0; i < BackgroundOrderList.Count; i++)
            {
                var order = BackgroundOrderList[i];
                if(!order.HasStarted)
                {
                    order.Start(this);
                }

                order.Proceed(this);

                if (order.TestIfFinished(this))
                {
                    order.End(this);
                    BackgroundOrderList.RemoveAt(i);
                    i--;
                }
            }

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
        refresh?.Invoke(this);
    }

    private void ContinueOrder()
    {
        OngoingOrder.Proceed(this);
    }

    public void ReceiveOrder(Order order)
    {
        if(order.IsBackgroundOrder)
        {
            BackgroundOrderList.Add(order);
            Debug.Log(BackgroundOrderList.Count);
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
        Subordinates.Add(division.DivisionId);
        OnChange();
    }

    public void RemoveSubordinate(RememberedDivision division)
    {
        Subordinates.Remove(division.DivisionId);
        OnChange();
    }

    public void FindAndRemoveSubordinateById(int divisionId)
    {
        if(Subordinates.Contains(divisionId))
        {
            Division other = RememberedDivisions[divisionId];
            //promote the guys under him
            foreach (var subordinateId in other.Subordinates)
            {
                var subordinate = RememberedDivisions[subordinateId];
                subordinate.Commander = divisionId;
                AddSubordinate(subordinate);
                subordinate.OnChange();
            }

            Subordinates.Remove(divisionId);
        }
        else
        {
            foreach(var subordinateId in Subordinates)
            {
                var subordinate = RememberedDivisions[subordinateId];
                subordinate.FindAndRemoveSubordinateById(divisionId);
            }
        }
    }

    public void DestroyDivision(Division other)
    {
        FindAndRemoveSubordinateById(other.DivisionId);
        other.NotifyAllVisibleDivisionsOfDestruction();
        GameObject.Destroy(other.Controller.gameObject);
    }

    public void NotifyAllVisibleDivisionsOfDestruction()
    {
        foreach(var kvp in VisibleDivisions)
        {
            kvp.Value.RememberedDivisions[DivisionId].HasBeenDestroyed = true;
            kvp.Value.RememberedDivisions[DivisionId].TimeStamp = GameManager.Instance.GameTime;
        }
    }

    public void AbsorbDivision(Division other)
    {
        TransferSoldiers(other.Soldiers);
        DestroyDivision(other);
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
        newDivision.Commander = DivisionId;
        OnChange();
        newDivision.OnChange();
        newController.AttachedDivision = newDivision;
        return newDivision;
    }

    #endregion

    public bool TakeDamage(float damage, Division from)
    {
        for(int i = 0; i < Soldiers.Count; i++)
        {
            if(damage == 0)
            {
                break;
            }

            var soldier = Soldiers[i];
            float damageToSoldier = Mathf.Min(damage,soldier.Health);
            soldier.Health -= damageToSoldier;
            damage -= damageToSoldier;

            if(soldier.Health <= 0)
            {
                Soldiers.RemoveAt(i);
                i--;
            }
        }

        if(Soldiers.Count == 0)
        {
            from.DestroyDivision(this);
            return true;
        }

        RecalculateAggrigateValues();
        return false;
    }

    public void RecalculateAggrigateValues()
    {
        MaxSightDistance = 0;
        Speed = 0;
        TotalHealth = 0;
        DamageOutput = 0;
        int cnt = 0;
        foreach(Soldier soldier in Soldiers)
        {
            MaxSightDistance += soldier.SightDistance;
            Speed += soldier.Speed;
            TotalHealth += soldier.Health;
            DamageOutput += soldier.HitStrength;
            cnt++;
        }

        MaxSightDistance /= cnt;
        Speed /= cnt;
        NumSoldiers = cnt;
        OnChange();
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
        UpdateRememberedDivision(new RememberedDivision(this));

        foreach (var key in VisibleDivisions.Keys)
        {
            UpdateRememberedDivision(new RememberedDivision(VisibleDivisions[key]));

            //only share info with same team
            if(VisibleDivisions[key].Controller.Controller.TeamId == Controller.Controller.TeamId)
            {
                foreach (var kvp in VisibleDivisions[key].RememberedDivisions)
                {
                    int divisionId = kvp.Key;
                    RememberedDivision otherDiv = kvp.Value;
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

        FixSubordinates(otherDiv);
    }

    private void FixSubordinates(RememberedDivision changedDivision)
    {
        Division commander;
        
        if(changedDivision.Commander == this.DivisionId)
        {
            commander = this;
        }
        else
        {
            if(RememberedDivisions.ContainsKey(changedDivision.Commander))
            {
                var rememeredCommander = RememberedDivisions[changedDivision.Commander];
                if (rememeredCommander.HasBeenDestroyed)
                {
                    //he has been orphined so we pick him up
                    commander = this;
                    changedDivision.Controller.AttachedDivision.Commander = this.DivisionId;
                    changedDivision.Commander = this.DivisionId;
                    changedDivision.TimeStamp = GameManager.Instance.GameTime;
                    RememberedDivisions[changedDivision.DivisionId] = changedDivision;
                }
                else
                {
                    commander = rememeredCommander;
                }
                
            }
            else
            {
                //hes clueless so go ahead and put him directly under you
                //commander = this;
                //changedDivision.Controller.AttachedDivision.Commander = this.DivisionId;
                return;
            }
        }
        
        if(!commander.Subordinates.Contains(changedDivision.DivisionId))
        {
            commander.Subordinates.Add(changedDivision.DivisionId);
        }


        //find commander
        //FindDivisionInSubordinates(this, to, new List<RememberedDivision>());

    }

    /*
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
    */
    /*
    public void RefreshOrdersRememberedDivisions(Dictionary<int, RememberedDivision> divisions)
    {
        foreach(var order in OrderQueue)
        {
            order.RefreshRememberedDivisions(divisions);
        }

        OngoingOrder.RefreshRememberedDivisions(divisions);
    }
    */
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

        foreach (int subordinateId in start.Subordinates)
        {
            RememberedDivision division = RememberedDivisions[subordinateId];
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
        if(Soldiers.Count <= 1)
        {
            throw new InsufficientSoldierCountException();
        }

        Soldier soldier = Soldiers[Soldiers.Count - 1];
        Soldiers.Remove(soldier);
        
        return soldier;
    }

    public DivisionController CreateNewDivision()
    {
        List<Soldier> soldiersToGive = new List<Soldier>
        {
            PopSoldier()
        };
        return Controller.CreateChild(soldiersToGive);
    }

    public bool TryCreateNewDivision(out DivisionController newDivision)
    {
        Soldier soldier = null;
        try
        {
            soldier = PopSoldier();
        }
        catch(InsufficientSoldierCountException)
        {
            newDivision = null;
            return false;
        }

        List<Soldier> soldiersToGive = new List<Soldier>
        {
            soldier
        };

        newDivision = Controller.CreateChild(soldiersToGive);
        return true;
    }

    //ONLY CALL ON DIVISIONS NOT REMEMBERED DIVISOIONS
    public void SendMessenger(RememberedDivision to, List<Order> orders)
    {
        //create a new division
        DivisionController messenger = CreateNewDivision();

        //todo discover location of to
        messenger.AttachedDivision.ReceiveOrder(new FindDivision(messenger.AttachedDivision, DivisionId, to.DivisionId));
        messenger.AttachedDivision.ReceiveOrder(new SendMessage(messenger.AttachedDivision, DivisionId, orders, to.DivisionId));
    }

    public override string ToString()
    {
        return $"({DivisionId}, {Commander})";
    }

    public string SerializeSubordinates(Division division, Division rootDivision)
    {
        string str = "[" + division.ToString();

        foreach (var subordinateId in division.Subordinates)
        {
            var subordinate = rootDivision.RememberedDivisions[subordinateId];
            str += SerializeSubordinates(subordinate, rootDivision);
        }

        return str + "]";
    }
}
