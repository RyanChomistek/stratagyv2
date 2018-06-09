using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public float GameSpeed = 1;
    public bool IsPaused = false;
    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
            
    }
}