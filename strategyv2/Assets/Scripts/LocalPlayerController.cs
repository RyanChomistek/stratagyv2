using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocalPlayerController : PlayerController {
    public static LocalPlayerController Instance { get; set; }
    [SerializeField]
    private BaseDivisionController Selected;

    [SerializeField]
    bool UIwaitingForUnitSelection = false;
    public delegate void UnitSelectDelegate(RememberedDivision division);
    UnitSelectDelegate UnitSelectCallback;

    [SerializeField]
    bool UIwaitingForZoneSelection = false;
    public delegate void ZoneSelectDelegate(ZoneDisplay zoneDisplay);
    ZoneSelectDelegate ZoneSelectCallback;

    private Dictionary<int, RememberedDivisionController> RememberedDivisionControllers = new Dictionary<int, RememberedDivisionController>();
    public GameObject RememberedDivisionControllerPrefab;
    
    public Light GeneralLight;

    void Start () {
        Instance = this;
        //MapManager.Instance.RenderMap(MapDisplays.TilesWithVision);
        ZoneDisplayManager.Instance.RegisterZoneDisplayCallback(SelectZoneDisplay);
    }
	
	void Update () {
        DisplayRememberedDivisions();
        DrawZones();
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
        Debug.Log($"select  {division} {division is ControlledDivision} | {UIwaitingForUnitSelection}");
        if (!UIwaitingForUnitSelection)
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
            UnitSelectCallback?.Invoke(new RememberedDivision(division));
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

    public void RegisterUnitSelectCallback(UnitSelectDelegate callback)
    {
        UnitSelectCallback += callback;
        UIwaitingForUnitSelection = true;
    }

    public void UnRegisterUnitSelectCallback(UnitSelectDelegate callback)
    {
        UnitSelectCallback -= callback;
        UIwaitingForUnitSelection = false;
    }

    public void SelectZoneDisplay(ZoneDisplay zoneDisplay)
    {
        ZoneSelectCallback?.Invoke(zoneDisplay);
    }

    public void RegisterZoneSelectCallback(ZoneSelectDelegate callback)
    {
        ZoneSelectCallback += callback;
        UIwaitingForZoneSelection = true;
        StartCoroutine(ZoneUICancelHelper(callback));
    }
    
    /// <summary>
    /// this function will wait for one frame after the register zone select callback happens 
    /// so that we clear the click and dont unregister it the same frame we register
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator ZoneUICancelHelper(ZoneSelectDelegate callback)
    {
        yield return new WaitForEndOfFrame();
        ButtonHandler Handler = new ButtonHandler(ButtonHandler.LeftClick, 
        (x, y) => 
        {
            InputController.Instance.UnRegisterHandler(x);
            ZoneSelectCallback -= callback;
        }, 
        (x, y) => { });
        InputController.Instance.RegisterHandler(Handler);
    }

    public void UnRegisterZoneSelectCallback(ZoneSelectDelegate callback)
    {
        ZoneSelectCallback -= callback;
        UIwaitingForZoneSelection = false;
    }

    public void BeginCreateZone()
    {
        var zoneDisplay = ZoneDisplayManager.Instance.CreateZoneDisplay();
        
        Vector3 start = Vector3.zero;
        InputController.Instance.RegisterHandler(new DragHandler("Fire1",
            (handler, mousePosition) => 
            {
                zoneDisplay.Change(mousePosition,mousePosition,0);
                start = mousePosition;
            },
            (handler, mousePosition, delta) =>
            {
                zoneDisplay.Change(start, mousePosition,0);
            },
            (handler, point) => 
            {
                InputController.Instance.UnRegisterHandler(handler);
                //check for collisions between rects and merge them
                for (int i = 0; i < ZoneDisplayManager.Instance.Zones.Count; i++)
                {
                    ZoneDisplay otherZoneDisplay = ZoneDisplayManager.Instance.Zones[i];
                    if (otherZoneDisplay != zoneDisplay && otherZoneDisplay.DisplayedZone.Overlaps(zoneDisplay.DisplayedZone))
                    {
                        //will remove otherzonedisplay from zonedisplaymanager
                        zoneDisplay.MergeZone(otherZoneDisplay);
                        i--;
                    }
                }

                GeneralDivision.AttachedDivision.AddZone(zoneDisplay.DisplayedZone);
            }));
    }
}
