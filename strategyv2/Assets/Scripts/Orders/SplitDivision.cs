using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplitDivision : Order {

    List<Tuple<SoldierType, int>> SoldiersToSplit;
    public bool IsFinishedSpliting;
    public SplitDivision(Division divisionToSplit, List<Tuple<SoldierType, int>> soldiersToSplit)
    {
        this.Host = divisionToSplit;
        this.SoldiersToSplit = soldiersToSplit;
        this.IsFinishedSpliting = false;
    }

    public override void Start()
    {
        Debug.Log("start split");
        List<Soldier> soldiers = new List<Soldier>();
        //find soldiers
        foreach(Soldier soldier in Host.Soldiers)
        {
            foreach(var soldierTypeWanted in SoldiersToSplit)
            {
                if(soldier.Type == soldierTypeWanted.first)
                {
                    soldierTypeWanted.second--;
                    soldiers.Add(soldier);
                    continue;
                }
            }
        }

        Host.Controller.CreateChild(soldiers);
        IsFinishedSpliting = true;
    }
    public override void Pause() { }
    public override void End() { }
    public override void OnClickedInUI()
    {

        OrderDisplayManager.instance.ClearOrders();
        CommanderSendingOrder.SendOrderTo(new RememberedDivision(Host), new SplitDivision(Host, 
            new List<Tuple<SoldierType, int>>()
            { new Tuple<SoldierType, int> (SoldierType.Melee, 1)}));
    }
    public override void Proceed() { }
    public override bool TestIfFinished() { return IsFinishedSpliting; }
}
