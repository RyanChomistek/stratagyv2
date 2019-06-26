using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoldierCardController : MonoBehaviour
{
    public SoldierType DisplayedSoldierType;

    public TextMeshProUGUI TypeDisplay;
    public TextMeshProUGUI TroopAmountDisplay;
    public TextMeshProUGUI SupplyAmountDisplay;
    public TextMeshProUGUI HPDisplay;

    public GameObject IndividualSoldierCardContainer;

    public GameObject IndividualSoldierCardPrefab;

    private Dictionary<int, IndividualSoldierCard> soldierCards = new Dictionary<int, IndividualSoldierCard>();

    private int DivisionIdToDisplay { get { return DivisionDisplayManager.Instance.DivisionIdToDisplay; } }

    public bool Displaying = false;

    public void ToggleIndividualCardDisplay()
    {
        if(!Displaying)
        {
            var localPlayer = LocalPlayerController.Instance.GeneralDivision.AttachedDivision;

            //try to see if the division is visible
            if (localPlayer.VisibleDivisions.TryGetValue(DivisionIdToDisplay, out ControlledDivision division))
            {
                UpdateCards(division);
            }
            else
            {
                //else grab its remembered info
                var rememberedDivision = localPlayer.RememberedDivisions[DivisionIdToDisplay];
                UpdateCards(rememberedDivision);
            }
        }
        else
        {
            var oldCardIds = soldierCards.Keys.ToList();
            foreach(var kvp in soldierCards)
            {
                Destroy(kvp.Value.gameObject);
            }

            soldierCards.Clear();
        }

        Displaying = !Displaying;
        DivisionDisplayManager.Instance.RebuildLayout();
    }

    public void UpdateCards(Division displayedDivision)
    {
        Dictionary<SoldierType, List<Soldier>> soldiersSplitIntoTypes =
            displayedDivision.SplitSoldiersIntoTypes();
        List<Soldier> displayedSoldiersList = soldiersSplitIntoTypes[DisplayedSoldierType];
        foreach(var soldier in displayedSoldiersList)
        {
            CreateIndividualSoldierCard(soldier);
        }
    }

    public void CreateIndividualSoldierCard(Soldier soldier)
    {
        //create card
        GameObject card = Instantiate(IndividualSoldierCardPrefab);
        card.transform.SetParent(IndividualSoldierCardContainer.transform);
        soldierCards.Add(soldier.Id, card.GetComponent<IndividualSoldierCard>());
        card.GetComponent<IndividualSoldierCard>().HPDisplay.text = "" + Mathf.Round(soldier.Health * 100);
        card.GetComponent<IndividualSoldierCard>().SupplyAmountDisplay.text = "" + soldier.Supply;
        //UpdateSoldierCard(kvp.Key, kvp.Value);
    }
}
