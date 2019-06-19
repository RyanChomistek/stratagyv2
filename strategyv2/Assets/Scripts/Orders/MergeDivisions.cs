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

    public override void Proceed(ControlledDivision Host)
    {
        ControlledDivision divisionToMergeWith;
        if(Host.FindVisibleDivision(TargetId, out divisionToMergeWith))
        {
            Host.ShareMapInformation(divisionToMergeWith);
            divisionToMergeWith.AbsorbDivision(Host);
            FinishedMerging = true;
        }
    }

    public override bool TestIfFinished(ControlledDivision Host) { return FinishedMerging; }
}
