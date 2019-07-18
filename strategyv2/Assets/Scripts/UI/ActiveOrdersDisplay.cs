using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActiveOrdersDisplay : MonoBehaviour
{
    private Dictionary<int, ActiveOrderCardController> OrderCards = new Dictionary<int, ActiveOrderCardController>();

    public GameObject OrderCardContainer;
    public GameObject OrderCardPrefab;

    public int DisplayedDivisonId;

    public void RefreshDisplay(int divisionId)
    {
        DisplayedDivisonId = divisionId;
        var localPlayer = LocalPlayerController.Instance.GeneralDivision.AttachedDivision;
        List<Order> orders = new List<Order>();
        //try to see if the division is visible
        if (localPlayer.VisibleDivisions.TryGetValue(divisionId, out ControlledDivision division))
        {
            orders.Add(division.OrderSystem.OngoingOrder);
            orders.AddRange(division.OrderSystem.OrderQueue);
            orders.AddRange(division.OrderSystem.BackgroundOrderList);
        }
        else
        {
            //else grab its remembered info
            var rememberedDivision = localPlayer.RememberedDivisions[divisionId];
            orders.Add(rememberedDivision.OrderSystem.OngoingOrder);
            orders.AddRange(rememberedDivision.OrderSystem.OrderQueue);
            orders.AddRange(rememberedDivision.OrderSystem.BackgroundOrderList);
        }

        RefreshCards(orders);
    }

    public void RefreshCards(List<Order> orders)
    {
        foreach (var order in orders)
        {
            if (!OrderCards.ContainsKey(order.orderId))
            {
                CreateOrderCard(order);
            }
        }

        //remove empty cards
        var oldCards = OrderCards.Keys;
        List<int> newOrderIds = orders.Select(x => x.orderId).ToList();
        List<int> cardsToRemove = new List<int>();

        foreach (var key in oldCards)
        {
            if (!newOrderIds.Contains(key))
            {
                cardsToRemove.Add(key);
            }
        }

        foreach (var key in cardsToRemove)
        {
            Destroy(OrderCards[key].gameObject);
            OrderCards.Remove(key);
        }
    }

    public void CreateOrderCard(Order order)
    {
        GameObject card = Instantiate(OrderCardPrefab);
        card.transform.SetParent(OrderCardContainer.transform);
        card.GetComponent<ActiveOrderCardController>().DisplayedOrder = order;
        card.GetComponent<ActiveOrderCardController>().OnOrderCanceled += CancelOrder;
        OrderCards.Add(order.orderId, card.GetComponent<ActiveOrderCardController>());
        RefreshCard(order);
    }

    public void RefreshCard(Order order)
    {
        var card = OrderCards[order.orderId];
        card.GetComponent<ActiveOrderCardController>().NameDisplay.text = order.name;
    }

    public void CancelOrder(Order order)
    {
        var card = OrderCards[order.orderId];
        OrderCards.Remove(order.orderId);
        Destroy(card.gameObject);
        
        var localPlayer = LocalPlayerController.Instance.GeneralDivision.AttachedDivision;

        if(localPlayer.TryGetVisibleOrRememberedDivisionFromId(DisplayedDivisonId, out Division division))
        {
            localPlayer.SendOrderTo(new RememberedDivision(division), 
                new CancelOrder(division, localPlayer.DivisionId, new HashSet<int>() { order.orderId }), 
                ref localPlayer.RememberedDivisions);
        }
    }

    public void ClearOrders()
    {
        Debug.Log("clearing order display " + OrderCards.Keys.Count);
        foreach (var kvp in OrderCards)
        {
            Destroy(kvp.Value.gameObject);
        }

        OrderCards.Clear();
    }
}
