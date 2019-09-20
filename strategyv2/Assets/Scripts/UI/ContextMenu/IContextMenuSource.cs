using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IContextMenuSource
{
    IContextMenu GetContextMenu();
}

public abstract class BaseContextMenuSource : IContextMenuSource
{
    public abstract IContextMenu GetContextMenu();
}
