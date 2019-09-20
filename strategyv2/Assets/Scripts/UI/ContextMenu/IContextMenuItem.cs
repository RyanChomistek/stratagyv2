using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IContextMenuItem
{
    Action GetAction();
    string GetDisplayName();
    bool HasSubMenu();
    List<IContextMenuItem> GetSubMenu();
}
