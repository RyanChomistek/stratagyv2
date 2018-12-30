using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SplitDivision : Order {

    Dictionary<SoldierType, int> SoldiersToSplit;
    public bool IsFinishedSpliting;
    public SplitDivision(Division divisionToSplit, int commanderSendingOrder, Dictionary<SoldierType, int> soldiersToSplit)
        : base(divisionToSplit, commanderSendingOrder, "split")
    {
        this.SoldiersToSplit = soldiersToSplit;
        this.IsFinishedSpliting = false;
    }

    public override void Start(Division Host)
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
        base.Start(Host);
    }

    public override void Pause(Division Host) { }
    public override void End(Division Host) { }
    public override void OnClickedInUI(Division Host)
    {
        if(Host.Soldiers.Count <= 1)
        {
            return;
        }

        var soldiersWanted = new Dictionary<SoldierType, int>();
        soldiersWanted.Add(SoldierType.Melee, 1);

        OrderDisplayManager.instance.ClearOrders();
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(Host, CommanderSendingOrderId);
        CommanderSendingOrder.SendOrderTo(new RememberedDivision(Host), new SplitDivision(Host, CommanderSendingOrderId, soldiersWanted));
    }
    public override void Proceed(Division Host) { }
    public override bool TestIfFinished(Division Host) { return IsFinishedSpliting; }
}
