﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    
    public Light GeneralLight;

    void Start () {
        Instance = this;
        MapManager.Instance.RenderMap(MapDisplays.TilesWithVision);
    }
	
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
                    //Debug.Log("removing remembered");
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
                RememberedDivisionControllers[kvp.Key].transform.position = kvp.Value.PredictedPosition;
                RememberedDivisionControllers[kvp.Key].AttachedDivision = kvp.Value;
                var LR = RememberedDivisionControllers[kvp.Key].GetComponent<LineRenderer>();
                
                if(GeneralDivision.AttachedDivision.RememberedDivisions.TryGetValue(kvp.Value.Commander, out RememberedDivision commander))
                {
                    LR.SetPosition(0, kvp.Value.PredictedPosition);
                    LR.SetPosition(1, commander.Position);

                    var chainOfCommand = GeneralDivision.AttachedDivision.FindDivisionInSubordinates(new RememberedDivision(GeneralDivision.AttachedDivision), kvp.Value, new List<RememberedDivision>(), ref GeneralDivision.AttachedDivision.RememberedDivisions);
                    //string str = "";
                    //chainOfCommand.Select(x => x.DivisionId).ToList().ForEach(x => str += x + ", ");
                    //Debug.Log(str + " | " + kvp.Key);
                    
                    var color = Color.red;
                    if (chainOfCommand != null)
                    {
                        color = Color.Lerp(Color.red, Color.green, 1f / chainOfCommand.Count);
                    }
                    
                    LR.material.color = color;
                    
                }
                
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

    public void Select(Division division)
    {
        Debug.Log($"select  {division} {division is ControlledDivision}");
        if (!UIwaitingForSelection)
        {
            if(division.Controller.Controller.TeamId != TeamId)
            {
                return;
            }

            Selected = division.Controller;
            //bing up order ui
            OrderDisplayManager.Instance.ClearOrders();
            List<Order> orders = new List<Order>(Selected.AttachedDivision.PossibleOrders);
            foreach (Order order in orders)
            {
                order.CommanderSendingOrderId = GeneralDivision.AttachedDivision.DivisionId;
            }

            OrderDisplayManager.Instance.AddOrderSet(orders, new RememberedDivision(division), this);

            //bring up division display
            DivisionDisplayManager.Instance.DisplayDivision(division);
        }
        else
        {
            UnitSelectCallback(new RememberedDivision(division));
        }
    }

    public void Select(RememberedDivisionController divisionController)
    {
        Select(divisionController.RememberedAttachedDivision);
    }

    public void Select(DivisionController divisionController)
    {
        Select(divisionController.AttachedDivision);
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
