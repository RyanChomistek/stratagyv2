using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PrefabStorage
{
    [SerializeField]
    public GameObject prefab;
    [SerializeField]
    public string identifier;
}


public class OrderPrefabManager : MonoBehaviour {

    public static OrderPrefabManager instance;
    [SerializeField]
    private List<PrefabStorage> prefabsInput;
    public Dictionary<string, GameObject> prefabs;
    public GameObject mainCanvas;
    void Awake()
    {
        instance = this;
        prefabs = new Dictionary<string, GameObject>();
        foreach(var prefab in prefabsInput)
        {
            prefabs.Add(prefab.identifier, prefab.prefab);
        }
    }
}
