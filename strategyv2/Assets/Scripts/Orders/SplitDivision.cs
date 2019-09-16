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

    /// <summary>
    /// split a division based on the percentage if soldiers to split off
    /// </summary>
    /// <param name="divisionToSplit"></param>
    /// <param name="commanderSendingOrder"></param>
    /// <param name="percentOfSoldiersToSplit"></param>
    public SplitDivision(Division divisionToSplit, int commanderSendingOrder, float percentOfSoldiersToSplit)
        : base(divisionToSplit, commanderSendingOrder, "split")
    {
        this.IsFinishedSpliting = false;
        var types = divisionToSplit.SplitSoldiersIntoTypes();
        this.SoldiersToSplit = new Dictionary<SoldierType, int>();
        foreach(var kvp in types)
        {
            SoldiersToSplit[kvp.Key] = (int) (kvp.Value.Count * percentOfSoldiersToSplit);
        }
    }

    public override void Start(ControlledDivision Host)
    {
        Debug.Log($"start split {Host.Soldiers.Count}");
        List<Soldier> soldiers = new List<Soldier>();

        var soldiersSplitIntoTypes = Host.SplitSoldiersIntoTypes();

        foreach (var soldierTypeWanted in SoldiersToSplit)
        {
            var soldiersOfType = soldiersSplitIntoTypes[soldierTypeWanted.Key];
            var soldiersTaken = soldiersOfType.Take(soldierTypeWanted.Value);
            soldiers.AddRange(soldiersTaken);
            soldiersTaken.ToList().ForEach(x => Host.Soldiers.Remove(x));
        }

        var child = Host.Controller.CreateChild(soldiers);
        IsFinishedSpliting = true;
        base.Start(Host);
        Debug.Log($"end split {Host.Soldiers.Count} + {child.AttachedDivision.Soldiers.Count}");
    }

    public override void Pause(ControlledDivision Host) { }
    public override void End(ControlledDivision Host) { base.End(Host); }
    public override void OnClickedInUI(Division Host, PlayerController playerController)
    {
        if(Host.Soldiers.Count <= 1)
        {
            return;
        }

        var soldiersWanted = new Dictionary<SoldierType, int>();
        soldiersWanted.Add(SoldierType.Melee, 1);

        OrderDisplayManager.Instance.ClearOrders();
        RememberedDivision CommanderSendingOrder = GetRememberedDivisionFromHost(playerController.GeneralDivision.AttachedDivision, CommanderSendingOrderId);
        CommanderSendingOrder.SendOrderTo(new RememberedDivision(Host), new SplitDivision(Host, CommanderSendingOrderId, soldiersWanted), ref playerController.GeneralDivision.AttachedDivision.RememberedDivisions);
    }
    public override void Proceed(ControlledDivision Host) { }
    public override bool TestIfFinished(ControlledDivision Host) { return IsFinishedSpliting; }
}
