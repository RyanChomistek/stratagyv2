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
        MapManager.Instance.OnTerrrainGenerationFinished += () =>
        {
            for (int i = 0; i < NumSoldiers; i++)
            {
                Soldier random = new Soldier();
                int value = Mathf.RoundToInt(Random.Range(1, 3.5f));
                random.Type = (SoldierType)value;
                Controller.AttachedDivision.Soldiers.Add(random);
            }

            Controller.AttachedDivision.PromoteOfficer();
        };
    }
}
