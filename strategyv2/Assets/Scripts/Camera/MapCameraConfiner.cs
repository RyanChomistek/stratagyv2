using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapCameraConfiner : MonoBehaviour
{
    Vector2 mapSize;
    public BoxCollider2D confines;
    // Start is called before the first frame update
    void Start()
    {
        var x = MapManager.Instance.map.GetUpperBound(0);
        var y = MapManager.Instance.map.GetUpperBound(1);
        mapSize = new Vector2(x, y);
        transform.position = mapSize / 2;
        confines.size = mapSize + Vector2.one * 2;
        
    }
}
