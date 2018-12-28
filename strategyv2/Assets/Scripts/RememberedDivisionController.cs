using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RememberedDivisionController : BaseDivisionController
{
    RememberedDivision RememberedAttachedDivision { get { return (RememberedDivision)AttachedDivision; } }
    void Update()
    {
        var generalDivision = LocalPlayerController.Instance.GeneralDivision;

        if (generalDivision == this || 
            generalDivision.VisibleControllers.Contains(AttachedDivision.Controller) || 
            ((RememberedDivision) AttachedDivision).HasBeenDestroyed)
        {
            Display(false);
        }
        else
        {
            Display(true);
        }
    }

    public override void SelectDivision()
    {
        base.SelectDivision();
        LocalPlayerController.Instance.Select(this);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(RememberedAttachedDivision.PredictedPosition, 1);
    }
}
