using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DivisionSplitMenu : MonoBehaviour {

    private Division division;

    [SerializeField]
    GameObject DivisionToSplitCardPrefab;

    public void Setup(Division division)
    {
        this.division = division;

        //build menu
        Dictionary<SoldierType, List<Soldier>> soldiersSplitIntoTypes =
            division.SplitSoldiersIntoTypes();

        foreach(var soldierGroup in soldiersSplitIntoTypes)
        {
            CreateCard(soldierGroup.Key, soldierGroup.Value);
        }
    }

    void CreateCard(SoldierType type, List<Soldier> soldiers)
    {
        GameObject card = Instantiate(DivisionToSplitCardPrefab);
        DivisionToSplitCardPrefab.GetComponent<DivisionSplitCard>().Setup(type, soldiers);
        card.transform.SetParent(transform);
    }

    public void OnSubmit()
    {

    }
}
