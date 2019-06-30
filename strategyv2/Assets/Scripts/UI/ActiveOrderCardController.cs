using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ActiveOrderCardController : MonoBehaviour
{
    public TextMeshProUGUI NameDisplay;
    public Order DisplayedOrder;
    public Action<Order> OnOrderCanceled;

    public void Init(Order order)
    {
        DisplayedOrder = order;
    }

    public void OrderCanceled()
    {
        OnOrderCanceled?.Invoke(DisplayedOrder);
    }
}
