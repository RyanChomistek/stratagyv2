using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DivisionController : MonoBehaviour {
    public Division AttachedDivision;
    public GameObject DivisionPrefab;

    [SerializeField]
    private List<DivisionController> VisibleControllers = new List<DivisionController>();

    void Awake () {
        AttachedDivision = new Division(this);
        var soldiers = new List<Soldier>() { new Soldier() };
        soldiers[0].Count = 5;
        AttachedDivision.TransferSoldiers(soldiers);
    }
	
	void Update () {
        AttachedDivision.DoOrders();
        RefreshVisibleDivisions();
        AttachedDivision.RefreshRememberedDivisionsFromVisibleDivisions();
    }

    public virtual DivisionController CreateChild(List<Soldier> soldiersForChild)
    {
        GameObject newDivision = Instantiate(DivisionPrefab);
        DivisionController newController = newDivision.GetComponent<DivisionController>();
        AttachedDivision.CreateChild(soldiersForChild, newController);
        return newController;
    }

    public void SendMessenger(RememberedDivision to, Order order)
    {
        AttachedDivision.SendMessenger(to, order);
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
}
