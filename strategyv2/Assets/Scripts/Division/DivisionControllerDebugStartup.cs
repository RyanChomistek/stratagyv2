using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DivisionControllerDebugStartup : MonoBehaviour
{
    public BaseDivisionController Controller;
    public int NumSoldiers;
    // Start is called before the first frame update
    void Start()
    {
        if (GameManager.DEBUG)
        {
            for (int i = 0; i < NumSoldiers; i++)
            {

                Soldier random = new Soldier();
                int value = Mathf.RoundToInt(Random.Range(0, 2.5f));
                random.Type = (SoldierType)value;
                Controller.AttachedDivision.Soldiers.Add(random);
            }
        }
    }
}
