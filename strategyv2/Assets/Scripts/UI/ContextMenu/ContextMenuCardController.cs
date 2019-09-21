using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Button;

public class ContextMenuCardController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI m_DisplayText;
    [SerializeField]
    private Button m_Button;

    public IContextMenuItem Item { get; set; }

    public void Init(IContextMenuItem item, IContextMenuController ContextMenuController)
    {
        Item = item;
        m_DisplayText.text = Item.GetDisplayString();
        if(Item.HasSubMenu())
        {
            m_Button.onClick.AddListener(() => { ContextMenuController.OpenSubMenu(item); });
        }
        else
        {
            m_Button.onClick.AddListener(() => { Item.GetAction()?.Invoke(); ContextMenuController.CloseMenu(); });
        }
        
    }
}
