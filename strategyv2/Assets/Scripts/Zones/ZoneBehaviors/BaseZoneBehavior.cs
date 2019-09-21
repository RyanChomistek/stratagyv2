using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseZoneBehavior : IZoneBehavior
{
    public virtual ZoneOrder GetOrder(int commanderId)
    {
        throw new System.NotImplementedException();
    }

    public static List<IZoneBehavior> GetAllZoneBehaviors()
    {
        List<IZoneBehavior> behaviors = new List<IZoneBehavior>
        {
            new DefendZoneBehavior(),
            new EmptyZoneBehavior()
        };

        return behaviors;
    }

    public override string ToString()
    {
        return "Base Zone";
    }
}
