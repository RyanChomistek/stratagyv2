using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IZoneBehavior
{
    /// <summary>
    /// gets the order which a division who will be attached to this zone will follow
    /// </summary>
    /// <param name="commanderId"></param>
    /// <returns></returns>
    ZoneOrder GetOrder(int commanderId);
}
