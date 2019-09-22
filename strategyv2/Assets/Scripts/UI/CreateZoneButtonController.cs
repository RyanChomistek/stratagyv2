using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateZoneButtonController : MonoBehaviour
{
    public void OnClick()
    {
        Debug.Log("Zone button pressed");
        LocalPlayerController.Instance.BeginCreateZone();
    }
}
