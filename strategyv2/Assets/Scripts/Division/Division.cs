using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections.ObjectModel;
using System;

public class InsufficientSoldierCountException : System.Exception{
}

[System.Serializable]
public class Division : IEquatable<Division>
{
    protected static int DivisionCounter = 0;
    public int DivisionId;
    public int TeamId = -1;
    public string Name;
    public int Commander;
    public ObservableCollection<Soldier> Soldiers = new ObservableCollection<Soldier>();
    public HashSet<int> Subordinates = new HashSet<int>();
    
    public List<Order> OrderQueue = new List<Order>();
    public List<Order> BackgroundOrderList = new List<Order>();
    public List<Order> PossibleOrders = new List<Order>();
    public Order OngoingOrder = null;

    //calculated values
    public float Speed = 10;
    public float MaxSightDistance = 0;
    public float TotalHealth = 0;
    public int NumSoldiers = 0;
    public float DamageOutput = 0;
    public float MaxHitRange = 0;
    public float Supply = 0;
    public float MaxSupply = 0;

    public float MessageCommunicationRange = 1;
    public bool HasBeenDestroyed;

    public DivisionController Controller;

    public delegate void RefreshDelegate(Division division);
    protected RefreshDelegate refresh;
    protected bool RefreshFlag = true;
    #region constructors
    public Division(Division division, DivisionController controller = null)
    {
        this.Commander = division.Commander;

        this.PossibleOrders = new List<Order>(division.PossibleOrders);
        this.Subordinates = new HashSet<int>(division.Subordinates);

        this.OrderQueue = new List<Order>();
        this.BackgroundOrderList = new List<Order>();
        this.OngoingOrder = division.OngoingOrder;

        this.Soldiers = new ObservableCollection<Soldier>(division.Soldiers);
        this.NumSoldiers = division.NumSoldiers;

        this.DivisionId = division.DivisionId;
        this.TeamId = division.TeamId;

        this.Name = "Division " + DivisionId;
        this.Controller = controller;
        
        RecalculateAggrigateValues();
        this.Soldiers.CollectionChanged += OnSoldiersChanged;
    }

    //use for creating a new division from inside a new controller
    public Division(int teamId, DivisionController controller = null)
    {
        this.Commander = -1;

        this.PossibleOrders = new List<Order>();
        this.Subordinates = new HashSet<int>();
        this.OrderQueue = new List<Order>();
        this.BackgroundOrderList = new List<Order>();
        this.Soldiers = new ObservableCollection<Soldier>();
        Init(controller);
        this.Soldiers.CollectionChanged += OnSoldiersChanged;

        this.TeamId = teamId;
    }

    public void Init(DivisionController controller = null)
    {
        this.DivisionId = DivisionCounter;
        DivisionCounter++;

        this.Name = "Division " + DivisionId;
        this.Controller = controller;
        this.OngoingOrder = new EmptyOrder();
        //SetupOrders();
    }

    #endregion

    public virtual void SetupOrders()
    {
        PossibleOrders = new List<Order>();
    }

    #region soldier transfer

    public void TransferSoldiers(List<Soldier> troops)
    {
        troops.ForEach(x => Soldiers.Add(x));
        troops.Clear();
        RecalculateAggrigateValues();
    }

    public void AddSubordinate(RememberedDivision division)
    {
        if(division.DivisionId != this.DivisionId)
        {
            Subordinates.Add(division.DivisionId);
            OnChange();
        }
    }

    public void RemoveSubordinate(RememberedDivision division)
    {
        Subordinates.Remove(division.DivisionId);
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

        ControlledDivision newDivision = new ControlledDivision(TeamId, newController)
        {
            Soldiers = new ObservableCollection<Soldier>(soldiersForChild)
        };

        AddSubordinate(new RememberedDivision(newDivision));
        newDivision.Commander = DivisionId;
        OnChange();
        newDivision.OnChange();
        newController.AttachedDivision = newDivision;
        return newDivision;
    }

    #endregion

