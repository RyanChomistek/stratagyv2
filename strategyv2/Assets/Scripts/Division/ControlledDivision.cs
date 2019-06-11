using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ControlledDivision : Division
{
    public Dictionary<int, RememberedDivision> RememberedDivisions = new Dictionary<int, RememberedDivision>();
    public Dictionary<int, ControlledDivision> VisibleDivisions = new Dictionary<int, ControlledDivision>();

    public ControlledDivision(Division division, DivisionController controller = null)
        : base(division, division.Controller)
    {
        SetupOrders();
    }

    public ControlledDivision(int teamId, DivisionController controller = null)
        : base(teamId, controller)
    {
        SetupOrders();
    }
    
    public void DestroyDivision(ControlledDivision other)
    {
        Debug.Log($"Destorying division {other}, from {this}");
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
        PossibleOrders = new List<Order>();
        this.PossibleOrders.Add(new Move(this, -1, new Vector3()));
        this.PossibleOrders.Add(new SplitDivision(this, -1, null));
        this.PossibleOrders.Add(new ScoutOrder(this, -1, new Vector3()));
        this.PossibleOrders.Add(new AttackOrder(this, -1, -1));
        this.PossibleOrders.Add(new HeartBeatOrder(this, -1, -1));
        this.PossibleOrders.Add(new EngageOrder(this, -1, -1));
        this.PossibleOrders.Add(new RecruitOrder(this, -1));
    }

    public void DoOrders()
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
        //if no orders queued
        else if (OrderQueue.Count == 0)
        {
            OnEmptyOrder();
        }
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
