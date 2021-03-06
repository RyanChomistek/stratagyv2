﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DivisionController : BaseDivisionController {

    //wrapper to treat attached divisions as controlled divisions
    public new ControlledDivision AttachedDivision { get { return base.AttachedDivision as ControlledDivision; } set { base.AttachedDivision = value; } }

    [SerializeField]
    public List<DivisionController> VisibleControllers = new List<DivisionController>();

    public bool Hide = false;

    void Awake ()
    {
        InitAwake();
    }

    protected void InitAwake()
    {
        base.AttachedDivision = new ControlledDivision(base.AttachedDivision.TeamId, this);
        AttachedDivision.Init(this);
    }

    private void Start()
    {
        if (AttachedDivision.Commander == -1)
        {
            //AttachedDivision.Commander = Controller.GeneralDivision.AttachedDivision.DivisionId;
            AttachedDivision.Commander = AttachedDivision.DivisionId;
        }

        DivisionControllerManager.Instance.AddDivision(this);
        /*
        AttachedDivision.AddRefreshDelegate(division => {
            FindVisibleDivisions();
            RefreshVisibleDivisions();
            VisibleControllers.ForEach(x => x.RefreshVisibleDivisions());
        });
        */
        AttachedDivision.RefreshDiscoveredTiles();
        FindVisibleDivisions();
        RefreshVisibleDivisions();
        AttachedDivision.AddAutoRunBackgroundOrders();
        
    }

    private void OnDestroy()
    {
        DivisionControllerManager.Instance.RemoveDivision(this);
    }

    public void OnTick()
    {
        var generalDivision = LocalPlayerController.Instance.GeneralDivision;
        
        if (generalDivision == this || generalDivision.VisibleControllers.Contains(this) || GameManager.DEBUG)
        {
            Display(true);
        }
        else
        {
            Display(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        if(AttachedDivision != null && AttachedDivision.RememberedDivisions != null)
        {
            foreach (var kvp in AttachedDivision.RememberedDivisions)
            {
                if (kvp.Value.HasBeenDestroyed)
                {
                    continue;
                }

                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(kvp.Value.Position, 1);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(kvp.Value.Position, kvp.Value.PredictedPosition);

                Gizmos.color = Color.red;
                Gizmos.DrawSphere(kvp.Value.PredictedPosition, 1);
            }

            if(AttachedDivision.RememberedDivisions.ContainsKey(AttachedDivision.Commander))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, AttachedDivision.RememberedDivisions[AttachedDivision.Commander].Controller.transform.position);
            }

            foreach (int id in AttachedDivision.Subordinates)
            {
                if (AttachedDivision.RememberedDivisions.ContainsKey(id))
                {
                    var controller = AttachedDivision.RememberedDivisions[id].Controller;
                    if(controller)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(transform.position, controller.transform.position);
                    }
                }
            }
        }
    }

    public DivisionController CreateChild(List<Soldier> soldiersForChild)
    {
        GameObject newDivision = Instantiate(DivisionControllerManager.Instance.DivisionPrefab);
        DivisionController newController = newDivision.GetComponent<DivisionController>();
        newController.transform.position = transform.position;
        newController.Controller = Controller;
        newController.AttachedDivision.TeamId = Controller.TeamId;
        newController.AttachedDivision.Soldiers.Clear();
        newController.AttachedDivision.TransferSoldiers(soldiersForChild);
        newController.AttachedDivision.PromoteOfficer();
        newController.AttachedDivision.Commander = AttachedDivision.DivisionId;
        AttachedDivision.AddSubordinate(new RememberedDivision(newController.AttachedDivision));
        DivisionControllerManager.Instance.AddDivision(newController);

        return newController;
    }

    public void SendMessenger(RememberedDivision to, RememberedDivision endTarget, List<Order> orders)
    {
        AttachedDivision.SendMessenger(to, endTarget, orders);
    }

    private void OnDivisionEnterSight(DivisionController controller)
    {
        //Debug.Log($"enter {this.name} {controller.name}");

        if(VisibleControllers.Contains(controller))
        {
            VisibleControllers.Remove(controller);
        }

        VisibleControllers.Add(controller);

        //VisibleControllers.ForEach(x => x.RefreshVisibleDivisions());
        RefreshVisibleDivisions();
        //VisibleControllers.ForEach(x => x.RefreshVisibleDivisions());
    }

    private void OnDivisionExitSight(DivisionController controller)
    {
        //Debug.Log($"exit {this.name} {controller.name}");
        //VisibleControllers.ForEach(x => x.RefreshVisibleDivisions());
        RefreshVisibleDivisions();

        VisibleControllers.Remove(controller);
        //VisibleControllers.ForEach(x => x.RefreshVisibleDivisions());
    }

    public void FindVisibleDivisions()
    {
        //VisibleControllers.Clear();
        //var divisions = FindObjectsOfType<DivisionController>();
        var divisions = DivisionControllerManager.Instance.Divisions;

        //foreach(var division in divisions)
        for (int i = 0; i < divisions.Count; i++)
        {
            var division = divisions[i];
            if(division.Hide)
            {
                continue;
            }

            var isInSight = (transform.position - division.transform.position).magnitude < AttachedDivision.MaxSightDistance;
            
            if (isInSight && !VisibleControllers.Contains(division))
            {
                if(division.AttachedDivision.HasBeenDestroyed)
                {
                    OnDivisionExitSight(division);
                }

                OnDivisionEnterSight(division);
            }
            else if (!isInSight && VisibleControllers.Contains(division))
            {
                OnDivisionExitSight(division);
            }
        }
    }

    public void RefreshVisibleDivisions()
    {
        for(int i = 0; i < VisibleControllers.Count; i++)
        {
            if(VisibleControllers[i] == null)
            {
                VisibleControllers.RemoveAt(i);
                i--;
            }
        }

        AttachedDivision.RefreshVisibleDivisions(VisibleControllers);
    }

    public override void SelectDivision()
    {
        base.SelectDivision();
        //VisibleControllers.ForEach(x => x.RefreshVisibleDivisions());
        RefreshVisibleDivisions();
        //VisibleControllers.ForEach(x => x.RefreshVisibleDivisions());
        LocalPlayerController.Instance.Select(this);
    }
}
