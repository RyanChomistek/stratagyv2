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
        //bing up ui to split choose what units to split
        GameObject splitMenu = OrderPrefabManager.Instantiate(OrderPrefabManager.instance.prefabs["DivisionSplitMenu"]);
        splitMenu.transform.SetParent(OrderPrefabManager.instance.mainCanvas.transform, false);
        splitMenu.GetComponent<DivisionSplitMenu>().Setup(Host);
        //regester a func as a callback
        //send the order
    }
    public override void Proceed() { }
    public override bool TestIfFinished() { return IsFinishedSpliting; }
}
