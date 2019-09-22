using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public interface IContextMenuController
{
    void Init(Vector3 screenPositionOfClick, IContextMenu menu, IContextMenuController parent);
    void CloseMenu();
    void OpenSubMenu(IContextMenuItem item);

    /// <summary>
    /// removes the sub menu from this menu. DOES NOT DESTROY ANYTHING
    /// </summary>
    void RemoveSubMenu();
}

public class ContextMenuController : MonoBehaviour, IContextMenuController, IPointerEnterHandler, IPointerExitHandler
{
    protected IContextMenu _Menu;
    protected IContextMenuController _Parent;
    protected IContextMenuController _SubMenu;

    [SerializeField]
    private ContextMenuCardController m_ContextMenuCardPrefab;
    [SerializeField]
    private Transform m_ContextMenuCardHolder;
    private Vector2 m_MinSize = new Vector2(50,0);
    private const int m_CharacterSize = 15;
    private const int m_LineSize = 30;
    private Vector2 m_size;
    private bool m_IsMouseOverThisMenu = true;
    private Dictionary<uint, ContextMenuCardController> m_CardControllers;
    ButtonHandler ClickOffMenuHandler;

    public void Init(Vector3 screenPositionOfClick, IContextMenu menu, IContextMenuController parent)
    {
        name = $"ContextMenu {menu.Id}";
        _Menu = menu;
        _Parent = parent;
        m_CardControllers = new Dictionary<uint, ContextMenuCardController>();
        m_size = m_MinSize;
        foreach(IContextMenuItem item in menu.Items)
        {
            ContextMenuCardController card = Instantiate(m_ContextMenuCardPrefab);
            card.transform.SetParent(m_ContextMenuCardHolder);
            card.Init(item, this);
            m_size.x = Mathf.Max(m_size.x, m_CharacterSize * item.GetDisplayString().Length);
            m_size.y += m_LineSize;
            m_CardControllers.Add(item.Id, card);
        }

        GetComponent<RectTransform>().sizeDelta = m_size;

        // Set the position make sure to offset so that the mouse appears in uper left corner
        GetComponent<RectTransform>().position = screenPositionOfClick + (new Vector3(m_size.x, m_size.y * -1) * .9f)/2;

        // TODO add a button handler so that when the user clicks off the menu they close the menu
        m_IsMouseOverThisMenu = false;

        ClickOffMenuHandler = new ButtonHandler(ButtonHandler.RightClick,
            // Down
            (handler, position) => { if (!m_IsMouseOverThisMenu && _SubMenu == null) { CloseMenu(); } },
            // Up
            (handler, position) => {  });

        ClickOffMenuHandler.IgnoreUI = false;
        ClickOffMenuHandler.OnlyUI = false;

        InputController.Instance.RegisterHandler(ClickOffMenuHandler);

    }

    public void CloseMenu()
    {
        if(_Parent != null)
        {
            _Parent.RemoveSubMenu();
        }

        if(_SubMenu != null)
        {
            _SubMenu.CloseMenu();
        }

        Destroy(gameObject);
    }

    public void OpenSubMenu(IContextMenuItem subMenuItem)
    {
        // Find Item
        ContextMenuCardController cardController = m_CardControllers[subMenuItem.Id];

        Vector3 position = cardController.transform.position + new Vector3(m_size.x/2,0);
            

        // Create a new submenu at its spot
        _SubMenu = ContextMenuManager.Instance.CreateContextMenu(Camera.main.ScreenToWorldPoint(position), subMenuItem.GetSubMenu(), this);
    }

    public void RemoveSubMenu()
    {
        Debug.Assert(_SubMenu != null);
        // Sub menu should handle destroying itself
        _SubMenu = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Debug.Log("Mouse enter");
        m_IsMouseOverThisMenu = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //Debug.Log($"{name} Mouse exit {_SubMenu} {_SubMenu == null} |");
        m_IsMouseOverThisMenu = false;
        if (_SubMenu == null)
        {
            CloseMenu();
        }
    }

    public void OnDestroy()
    {
        //free input handelers
        InputController.Instance.UnRegisterHandler(ClickOffMenuHandler);
    }
}
