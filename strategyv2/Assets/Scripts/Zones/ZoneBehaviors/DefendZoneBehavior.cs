using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefendZoneBehavior : BaseZoneBehavior
{
    public override ZoneOrder GetOrder(int commanderId)
    {
        return new PatrolZoneOrder(null, commanderId, null);
    }
}
