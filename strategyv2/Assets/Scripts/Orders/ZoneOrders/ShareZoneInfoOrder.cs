using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShareZoneInfoOrder : Order
{
    public List<IZone> ZoneInfo;
    public ShareZoneInfoOrder(Division controller, int commanderSendingOrderId, List<IZone> zoneInfo) 
        : base(controller, commanderSendingOrderId, "Share Zone Info")
    {
        ZoneInfo = new List<IZone>();
        //deep copy every zone so that different divisions have different instances of zones
        foreach(var zone in zoneInfo)
        {
            ZoneInfo.Add(new Zone(zone));
        }
    }

    public override void Start(ControlledDivision Host)
    {
        base.Start(Host);
        Host.MergeZones(ZoneInfo);

        //end the order
        Canceled = true;
    }
}
