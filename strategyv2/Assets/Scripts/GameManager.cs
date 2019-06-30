using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public static bool DEBUG = true;
    [SerializeField]
    private float _gameSpeed = 1;
    public static float GameSpeed { get { return GameManager.Instance._gameSpeed; } set { GameManager.Instance._gameSpeed = value; } }
    public static float DeltaTime { get{ return Time.deltaTime * GameSpeed; } }
    public float GameTime = 0;
    public float GameUITimeSetting = .5f;
    public bool IsPaused = false;
    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            if(IsPaused)
            {
                IsPaused = false;
                GameSpeed = GameUITimeSetting;
            }
            else
            {
                IsPaused = true;
                GameSpeed = 0;
            }
        }

        GameTime += DeltaTime * GameSpeed;

    }
}