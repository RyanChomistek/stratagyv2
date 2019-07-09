using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateZoneButtonController : MonoBehaviour
{
    public void OnClick()
    {
        LocalPlayerController.Instance.BeginCreateZone();
    }
}
