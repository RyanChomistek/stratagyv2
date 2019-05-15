using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHandler
{
    public string ButtonName;
    public Action<ButtonHandler, Vector3> OnButtonDownCallBack;
    public Action<ButtonHandler, Vector3> OnButtonUpCallBack;
    public ButtonHandler(string buttonName, Action<ButtonHandler, Vector3> onButtonDown, Action<ButtonHandler, Vector3> onButtonUp)
    {
        this.ButtonName = buttonName;
        this.OnButtonDownCallBack = onButtonDown;
        this.OnButtonUpCallBack = onButtonUp;
    }

    public virtual void OnButtonDown()
    {
        this.OnButtonDownCallBack(this, Camera.main.ScreenToWorldPoint(Input.mousePosition));
    }

    public virtual void OnButton()
    {}

    public virtual void OnButtonUp()
    {
        this.OnButtonUpCallBack(this, Camera.main.ScreenToWorldPoint(Input.mousePosition));
    }
}

public class DragHandler : ButtonHandler
{
    /*position, delta */
    public Action<DragHandler, Vector3, Vector3> OnDragCallBack;
    
    public bool IsCurrentlyDragging = false;
    public Vector3 LastMousePosition;

    public DragHandler(string buttonName, Action<ButtonHandler, Vector3> onButtonDown, Action<DragHandler, Vector3, Vector3> onButtonDrag, Action<ButtonHandler, Vector3> onButtonUp)
        : base(buttonName, onButtonDown, onButtonUp)
    {
        this.OnDragCallBack = onButtonDrag;
    }

    public override void OnButtonDown()
    {
        this.LastMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        this.IsCurrentlyDragging = true;
        base.OnButtonDown();
    }

    public override void OnButton()
    {
        if (this.IsCurrentlyDragging)
        {
            var mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var mouseDelta = mousePosition - this.LastMousePosition;
            mouseDelta = new Vector3(mouseDelta.x, mouseDelta.y, 0);
            this.LastMousePosition = mousePosition;
            this.OnDragCallBack(this, mousePosition, mouseDelta);
        }
    }

    public override void OnButtonUp()
    {
        this.IsCurrentlyDragging = false;
        base.OnButtonUp();
    }
}

public class AxisHandler
{
    public string AxisName;
    public Action<AxisHandler, float> OnChangeCallBack;
    public AxisHandler(string axisName, Action<AxisHandler, float> onChangeCallBack)
    {
        this.AxisName = axisName;
        this.OnChangeCallBack = onChangeCallBack;
    }

    public virtual void OnChange()
    {
        this.OnChangeCallBack(this, Input.GetAxis(AxisName));
    }
    
}

public class InputController : MonoBehaviour {
    public static InputController Instance { get; set; }
    public Player Player;
    public delegate void OnClick(Vector3 mouseLoc);
    public OnClick OnClickDel;

    private List<ButtonHandler> _buttonHandlers;
    private List<AxisHandler> _axisHandlers;

    private void Awake()
    {
        _buttonHandlers = new List<ButtonHandler>();
        _axisHandlers = new List<AxisHandler>();
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

        foreach(ButtonHandler handler in _buttonHandlers)
        {
            if (Input.GetButtonDown(handler.ButtonName))
            {
                handler.OnButtonDown();
            }
            else if(Input.GetButton(handler.ButtonName))
            {
                handler.OnButton();
            }
            else if (Input.GetButtonUp(handler.ButtonName))
            {
                handler.OnButtonUp();
            }
        }

        foreach (AxisHandler handler in _axisHandlers)
        {
            if (Input.GetAxis(handler.AxisName) != 0)
            {
                handler.OnChange();
            }
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

    public void RegisterButtonHandler(ButtonHandler handler)
    {
        _buttonHandlers.Add(handler);
    }

    public void RegisterAxisHandler(AxisHandler handler)
    {
        _axisHandlers.Add(handler);
    }
}
