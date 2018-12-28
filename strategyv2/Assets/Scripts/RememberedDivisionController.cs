using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RememberedDivisionController : BaseDivisionController
{
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
}
