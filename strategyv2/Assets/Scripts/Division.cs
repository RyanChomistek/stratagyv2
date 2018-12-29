using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Division {
    protected static int DivisionCounter = 0;
    public int DivisionId;
    public string Name;
    public int Commander;
    public List<Soldier> Soldiers;
    public HashSet<int> Subordinates = new HashSet<int>();

    [HideInInspector]
    public List<Order> OrderQueue;
    [HideInInspector]
    public List<Order> PossibleOrders;
    [HideInInspector]
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
    /*
    public Division(RememberedDivision commander, List<Order> orders,
    List<Soldier> soldiers, List<Order> possibleOrders, HashSet<int> subordinates,
    Dictionary<int, RememberedDivision> rememberedDivisions,
    DivisionController controller = null)
    {
        this.Commander = commander.DivisionId;

        this.PossibleOrders = new List<Order>(possibleOrders);
        this.Subordinates = new HashSet<int>(subordinates);
        this.OrderQueue = new List<Order>(orders);
        this.Soldiers = new List<Soldier>(soldiers);
        
        this.RememberedDivisions = new Dictionary<int, RememberedDivision> (rememberedDivisions);

        this.DivisionId = DivisionCounter;
        DivisionCounter++;

        this.Name = "Division " + DivisionId;
        this.Controller = controller;
        this.OngoingOrder = new EmptyOrder();
        SetupOrders();
        OnChange();
    }
    */
    public Division(Division division, DivisionController controller = null)
    {
        this.Commander = division.Commander;

        this.PossibleOrders = new List<Order>(division.PossibleOrders);
        this.Subordinates = new HashSet<int>(division.Subordinates);
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
        this.Commander = -1;

        this.PossibleOrders = new List<Order>();
        this.Subordinates = new HashSet<int>();
        this.OrderQueue = new List<Order>();
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
        this.PossibleOrders.Add(new Move(this, null, new Vector3()));
        this.PossibleOrders.Add(new SplitDivision(this, null));
        this.PossibleOrders.Add(new ScoutOrder(this, null, new Vector3()));
        this.PossibleOrders.Add(new AttackOrder(this, null, null));
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
        refresh?.Invoke(this);
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
        RememberedDivisions[other.DivisionId].HasBeenDestroyed = true;
        GameObject.Destroy(other.Controller.gameObject);
        OnChange();
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
        foreach(var key in VisibleDivisions.Keys)
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
        RefreshOrdersRememberedDivisions(RememberedDivisions);
    }

    public void UpdateRememberedDivision(RememberedDivision otherDiv)
    {
        int divisionId = otherDiv.DivisionId;

        if(divisionId == DivisionId)
        {
            return;
        }

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
                commander = RememberedDivisions[changedDivision.Commander];
            }
            else
            {
                //hes clueless so go ahead and put him directly under you
                commander = this;
                changedDivision.Controller.AttachedDivision.Commander = this.DivisionId;

            }
        }
        
        if(!commander.Subordinates.Contains(changedDivision.DivisionId))
        {
            commander.Subordinates.Add(changedDivision.DivisionId);
            //Debug.Log(SerializeSubordinates(this));
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
    public void RefreshOrdersRememberedDivisions(Dictionary<int, RememberedDivision> divisions)
    {
        foreach(var order in OrderQueue)
        {
            order.RefreshRememberedDivisions(divisions);
        }

        OngoingOrder.RefreshRememberedDivisions(divisions);
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

    //ONLY CALL ON DIVISIONS NOT REMEMBERED DIVISOIONS
    public void SendMessenger(RememberedDivision to, List<Order> orders)
    {
        //create a new division
        DivisionController messenger = CreateNewDivision();

        //todo discover location of to
        messenger.AttachedDivision.ReceiveOrder(new FindDivision(messenger.AttachedDivision, new RememberedDivision(this), to));
        messenger.AttachedDivision.ReceiveOrder(new SendMessage(messenger.AttachedDivision, new RememberedDivision(this), orders, to));
    }

    public override string ToString()
    {
        return $"({DivisionId}, {Commander}, {Controller})";
    }

    public string SerializeSubordinates(Division division, Division rootDivision)
    {
        string str = division.ToString();

        foreach (var subordinateId in division.Subordinates)
        {
            var subordinate = rootDivision.RememberedDivisions[subordinateId];
            str += SerializeSubordinates(subordinate, rootDivision);
        }

        return str;
    }
}
