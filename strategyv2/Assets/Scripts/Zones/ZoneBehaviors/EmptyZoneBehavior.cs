using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyZoneBehavior : BaseZoneBehavior
{
    public override ZoneOrder GetOrder(int commanderId)
    {
        return new ZoneOrder(null, commanderId, null);
    }
    public override string ToString()
    {
        return "EmptyZoneBehavior";
    }
}

