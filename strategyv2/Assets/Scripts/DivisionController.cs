using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DivisionController : MonoBehaviour {
    public Division AttachedDivision;
    public GameObject DivisionPrefab;
    // Use this for initialization
    void Awake () {
        AttachedDivision = new Division(this);
    }
	
	// Update is called once per frame
	void Update () {
        AttachedDivision.DoOrders();
    }

    public virtual DivisionController CreateChild(List<Soldier> soldiersForChild)
    {
        GameObject newDivision = Instantiate(DivisionPrefab);
        DivisionController newController = newDivision.GetComponent<DivisionController>();
        AttachedDivision.CreateChild(soldiersForChild, newController);
        return newController;
    }
}
