using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalPlayerController : PlayerController {
    public static LocalPlayerController Instance { get; set; }
    [SerializeField]
    private BaseDivisionController Selected;

    [SerializeField]
    bool UIwaitingForSelection = false;
    public delegate void responseToUI(RememberedDivision division);
    responseToUI UnitSelectCallback;

    private Dictionary<int, RememberedDivisionController> RememberedDivisionControllers = new Dictionary<int, RememberedDivisionController>();
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
            if(kvp.Value.HasBeenDestroyed)
            {
                if (RememberedDivisionControllers.ContainsKey(kvp.Key))
                {
                    var controller = RememberedDivisionControllers[kvp.Key];
                    RememberedDivisionControllers.Remove(kvp.Key);
                    Destroy(controller.gameObject);
                }

                continue;
            }

            if(kvp.Key == GeneralDivision.AttachedDivision.DivisionId)
            {
                continue;
            }
            else if(RememberedDivisionControllers.ContainsKey(kvp.Key))
            {
                RememberedDivisionControllers[kvp.Key].transform.position = kvp.Value.Position;
                RememberedDivisionControllers[kvp.Key].AttachedDivision = kvp.Value;
            }
            else
            {
                var remDiv = Instantiate(RememberedDivisionControllerPrefab);
                remDiv.GetComponent<BaseDivisionController>().AttachedDivision = kvp.Value;
                remDiv.GetComponent<BaseDivisionController>().Controller = this;
                remDiv.transform.position = kvp.Value.Position;
                RememberedDivisionControllers.Add(kvp.Key, remDiv.GetComponent<RememberedDivisionController>());
            }
        }
    }

    public void Select(RememberedDivision division)
    {
        if (!UIwaitingForSelection)
        {
            if(division.Controller.Controller.TeamId != TeamId)
            {
                return;
            }

            Selected = division.Controller;
            //bing up order ui
            OrderDisplayManager.instance.ClearOrders();
            List<Order> orders = new List<Order>(Selected.AttachedDivision.PossibleOrders);
            foreach (Order order in orders)
            {
                order.CommanderSendingOrderId = GeneralDivision.AttachedDivision.DivisionId;
            }

            OrderDisplayManager.instance.AddOrderSet(orders, division);
        }
        else
        {
            UnitSelectCallback(division);
        }
    }

    public void Select(RememberedDivisionController divisionController)
    {
        Select(divisionController.RememberedAttachedDivision);
    }

    public void Select(DivisionController divisionController)
    {
        Select(new RememberedDivision(divisionController.AttachedDivision));
    }

    public void RegisterUnitSelectCallback(responseToUI callback)
    {
        UnitSelectCallback += callback;
        UIwaitingForSelection = true;
    }

    public void UnRegisterUnitSelectCallback(responseToUI callback)
    {
        UnitSelectCallback -= callback;
        UIwaitingForSelection = false;
    }

    
}
