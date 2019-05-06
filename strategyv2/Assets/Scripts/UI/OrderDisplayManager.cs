using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OrderDisplayManager : MonoBehaviour {
    public static OrderDisplayManager instance { get; set; }
    private List<GameObject> displays;
    private List<GameObject> displayedOrders = new List<GameObject>();
    [SerializeField]
    GameObject orderPrefab;

    public void AddOrderSet(List<Order> orders, RememberedDivision division)
    {
        foreach(Order order in orders)
        {
            string name = order.GetType().Name;
            GameObject temp = Instantiate(orderPrefab);
            temp.transform.GetChild(0).GetComponent<Text>().text = name;
            temp.GetComponent<Button>().onClick.AddListener(delegate { order.OnClickedInUI(division); });
            AddToDisplay(temp);
        }
    }

    void AddToDisplay(GameObject order)
    {
        order.transform.SetParent(displays[displayedOrders.Count % displays.Count].transform);
        displayedOrders.Add(order);
    }

    public void ClearOrders()
    {
        foreach(GameObject order in displayedOrders)
        {
            Destroy(order);
        }
        displayedOrders.Clear();
    }

    void Start()
    {
        instance = this;
        int numChildren = transform.childCount;
        displays = new List<GameObject>();
        for (int i = 0; i < numChildren; i++)
        {
            displays.Add(transform.GetChild(i).gameObject);
        }

    }
}
