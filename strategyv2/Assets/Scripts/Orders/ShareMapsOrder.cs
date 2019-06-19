using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShareMapsOrder : Order
{
    int TargetId;
    bool FinishedSharing;

    public ShareMapsOrder(Division controller, int commanderSendingOrderId, int targetId)
        : base(controller, commanderSendingOrderId, "merge")
    {
        this.FinishedSharing = false;
        this.TargetId = targetId;
    }

    public override void Proceed(ControlledDivision Host)
    {
        if (Host.FindVisibleDivision(TargetId, out ControlledDivision divisionToMergeWith))
        {
            Host.ShareMapInformation(divisionToMergeWith);
            FinishedSharing = true;
        }
    }

    public override bool TestIfFinished(ControlledDivision Host) { return FinishedSharing; }
}
