using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public DivisionController GeneralDivision;
    private static int _teamIdCnt = 0;
    [SerializeField]
    private int _teamId = -1;
    public int TeamId { get { if (_teamId == -1) { _teamId = _teamIdCnt++; } return _teamId; } }

    private void Awake()
    {
        _teamId = _teamIdCnt++;
    }
}
