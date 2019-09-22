using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DivisionDisplayManager : MonoBehaviour
{
    public static DivisionDisplayManager Instance { get; set; }
    [SerializeField]
    public int DivisionIdToDisplay;
    [SerializeField]
    private TextMeshProUGUI _divisionNameDisplay;
    [SerializeField]
    private TextMeshProUGUI _troopCountDisplay;
    [SerializeField]
    private TextMeshProUGUI _supplyDisplay;
    [SerializeField]
    private GameObject _divisionDisplayContainer;
    [SerializeField]
    private GameObject _soldierCardsContainer;
    [SerializeField]
    private GameObject _soldierCardPrefab;
    [SerializeField]
    private ActiveOrdersDisplay _orderDisplay;

    private Dictionary<SoldierType, SoldierCardController> soldierTypeCards = new Dictionary<SoldierType, SoldierCardController>();

    private void Awake()
    {
        Instance = this;
    }

    public void DisplayDivision(Division divisionToDisplay)
    {
        _divisionDisplayContainer.SetActive(true);
        _orderDisplay.ClearOrders();
        DivisionIdToDisplay = divisionToDisplay.DivisionId;
        var localPlayer = LocalPlayerController.Instance.GeneralDivision;
        RefreshDivisionCallback(divisionToDisplay);
        divisionToDisplay.AddRefreshDelegate(RefreshDivisionCallback);
        ButtonHandler Handler = new ButtonHandler(ButtonHandler.LeftClick, (x, y) => { },
            (handler, mousePos) => {
                InputController.Instance.UnRegisterHandler(handler);
                OnClickOff(mousePos);
            });

        InputController.Instance.RegisterHandler(Handler);
    }

    public void RefreshDivisionCallback(Division rootDivision)
    {
        var localPlayer = LocalPlayerController.Instance.GeneralDivision.AttachedDivision;

        //try to see if the division is visible
        if(localPlayer.VisibleDivisions.TryGetValue(DivisionIdToDisplay, out ControlledDivision division))
        {
            RefreshDivision(division);
        }
        else
        {
            //else grab its remembered info
            var rememberedDivision = localPlayer.RememberedDivisions[DivisionIdToDisplay];
            RefreshDivision(rememberedDivision);
        }
    }

    public void RebuildLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(_soldierCardsContainer.GetComponent<RectTransform>());
    }

    public void RefreshDivision(Division displayedDivision)
    {
        _divisionNameDisplay.text = displayedDivision.Name;
        _troopCountDisplay.text = displayedDivision.NumSoldiers.ToString();
        _supplyDisplay.text = $"{Mathf.Round(displayedDivision.Supply)}/{displayedDivision.MaxSupply}";
        UpdateSoldierCards(displayedDivision);
        _orderDisplay.RefreshDisplay(displayedDivision.DivisionId);
        RebuildLayout();
    }

    private void UpdateSoldierCards(Division division)
    {
        Dictionary<SoldierType, List<Soldier>> soldiersSplitIntoTypes =
            division.SplitSoldiersIntoTypes();

        foreach(var kvp in soldiersSplitIntoTypes)
        {
            if (soldierTypeCards.ContainsKey(kvp.Key))
            {
                UpdateSoldierCard(kvp.Key, kvp.Value);
            }
            else
            {
                //create card
                GameObject card = Instantiate(_soldierCardPrefab);
                card.GetComponent<SoldierCardController>().DisplayedSoldierType = kvp.Key;
                card.transform.SetParent(_soldierCardsContainer.transform);
                soldierTypeCards.Add(kvp.Key, card.GetComponent<SoldierCardController>());
                UpdateSoldierCard(kvp.Key, kvp.Value);
            }
        }

        //remove empty cards
        var oldCardTypes = soldierTypeCards.Keys;
        List<SoldierType> cardsToRemove = new List<SoldierType>();

        foreach(var key in oldCardTypes)
        { 
            if(!soldiersSplitIntoTypes.ContainsKey(key))
            {
                cardsToRemove.Add(key);
            }
        }

        foreach (var key in cardsToRemove)
        {
            Destroy(soldierTypeCards[key].gameObject);
            soldierTypeCards.Remove(key);
        }
    }

    private void UpdateSoldierCard(SoldierType type, List<Soldier> soldiers)
    {
        var card = soldierTypeCards[type];
        card.TypeDisplay.text = type.ToString();
        card.TroopAmountDisplay.text = soldiers.Count.ToString();

        float supply = 0, MaxSupply = 0, HP = 0;

        foreach(var soldier in soldiers)
        {
            supply += soldier.Supply;
            MaxSupply += soldier.MaxSupply;
            HP += soldier.Health;
        }

        card.SupplyAmountDisplay.text = $"{Mathf.Round(supply)}/{MaxSupply}";
        card.HPDisplay.text = $"{Mathf.Round(HP * 100)}";

        if(card.DisplayingIndividualCards)
        {
            card.RefreshCards(soldiers);
        }

    }

    public void OnClickOff(Vector3 pos)
    {
        _divisionDisplayContainer.SetActive(false);
    }
}
