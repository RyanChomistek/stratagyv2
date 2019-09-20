using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseZoneBehavior : IZoneBehavior
{
    public virtual ZoneOrder GetOrder(int commanderId)
    {
        throw new System.NotImplementedException();
    }
}
