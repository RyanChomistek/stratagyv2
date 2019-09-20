using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextMenuManager : MonoBehaviour
{
    [SerializeField]
    private ContextMenuController ContextMenuPrefab;

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
    public void CreateContextMenu(Vector3 position, IContextMenuSource source)
    {
        Debug.Log($"adding context menu at {position}");
        ContextMenuController newContextMenu = Instantiate(ContextMenuPrefab, position, Quaternion.identity, null);
    }
}
