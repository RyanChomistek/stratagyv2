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

    public bool DisplayingIndividualCards = false;

    public void ToggleIndividualCardDisplay()
    {
        if(!DisplayingIndividualCards)
        {
            var localPlayer = LocalPlayerController.Instance.GeneralDivision.AttachedDivision;
            
            //try to see if the division is visible
            if (localPlayer.VisibleDivisions.TryGetValue(DivisionIdToDisplay, out ControlledDivision division))
            {
                Dictionary<SoldierType, List<Soldier>> soldiersSplitIntoTypes = division.SplitSoldiersIntoTypes();
                List<Soldier> soldiers = soldiersSplitIntoTypes[DisplayedSoldierType];
                RefreshCards(soldiers);
            }
            else
            {
                //else grab its remembered info
                var rememberedDivision = localPlayer.RememberedDivisions[DivisionIdToDisplay];
                Dictionary<SoldierType, List<Soldier>> soldiersSplitIntoTypes = rememberedDivision.SplitSoldiersIntoTypes();
                List<Soldier> soldiers = soldiersSplitIntoTypes[DisplayedSoldierType];
                RefreshCards(soldiers);
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

        DisplayingIndividualCards = !DisplayingIndividualCards;
        DivisionDisplayManager.Instance.RebuildLayout();
    }

    public void RefreshCards(List<Soldier> soldiers)
    {
        foreach (var soldier in soldiers)
        {
            if(!soldierCards.ContainsKey(soldier.Id))
            {
                CreateIndividualSoldierCard(soldier);
            }
            else
            {
                RefreshCard(soldier);
            }
        }
    }

    public void CreateIndividualSoldierCard(Soldier soldier)
    {
        //create card
        GameObject card = Instantiate(IndividualSoldierCardPrefab);
        card.transform.SetParent(IndividualSoldierCardContainer.transform);
        soldierCards.Add(soldier.Id, card.GetComponent<IndividualSoldierCard>());
        
        RefreshCard(soldier);
    }

    public void RefreshCard(Soldier soldier)
    {
        var card = soldierCards[soldier.Id];
        card.GetComponent<IndividualSoldierCard>().HPDisplay.text = "" + Mathf.Round(soldier.Health * 100);
        card.GetComponent<IndividualSoldierCard>().SupplyAmountDisplay.text = "" + Mathf.Round(soldier.Supply * 100) / 100;
    }
}
