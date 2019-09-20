using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IContextMenu
{
    /// <summary>
    /// gets a unique it for this menu
    /// </summary>
    /// <returns></returns>
    uint Id { get; set; }

    /// <summary>
    /// gets the items contained in this menu
    /// </summary>
    ICollection<IContextMenuItem> Items { get; }
}

public abstract class BaseContextMenu : IContextMenu
{
    protected ICollection<IContextMenuItem> _Items;

    private static uint IdCnt = 0;
    public uint Id { get; set; }

    public ICollection<IContextMenuItem> Items { get { return _Items; } }

    public BaseContextMenu()
    {
        Id = IdCnt;
        IdCnt++;
    }
}
