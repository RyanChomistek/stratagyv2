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
    private TextMeshProUGUI TroopCountDisplay;
    [SerializeField]
    private TextMeshProUGUI SupplyDisplay;

    private void Awake()
    {
        Instance = this;
    }

    public void DisplayDivision(Division divisionToDisplay)
    {
        _divisionIdToDisplay = divisionToDisplay.DivisionId;
        var localPlayer = LocalPlayerController.Instance.GeneralDivision;
        RefreshDivision(localPlayer.AttachedDivision);
        localPlayer.AttachedDivision.AddRefreshDelegate(RefreshDivision);
    }

    public void RefreshDivision(Division division)
    {
        var localPlayer = LocalPlayerController.Instance.GeneralDivision.AttachedDivision;
        TroopCountDisplay.text = localPlayer.RememberedDivisions[_divisionIdToDisplay].NumSoldiers.ToString();
        SupplyDisplay.text = $"{localPlayer.RememberedDivisions[_divisionIdToDisplay].Supply}/{localPlayer.RememberedDivisions[_divisionIdToDisplay].MaxSupply}";
    }
}
