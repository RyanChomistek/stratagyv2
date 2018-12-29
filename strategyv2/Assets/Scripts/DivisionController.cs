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
        AttachedDivision = new Division(this);
        var soldiers = new List<Soldier>() { new Soldier(), new Soldier(), new Soldier(), new Soldier(), new Soldier() };
        //soldiers[0].Count = 5;
        AttachedDivision.TransferSoldiers(soldiers);
        
    }
	
	void Update () {
        RefreshVisibleDivisions();
        AttachedDivision.RefreshRememberedDivisionsFromVisibleDivisions();

        AttachedDivision.DoOrders();
        AttachedDivision.RecalculateAggrigateValues();
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
            foreach (var kvp in AttachedDivision.RememberedDivisions)
            {
                if(kvp.Value.HasBeenDestroyed)
                {
                    continue;
                }

                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(kvp.Value.Position, 1);
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
