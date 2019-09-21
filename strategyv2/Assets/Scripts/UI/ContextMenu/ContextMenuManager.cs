using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextMenuManager : MonoBehaviour
{
    [SerializeField]
    private ContextMenuController m_ContextMenuPrefab;
    [SerializeField]
    private Canvas m_ContextMenuCanvas;
    public static ContextMenuManager Instance { get; set; }

    private void Awake()
    {
        Debug.Assert(Instance == null, "ContextMenuManager instance already set.");
        Instance = this;
    }

    /// <summary>
    /// creates a context menu from given source
    /// </summary>
    /// <param name="source"></param>
    public ContextMenuController CreateContextMenu(Vector3 position, IContextMenuSource source)
    {
        return CreateContextMenu(position, source.GetContextMenu(), null);
    }

    public ContextMenuController CreateContextMenu(Vector3 position, IContextMenu menu)
    {
        return CreateContextMenu(position, menu, null);
    }

    public ContextMenuController CreateContextMenu(Vector3 position, IContextMenu menu, IContextMenuController parent)
    {
        Debug.Log($"adding context menu at {position}");
        ContextMenuController newContextMenu = Instantiate(m_ContextMenuPrefab);
        newContextMenu.transform.SetParent(m_ContextMenuCanvas.transform);
        newContextMenu.Init(Camera.main.WorldToScreenPoint(position), menu, parent);
        return newContextMenu;
    }

}
