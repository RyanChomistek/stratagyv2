using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DivisionControllerManager : MonoBehaviour
{
    private static DivisionControllerManager _instance;
    public static DivisionControllerManager Instance { get { return _instance; } }
    [SerializeField]
    protected List<DivisionController> _divisions;
    public List<DivisionController> Divisions { get { return _divisions; } protected set { _divisions = value; } }

    public GameObject DivisionPrefab;

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
        //Divisions.Remove(division);
    }

    private void Update()
    {
        for (int i = 0; i < Divisions.Count; i++)
        {
            DivisionController division = Divisions[i];
            UnityEngine.Profiling.Profiler.BeginSample("find visible");
            division.FindVisibleDivisions();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("do orders");
            division.AttachedDivision.OrderSystem.DoOrders(division.AttachedDivision);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("do background orders");
            division.AttachedDivision.OrderSystem.DoBackgroundOrders(division.AttachedDivision);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("check refresh");
            division.AttachedDivision.CheckRefresh();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("on tick");
            division.OnTick();
            UnityEngine.Profiling.Profiler.EndSample();
        }

        for (int i = 0; i < Divisions.Count; i++)
        {
            DivisionController division = Divisions[i];
            if(division.AttachedDivision.HasBeenDestroyed)
            {
                Divisions.RemoveAt(i);
                i--;
            }
        }
    }

}
