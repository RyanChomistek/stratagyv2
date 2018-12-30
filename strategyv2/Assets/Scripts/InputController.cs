using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class InputController : MonoBehaviour {
    public static InputController Instance { get; set; }
    public Player Player;
    public delegate void OnClick(Vector3 mouseLoc);
    public OnClick OnClickDel;

    // Use this for initialization
    void Start ()
    {
        Instance = this;
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetButtonUp("Fire1") && !EventSystem.current.IsPointerOverGameObject())
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if(OnClickDel != null)
                OnClickDel(mousePos);
        }
	}
    
    public void RegisterOnClickCallBack(OnClick callback)
    {
        Debug.Log("registering click");
        OnClickDel += callback;
    }

    public void UnregisterOnClickCallBack(OnClick callback)
    {
        Debug.Log("unregistering click");
        OnClickDel -= callback;
    }
}
