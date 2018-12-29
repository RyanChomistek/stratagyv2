using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SplitDivision : Order {

    Dictionary<SoldierType, int> SoldiersToSplit;
    public bool IsFinishedSpliting;
    public SplitDivision(Division divisionToSplit, Dictionary<SoldierType, int> soldiersToSplit)
    {
        this.Host = divisionToSplit;
        this.SoldiersToSplit = soldiersToSplit;
        this.IsFinishedSpliting = false;
    }

    public override void Start()
    {
        Debug.Log("start split");
        List<Soldier> soldiers = new List<Soldier>();

        var soldiersSplitIntoTypes = Host.SplitSoldiersIntoTypes();

        foreach (var soldierTypeWanted in SoldiersToSplit)
        {
            var soldiersOfType = soldiersSplitIntoTypes[soldierTypeWanted.Key];
            var soldiersTaken = soldiersOfType.Take(soldierTypeWanted.Value);
            soldiers.AddRange(soldiersTaken);
            soldiersTaken.ToList().ForEach(x => Host.Soldiers.Remove(x));
        }

        Host.Controller.CreateChild(soldiers);
        IsFinishedSpliting = true;
    }

    public override void Pause() { }
    public override void End() { }
    public override void OnClickedInUI()
    {
        if(Host.Soldiers.Count <= 1)
        {
            return;
        }

        var soldiersWanted = new Dictionary<SoldierType, int>();
        soldiersWanted.Add(SoldierType.Melee, 1);

        OrderDisplayManager.instance.ClearOrders();
        CommanderSendingOrder.SendOrderTo(new RememberedDivision(Host), new SplitDivision(Host, soldiersWanted));
    }
    public override void Proceed() { }
    public override bool TestIfFinished() { return IsFinishedSpliting; }
}
