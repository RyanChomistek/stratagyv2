using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum OrderPrefabType
{
    Split
}

[System.Serializable]
public struct PrefabStorage
{
    [SerializeField]
    public GameObject Prefab;
    [SerializeField]
    public OrderPrefabType Type;
}


public class OrderPrefabManager : MonoBehaviour {

    public static OrderPrefabManager instance;
    [SerializeField]
    private List<PrefabStorage> _prefabsInput;
    public Dictionary<OrderPrefabType, GameObject> prefabs;
    void Awake()
    {
        instance = this;
        prefabs = new Dictionary<OrderPrefabType, GameObject>();
        foreach(var prefab in _prefabsInput)
        {
            prefabs.Add(prefab.Type, prefab.Prefab);
        }
    }
}
