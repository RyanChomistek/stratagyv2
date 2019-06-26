using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActiveOrdersDisplay : MonoBehaviour
{
    private Dictionary<int, ActiveOrderCardController> OrderCards = new Dictionary<int, ActiveOrderCardController>();

    public GameObject OrderCardContainer;
    public GameObject OrderCardPrefab;

    public void RefreshDisplay(int divisionId)
    {
        var localPlayer = LocalPlayerController.Instance.GeneralDivision.AttachedDivision;
        List<Order> orders = new List<Order>();
        //try to see if the division is visible
        if (localPlayer.VisibleDivisions.TryGetValue(divisionId, out ControlledDivision division))
        {
            orders.Add(division.OngoingOrder);
            orders.AddRange(division.OrderQueue);
            orders.AddRange(division.BackgroundOrderList);
        }
        else
        {
            //else grab its remembered info
            var rememberedDivision = localPlayer.RememberedDivisions[divisionId];
            orders.Add(rememberedDivision.OngoingOrder);
            orders.AddRange(rememberedDivision.OrderQueue);
            orders.AddRange(rememberedDivision.BackgroundOrderList);
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
        OrderCards.Add(order.orderId, card.GetComponent<ActiveOrderCardController>());
        RefreshCard(order);
    }

    public void RefreshCard(Order order)
    {
        var card = OrderCards[order.orderId];
        card.GetComponent<ActiveOrderCardController>().NameDisplay.text = order.name;
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
