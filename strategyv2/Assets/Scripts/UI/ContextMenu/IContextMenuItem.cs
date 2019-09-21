using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IContextMenuItem
{
    /// <summary>
    /// gets a unique it for this menu item
    /// </summary>
    /// <returns></returns>
    uint Id { get; set; }

    /// <summary>
    /// gets the action which will be called when this item is clicked
    /// </summary>
    /// <returns></returns>
    Action GetAction();

    /// <summary>
    /// gets the display string for this item
    /// </summary>
    /// <returns></returns>
    string GetDisplayString();

    /// <summary>
    /// returns if this item has a sub menu
    /// </summary>
    /// <returns> if this has a sub menu </returns>
    bool HasSubMenu();

    /// <summary>
    /// Gets the sub menu
    /// </summary>
    /// <returns></returns>
    IContextMenu GetSubMenu();
}

public abstract class BaseContextMenuItem : IContextMenuItem
{
    private static uint IdCnt = 0;
    public uint Id { get; set; }

    public BaseContextMenuItem()
    {
        Id = IdCnt;
        IdCnt++;
    }

    public abstract Action GetAction();
    public abstract string GetDisplayString();

    public uint GetId()
    {
        throw new NotImplementedException();
    }

    public abstract IContextMenu GetSubMenu();
    public abstract bool HasSubMenu();

    public bool TryGetSubMenu(out IContextMenu subMenu)
    {
        if(HasSubMenu())
        {
            subMenu = GetSubMenu();
            return true;
        }

        subMenu = null;
        return false;
    }
}
