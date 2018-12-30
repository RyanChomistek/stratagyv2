using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergeDivisions : Order
{
    int TargetId;
    bool FinishedMerging;

    public MergeDivisions(Division controller, int commanderSendingOrderId, int targetId)
        : base(controller, commanderSendingOrderId, "merge")
    {
        this.FinishedMerging = false;
        this.TargetId = targetId;
    }

    public override void Proceed(Division Host)
    {
        Division divisionToMergeWith;
        if(Host.FindVisibleDivision(TargetId, out divisionToMergeWith))
        {
            divisionToMergeWith.AbsorbDivision(Host);
            FinishedMerging = true;
        }
    }

    public override bool TestIfFinished(Division Host) { return FinishedMerging; }
}
