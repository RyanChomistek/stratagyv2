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

    // Use this for initialization
    void Start () {
        Instance = this;
	}
	
	// Update is called once per frame
	void Update () {
		
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

    public void Select(RememberedDivision divisionController)
    {
        if (!UIwaitingForSelection)
        {
            //bing up order ui
            OrderDisplayManager.instance.ClearOrders();
            OrderDisplayManager.instance.AddOrderSet(divisionController.PossibleOrders);
        }
        else
        {
            UIResponseRememberedDivision(divisionController);
        }
    }
}
