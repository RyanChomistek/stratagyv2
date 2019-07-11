using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneDisplayManager : MonoBehaviour
{
    public static ZoneDisplayManager Instance { get; set; }
    private List<ZoneDisplay> Zones = new List<ZoneDisplay>();

    private Action<ZoneDisplay> ZoneDisplaySelectCallback;

    public GameObject ZoneDisplayPrefab;

    private void Start()
    {
        Instance = this;
        InputController.Instance.RegisterOnClickCallBack( mousePosition => 
        {
            var tileCoordinate = MapManager.Instance.GetTilePositionFromPosition(mousePosition);
            foreach(ZoneDisplay zone in Zones)
            {
                if(zone.DisplayedZone.Contains(tileCoordinate))
                {
                    ZoneDisplaySelectCallback?.Invoke(zone);
                }
            }
        });
    }

    public ZoneDisplay CreateZoneDisplay()
    {
        Zone zone = new Zone(new List<Rect>() { new Rect(Vector2.zero, Vector2.zero) }, Color.white);
        ZoneDisplay zoneDisplay = Instantiate(ZoneDisplayPrefab).GetComponent<ZoneDisplay>();
        zoneDisplay.Init(zone);
        RegisterZoneDisplay(zoneDisplay);
        return zoneDisplay;
    }

    public void RegisterZoneDisplay(ZoneDisplay zone)
    {
        Zones.Add(zone);
    }

    public void UnRegisterZoneDisplay(ZoneDisplay zone)
    {
        Zones.Remove(zone);
    }

    public void RegisterZoneDisplayCallback(Action<ZoneDisplay> zoneDisplaySelectCallback)
    {
        ZoneDisplaySelectCallback += zoneDisplaySelectCallback;
    }

    public void UnRegisterZoneDisplayCallback(Action<ZoneDisplay> zoneDisplaySelectCallback)
    {
        ZoneDisplaySelectCallback -= zoneDisplaySelectCallback;
    }
}
