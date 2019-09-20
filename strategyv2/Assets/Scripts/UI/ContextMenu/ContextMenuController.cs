using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextMenuController : MonoBehaviour
{
    protected IContextMenu Menu;

    public void Init(IContextMenu menu)
    {
        Menu = menu;
    }
}
