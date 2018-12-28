using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RememberedDivision : Division
{
    public float TimeStamp;
    public Vector3 Position;
    public Vector3 PredictedPosition {
        get
        {
            return OngoingOrder.GetPredictedPosition(this);
        }
    }
    public Vector3 Velocity;
    public bool HasBeenDestroyed;

    public RememberedDivision(RememberedDivision commander, List<Order> orders,
    List<Soldier> soldiers, List<Order> possibleOrders, Dictionary<int, RememberedDivision> subordinates,
    Dictionary<int, RememberedDivision> rememberedDivisions,
    Vector3 position, Vector3 velocity, bool hasBeenDestroyed)
        : base(commander, orders, soldiers, possibleOrders, subordinates, rememberedDivisions)
    {
        this.Position = position;
        this.Velocity = velocity;
        TimeStamp = GameManager.Instance.GameTime;
        HasBeenDestroyed = hasBeenDestroyed;
    }

    public RememberedDivision(Division division, Vector3 position, Vector3 velocity)
        :base(division, division.Controller)
    {
        this.Position = position;
        this.Velocity = velocity;
        TimeStamp = GameManager.Instance.GameTime;
        HasBeenDestroyed = false;
    }

    public RememberedDivision(Division division)
    : base(division, division.Controller)
    {
        this.Position = division.Controller.transform.position;
        this.Velocity = division.Controller.GetComponent<Rigidbody>().velocity;
        TimeStamp = GameManager.Instance.GameTime;
        HasBeenDestroyed = false;
    }

    /*
     * call this function to update this with another division
     */
    public void Update(DivisionController division)
    {
    
    }

    public void SendOrderTo(RememberedDivision to, Order order)
    {
        //follow commander tree to get there
        List<RememberedDivision> pathToDivision = FindDivisionInSubordinates(this, to, new List<RememberedDivision>());
        //if path is only size one, were at where the order needs to go
        if (pathToDivision.Count == 1)
        {
            Debug.Log(order);
            Controller.AttachedDivision.ReceiveOrder(order);
            return;
        }

        //send order to the next commander
        pathToDivision[0].Controller.SendMessenger(pathToDivision[1], order);
    }

    public override string ToString()
    {
        return $"({DivisionId}, {Controller}, {HasBeenDestroyed})";
    }
}