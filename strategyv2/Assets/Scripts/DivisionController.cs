using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DivisionController : BaseDivisionController {
    
    public GameObject DivisionPrefab;

    [SerializeField]
    public List<DivisionController> VisibleControllers = new List<DivisionController>();

    [SerializeField]
    private SphereCollider SightCollider;
    void Awake () {
        //AttachedDivision = new Division(this);
        //var soldiers = new List<Soldier>() { new Soldier(), new Soldier(), new Soldier(), new Soldier(), new Soldier() };
        //soldiers[0].Count = 5;
        //AttachedDivision.TransferSoldiers(soldiers);
        AttachedDivision.Init(this);
    }

    private void Start()
    {
        
        if (AttachedDivision.Commander == -1)
        {
            AttachedDivision.Commander = Controller.GeneralDivision.AttachedDivision.DivisionId;
        }
    }

    void Update () {

        //these are potentually needed to sync but kills perf
        //RefreshVisibleDivisions();
        //AttachedDivision.RefreshRememberedDivisionsFromVisibleDivisions();

        AttachedDivision.DoOrders();
        AttachedDivision.DoBackgroundOrders();
        //AttachedDivision.RecalculateAggrigateValues();
        SightCollider.radius = AttachedDivision.MaxSightDistance;
        var generalDivision = LocalPlayerController.Instance.GeneralDivision;
        
        if (generalDivision == this || generalDivision.VisibleControllers.Contains(this))
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
        GameObject newDivision = Instantiate(DivisionPrefab);
        DivisionController newController = newDivision.GetComponent<DivisionController>();
        newController.Controller = Controller;
        AttachedDivision.CreateChild(soldiersForChild, newController);
        return newController;
    }

    public void SendMessenger(RememberedDivision to, List<Order> orders)
    {
        AttachedDivision.SendMessenger(to, orders);
    }

    private void OnTriggerEnter(Collider other)
    {
        var controller = other.gameObject.GetComponent<DivisionController>();
        if (!controller)
        {
            return;
        }

        if(VisibleControllers.Contains(controller))
        {
            VisibleControllers.Remove(controller);
        }

        VisibleControllers.Add(controller);
        RefreshVisibleDivisions();
    }

    private void OnTriggerExit(Collider other)
    {
        var controller = other.gameObject.GetComponent<DivisionController>();
        if (!controller)
        {
            return;
        }
        
        VisibleControllers.Remove(controller);
        RefreshVisibleDivisions();
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
        LocalPlayerController.Instance.Select(this);
    }
}
