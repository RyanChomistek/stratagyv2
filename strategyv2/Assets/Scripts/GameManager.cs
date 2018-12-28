using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public float GameSpeed = 1;
    public float DeltaTime { get{ return Time.deltaTime * GameSpeed; } }
    public float GameTime = 0;
    public bool IsPaused = false;
    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        GameTime += DeltaTime;
    }
}