using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergeDivisions : Order {

    RememberedDivision Target;
    bool FinishedMerging;
    public MergeDivisions(Division controller, RememberedDivision target)
    {
        this.Host = controller;
        this.Target = target;
        this.FinishedMerging = false;
    }

    public override void Start() { }
    public override void Pause() { }
    public override void End() { }
    public override void OnClickedInUI() { }
    public override void Proceed()
    {
        Division divisionToMergeWith;
        if(Host.FindVisibleDivision(Target.DivisionId, out divisionToMergeWith))
        {
            divisionToMergeWith.AbsorbDivision(Host);
            FinishedMerging = true;
        }
    }
    public override bool TestIfFinished() { return FinishedMerging; }
}
