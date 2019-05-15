using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DivisionControllerManager : MonoBehaviour
{
    private static DivisionControllerManager _instance;
    public static DivisionControllerManager Instance { get { return _instance; } }
    [SerializeField]
    protected List<DivisionController> _divisions;
    public List<DivisionController> Divisions { get { return _divisions; } protected set { _divisions = value; } }

    public void Awake()
    {
        _instance = this;
        _divisions = new List<DivisionController>();    
    }

    public void AddDivision(DivisionController division)
    {
        if(!Divisions.Contains(division))
            Divisions.Add(division);
    }

    public void RemoveDivision(DivisionController division)
    {
        Divisions.Remove(division);
    }
}
