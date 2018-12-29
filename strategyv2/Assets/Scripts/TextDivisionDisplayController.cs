using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextDivisionDisplayController : MonoBehaviour
{
    BaseDivisionController AttachedDivisionController;
    public Text TextBox;
    public Button Selector;
    int AttachmentToken;

    private void Awake()
    {
        
    }

    // Use this for initialization
    void Start ()
    {
        AttachedDivisionController = transform.parent.GetComponent<BaseDivisionController>();
        AttachedDivisionController.AttachedDivision.AddRefreshDelegate(OnDivisionChange);
    }
	
	private void OnDivisionChange(Division division)
    {
        //Debug.Log("refresh " + GenerateText(division));
        TextBox.text = GenerateText(division);
    }

    private string GenerateText(Division division)
    {
        string str = "";
        str += division.Name + '\n';
        str += "commander : " + (division.Commander == -1 ? "NONE" : ""+division.Commander) + "\n";
        str += $"hp :  {division.TotalHealth}\n";
        str += $"dam :  {division.DamageOutput}\n";
        str += $"num S :  {division.NumSoldiers}";

        return str;
    }
}
