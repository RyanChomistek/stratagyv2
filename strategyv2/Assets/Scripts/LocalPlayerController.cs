using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalPlayerController : MonoBehaviour {
    public static LocalPlayerController Instance { get; set; }
    [SerializeField]
    private DivisionController Selected;

    public DivisionController GeneralDivision;

    [SerializeField]
    bool UIwaitingForSelection = false;
    public delegate void responseToUI(DivisionController division);
    responseToUI UIResponse;

    public delegate void responseToUIRemembered(RememberedDivision division);
    responseToUIRemembered UIResponseRememberedDivision;

    Dictionary<int, RememberedDivisionController> RememberedDivisionControllers = new Dictionary<int, RememberedDivisionController>();
    public GameObject RememberedDivisionControllerPrefab;
    // Use this for initialization
    void Start () {
        Instance = this;
	}
	
	// Update is called once per frame
	void Update () {
        DisplayRememberedDivisions();
    }

    public void DisplayRememberedDivisions()
    {
        foreach(var kvp in GeneralDivision.AttachedDivision.RememberedDivisions)
        {
            if(RememberedDivisionControllers.ContainsKey(kvp.Key))
            {
                RememberedDivisionControllers[kvp.Key].transform.position = kvp.Value.Position;
                RememberedDivisionControllers[kvp.Key].AttachedDivision = kvp.Value;
            }
            else
            {
                var remDiv = Instantiate(RememberedDivisionControllerPrefab);
                remDiv.GetComponent<BaseDivisionController>().AttachedDivision = kvp.Value;
                remDiv.transform.position = kvp.Value.Position;
                RememberedDivisionControllers.Add(kvp.Key, remDiv.GetComponent<RememberedDivisionController>());
            }
        }
    }

    public void Select(DivisionController divisionController)
    {
        if (!UIwaitingForSelection)
        {
            Selected = divisionController;
            //bing up order ui
            OrderDisplayManager.instance.ClearOrders();
            List<Order> orders = new List<Order>(Selected.AttachedDivision.PossibleOrders);
            foreach(Order order in orders)
            {
                order.CommanderSendingOrder = new RememberedDivision(GeneralDivision.AttachedDivision);
            }

            OrderDisplayManager.instance.AddOrderSet(orders);
        }
        else
        {
            UIResponse(divisionController);
        }
    }

    public void Select(RememberedDivisionController divisionController)
    {
        Select(divisionController.AttachedDivision.Controller);
    }
}
