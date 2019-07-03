using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RememberedDivision : Division
{
    public float TimeStamp;
    public Vector3 Velocity;

    public Vector3 PredictedPosition {
        get
        {
            return OngoingOrder.GetPredictedPosition(this);
        }
    }
    
    /*
    public RememberedDivision(RememberedDivision commander, List<Order> orders,
    List<Soldier> soldiers, List<Order> possibleOrders, HashSet<int> subordinates,
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
    */

    public RememberedDivision(Division division)
    : base(division, division.Controller)
    {
        this.Position = division.Controller.transform.position;
        this.Velocity = division.Controller.GetComponent<Rigidbody>().velocity;
        TimeStamp = GameManager.Instance.GameTime;
        //add a small time delta so remembered divisions that are created in the same frame dont have exactly the same time stamp
        GameManager.Instance.GameTime += .001f;
        HasBeenDestroyed = false;
    }

    public override string ToString()
    {
        //, {Controller}, {HasBeenDestroyed}, {Position}, {Velocity}
        string str = "";

        foreach (var remembered in Subordinates)
        {
            str += remembered + ", ";
        }
        
        return $"({DivisionId}, {OngoingOrder}, <{str}>, {TimeStamp}, {HasBeenDestroyed})";
    }
}