    void OnSoldiersChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        RecalculateAggrigateValues();
    }

    public void AddRefreshDelegate(RefreshDelegate refreshDelegate)
    {
        refresh += refreshDelegate;
        OnChange();
    }

    public void OnChange()
    {
        if (!HasBeenDestroyed)
        {
            RefreshFlag = true;
        }
    }

    public void CheckRefresh()
    {
        if (RefreshFlag)
        {
            refresh?.Invoke(this);
            RefreshFlag = false;
        }
    }

    public virtual void RecalculateAggrigateValues()
    {
        //check if soldier hash has changed

        MaxSightDistance = 0;
        Speed = 0;
        TotalHealth = 0;
        DamageOutput = 0;
        MaxHitRange = 0;
        Supply = 0;
        MaxSupply = 0;
        int cnt = 0;
        foreach(Soldier soldier in Soldiers)
        {
            MaxSightDistance += soldier.SightDistance;
            Speed += soldier.Speed;
            TotalHealth += soldier.Health;
            DamageOutput += soldier.HitStrength;
            Supply += soldier.Supply;
            MaxSupply += soldier.MaxSupply;
            MaxHitRange = Mathf.Max(MaxHitRange, soldier.MaxRange);
            cnt++;
        }
        
        MaxSightDistance /= cnt;
        NumSoldiers = cnt;
        Speed /= cnt;
        var pos = Controller.transform.position;
        if(MapManager.Instance != null)
        {
            var tile = MapManager.Instance.GetTileFromPosition(pos);
            var prespeed = Speed;
            Speed *= 1 / (float)tile.MoveCost;
            //Debug.Log($"{prespeed} {Speed} {tile.MoveCost} {Controller.name}");
        }

        OnChange();
    }

    public void FindAndRemoveSubordinateById(int divisionId, ref Dictionary<int, RememberedDivision> rememberedDivisions)
    {
        if (Subordinates.Contains(divisionId))
        {
            Division other = rememberedDivisions[divisionId];
            //promote the guys under him
            foreach (var subordinateId in other.Subordinates)
            {
                var subordinate = rememberedDivisions[subordinateId];
                subordinate.Commander = divisionId;
                AddSubordinate(subordinate);
                subordinate.OnChange();
            }

            Subordinates.Remove(divisionId);
        }
        else
        {
            //look in each of my subordinates to see if they have it
            foreach (var subordinateId in Subordinates)
            {
                var subordinate = rememberedDivisions[subordinateId];
                subordinate.FindAndRemoveSubordinateById(divisionId, ref rememberedDivisions);
            }
        }
    }

    protected void FixSubordinates(RememberedDivision changedDivision, ref Dictionary<int, RememberedDivision> rememberedDivisions)
    {
        Division commander;
        
        if(changedDivision.Commander == this.DivisionId)
        {
            commander = this;
        }
        else
        {
            if(rememberedDivisions.ContainsKey(changedDivision.Commander))
            {
                var rememeredCommander = rememberedDivisions[changedDivision.Commander];
                if (rememeredCommander.HasBeenDestroyed)
                {
                    //he has been orphined so we pick him up
                    commander = this;
                    changedDivision.Controller.AttachedDivision.Commander = this.DivisionId;
                    changedDivision.Commander = this.DivisionId;
                    changedDivision.TimeStamp = GameManager.Instance.GameTime;
                    rememberedDivisions[changedDivision.DivisionId] = changedDivision;
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
            //commander.Subordinates.Add(changedDivision.DivisionId);
            commander.AddSubordinate(changedDivision);
        }


        //find commander
        //FindDivisionInSubordinates(this, to, new List<RememberedDivision>());

    }

    public void SendOrderTo(RememberedDivision to, Order order, ref Dictionary<int, RememberedDivision> rememberedDivisions)
    {
        SendOrdersTo(to, new List<Order>() { order }, ref rememberedDivisions);
    }

    public void SendOrdersTo(RememberedDivision to, List<Order> orders, ref Dictionary<int, RememberedDivision> rememberedDivisions)
    {
        //follow command tree to get there
        List<RememberedDivision> pathToDivision = FindDivisionInSubordinatesHelper(new RememberedDivision(this), to, ref rememberedDivisions);
        //if path is only size one, we're at where the order needs to go
        if (pathToDivision.Count == 1)
        {
            Controller.AttachedDivision.ReceiveOrders(orders);
            return;
        }

        //send order to the next commander
        pathToDivision[0].Controller.SendMessenger(pathToDivision[1], to, orders);
    }

    public List<RememberedDivision> FindDivisionInSubordinatesHelper(RememberedDivision start, RememberedDivision end, ref Dictionary<int, RememberedDivision> rememberedDivisions)
    {
        //first go to top of command chain
        List<RememberedDivision> pathToDivision = new List<RememberedDivision>();
        
        RememberedDivision topOfChain = new RememberedDivision(this);
        
        while (topOfChain.DivisionId != topOfChain.Commander)
        {
            pathToDivision.Add(topOfChain);
            topOfChain = rememberedDivisions[topOfChain.Commander];
        }

        pathToDivision = FindDivisionInSubordinates(topOfChain, end, pathToDivision, ref rememberedDivisions);

        //remove cycles
        Stack<RememberedDivision> prevNodes = new Stack<RememberedDivision>();
        foreach(var division in pathToDivision)
        {
            if (prevNodes.Contains(division))
            {
                while(!prevNodes.Peek().Equals(division))
                {
                    prevNodes.Pop();
                }
            }
            else
            {
                prevNodes.Push(division);
            }
        }

        pathToDivision = prevNodes.ToList();
        pathToDivision.Reverse();
        return pathToDivision;
    }

    //returns the chain of command to a subordinate
    public List<RememberedDivision> FindDivisionInSubordinates(RememberedDivision start, RememberedDivision end, List<RememberedDivision> prev_, ref Dictionary<int, RememberedDivision> rememberedDivisions)
    {
        List<RememberedDivision> prev = new List<RememberedDivision>(prev_);
        prev.Add(start);
        if (start.DivisionId == end.DivisionId)
        {
            return prev;
        }

        foreach (int subordinateId in start.Subordinates)
        {
            if(rememberedDivisions.TryGetValue(subordinateId, out RememberedDivision division))
            {
                //RememberedDivision division = rememberedDivisions[subordinateId];
                List<RememberedDivision> temp = FindDivisionInSubordinates(division, end, prev, ref rememberedDivisions);
                if (temp != null)
                {
                    return temp;
                }
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

    public override string ToString()
    {
        return $"({DivisionId}, {Commander})";
    }

    public string SerializeSubordinates(Division division, Division rootDivision)
    {
        string str = "[" + division + " | ";

        foreach (var subordinateId in division.Subordinates)
        {
            //var subordinate = rootDivision.RememberedDivisions[subordinateId];
            //str += SerializeSubordinates(subordinate, rootDivision);
            str +=  subordinateId + ", ";
        }

        return str + "]";
    }

    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        Division objAsDivision = obj as Division;
        if (objAsDivision == null) return false;
        else return Equals(objAsDivision);
    }

    public bool Equals(Division other)
    {
        return this.DivisionId == other.DivisionId;
    }

    public override int GetHashCode()
    {
        return this.DivisionId;
    }
}
