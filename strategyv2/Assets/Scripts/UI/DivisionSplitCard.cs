using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DivisionSplitCard : MonoBehaviour {
    public List<Soldier> soldiers;
    public List<Soldier> selectedSoldiers;
    public Text unitSelectedRatio;
    public Text name;
    public InputField numUnitsToSelectField;

    public void Setup(SoldierType type,List<Soldier> soldiers)
    {
        this.soldiers = soldiers;
        selectedSoldiers = new List<Soldier>();
        name.text = type.ToString();
        unitSelectedRatio.text = "0/" + soldiers.Count;
        /*
        var onChange = new InputField.OnChangeEvent();
        onChange.AddListener(OnTextChanged);
        numUnitsToSelectField.onValueChanged = onChange;*/
    }

    public void OnTextChanged(string str)
    {
        int amt;

        if (!int.TryParse(str, out amt))
        {
            amt = 0;
        }

        UpdateSelectedSoldiers(amt);
    }

    public void UpdateSelectedSoldiers(int amt)
    {
        selectedSoldiers = new List<Soldier>();
        if(amt > soldiers.Count)
        {
            amt = soldiers.Count;
        }

        for(int k = 0; k < amt; k++)
        {
            selectedSoldiers.Add(soldiers[k]);
        }

        unitSelectedRatio.text = selectedSoldiers.Count + "/" + soldiers.Count;
    }

}
