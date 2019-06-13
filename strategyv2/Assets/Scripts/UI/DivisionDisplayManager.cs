using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DivisionDisplayManager : MonoBehaviour
{
    public static DivisionDisplayManager Instance { get; set; }
    [SerializeField]
    private int _divisionIdToDisplay;
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

    private Dictionary<SoldierType, SoldierCardController> soldierTypeCards = new Dictionary<SoldierType, SoldierCardController>();

    private void Awake()
    {
        Instance = this;
    }

    public void DisplayDivision(Division divisionToDisplay)
    {
        _divisionDisplayContainer.SetActive(true);

        _divisionIdToDisplay = divisionToDisplay.DivisionId;
        var localPlayer = LocalPlayerController.Instance.GeneralDivision;
        RefreshDivision(localPlayer.AttachedDivision);
        localPlayer.AttachedDivision.AddRefreshDelegate(RefreshDivision);

        InputController.Instance.RegisterOnClickCallBack(OnClickOff);
    }

    public void RefreshDivision(Division division)
    {
        var localPlayer = LocalPlayerController.Instance.GeneralDivision.AttachedDivision;
        var displayedDivision = localPlayer.RememberedDivisions[_divisionIdToDisplay];
        _divisionNameDisplay.text = displayedDivision.Name;
        _troopCountDisplay.text = displayedDivision.NumSoldiers.ToString();
        _supplyDisplay.text = $"{displayedDivision.Supply}/{displayedDivision.MaxSupply}";
        UpdateSoldierCards(displayedDivision);
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
                card.transform.SetParent(_soldierCardsContainer.transform);
                soldierTypeCards.Add(kvp.Key, card.GetComponent<SoldierCardController>());
                UpdateSoldierCard(kvp.Key, kvp.Value);
            }
        }
    }

    private void UpdateSoldierCard(SoldierType type, List<Soldier> soldiers)
    {
        var card = soldierTypeCards[type];
        card.TypeDisplay.text = type.ToString();
        card.TroopAmountDisplay.text = soldiers.Count.ToString();

        float supply = 0, MaxSupply = 0;

        foreach(var soldier in soldiers)
        {
            supply += soldier.Supply;
            MaxSupply += soldier.MaxSupply;
        }

        card.SupplyAmountDisplay.text = $"{supply}/{MaxSupply}";
    }

    public void OnClickOff(Vector3 pos)
    {
        _divisionDisplayContainer.SetActive(false);
    }
}
