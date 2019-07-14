using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Handler
{
    public bool Cancel = false;
}

public class ButtonHandler : Handler
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

public class HoverHandler : Handler
{
    public Action<HoverHandler, Vector3> OnHoverWarmupEnterCallBack;
    public Action<HoverHandler, Vector3> OnHoverWarmupExitCallBack;
    public Action<HoverHandler, Vector3> OnHoverStartCallBack;
    public Action<HoverHandler, Vector3> OnHoverCallBack;
    public Action<HoverHandler, Vector3> OnHoverEndCallBack;

    private float _warmupTimestamp;
    public float hoverWarmupTime;
    public bool IsCurrentlyWarmingup = false;

    public bool IsCurrentlyHovering = false;
    public Vector3 LastMousePosition;

    /// <summary>
    /// create a hover handler that triggers when the mouse is still for an amount of time
    /// </summary>
    /// <param name="onHoverWarmupEnter"></param>
    /// <param name="onHoverWarmupExit"></param>
    /// <param name="onHoverStart"></param>
    /// <param name="onHover"></param>
    /// <param name="onHoverEnd"></param>
    /// <param name="hoverWarmupTime"></param>
    public HoverHandler(Action<HoverHandler, Vector3> onHoverWarmupEnter,
        Action<HoverHandler, Vector3> onHoverWarmupExit,
        Action<HoverHandler, Vector3> onHoverStart, 
        Action<HoverHandler, Vector3> onHover, 
        Action<HoverHandler, Vector3> onHoverEnd,
        float hoverWarmupTime = 1f)
    {
        this.OnHoverWarmupEnterCallBack = onHoverWarmupEnter;
        this.OnHoverWarmupExitCallBack = onHoverWarmupExit;
        this.OnHoverStartCallBack = onHoverStart;
        this.OnHoverCallBack = onHover;
        this.OnHoverEndCallBack = onHoverEnd;
        this.IsCurrentlyHovering = false;
        this.IsCurrentlyWarmingup = false;
        this._warmupTimestamp = Time.time;
        this.hoverWarmupTime = hoverWarmupTime;
    }

    public virtual bool IsTemporarilyHovering()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return mousePos == LastMousePosition;
    }

    public virtual void OnHoverWarmupEnter()
    {
        //Debug.Log("warm");
        this._warmupTimestamp = Time.time;
        IsCurrentlyWarmingup = true;
        this.OnHoverWarmupEnterCallBack(this, Camera.main.ScreenToWorldPoint(Input.mousePosition));
    }

    //only called when warmup begins but is exited before hover starts
    public virtual void OnHoverWarmupExit()
    {
        //Debug.Log("hover warm exit");
        IsCurrentlyWarmingup = false;
        this.OnHoverWarmupExitCallBack(this, LastMousePosition);
    }

    public virtual void OnHoverStart()
    {
        //Debug.Log("start");
        IsCurrentlyWarmingup = false;
        this.IsCurrentlyHovering = true;
        this.OnHoverStartCallBack(this, Camera.main.ScreenToWorldPoint(Input.mousePosition));
    }

    public virtual void OnHover()
    {
        //Debug.Log("hover");
        this.OnHoverCallBack(this, Camera.main.ScreenToWorldPoint(Input.mousePosition));
    }

    public virtual void OnHoverEnd()
    {
        //Debug.Log("end");
        this.IsCurrentlyHovering = false;
        this.OnHoverEndCallBack(this, Camera.main.ScreenToWorldPoint(Input.mousePosition));
    }

    public bool IsDoneWarmingup()
    {
        return IsCurrentlyWarmingup && (Time.time - _warmupTimestamp > hoverWarmupTime);
    }
}

public class ConditionalHoverHandler : HoverHandler
{
    Func<ConditionalHoverHandler, bool> TemporaryHoverCondition;

    public ConditionalHoverHandler(
        Action<HoverHandler, Vector3> onHoverWarmupEnter,
        Action<HoverHandler, Vector3> onHoverWarmupExit, 
        Action<HoverHandler, Vector3> onHoverStart,
        Action<HoverHandler, Vector3> onHover, 
        Action<HoverHandler, Vector3> onHoverEnd,
        Func<ConditionalHoverHandler, bool> temporaryHoverCondition,
        float hoverWarmupTime = 1) 
        : base(onHoverWarmupEnter, onHoverWarmupExit, onHoverStart, onHover, onHoverEnd, hoverWarmupTime)
    {
        TemporaryHoverCondition = temporaryHoverCondition;
    }

    public override bool IsTemporarilyHovering()
    {
        return TemporaryHoverCondition(this);
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
    public OnClick OnUIClickDel;

    private List<ButtonHandler> _buttonHandlers;
    private List<AxisHandler> _axisHandlers;
    private List<HoverHandler> _hoverHandlers;

    private void Awake()
    {
        _buttonHandlers = new List<ButtonHandler>();
        _axisHandlers = new List<AxisHandler>();
        _hoverHandlers = new List<HoverHandler>();
        Instance = this;
    }
	
	// Update is called once per frame
	void Update ()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetButtonUp("Fire1"))
            {
                OnUIClickDel?.Invoke(mousePos);
            }
            
            return;
        }

        if (Input.GetButtonUp("Fire1"))
        {
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

        for (int i = 0; i < _buttonHandlers.Count; i++)
        {
            ButtonHandler handler = _buttonHandlers[i];
            if (handler.Cancel)
            {
                _buttonHandlers.RemoveAt(i);
                i--;
            }
        }

        foreach (AxisHandler handler in _axisHandlers)
        {
            if (Input.GetAxis(handler.AxisName) != 0)
            {
                handler.OnChange();
            }
        }

        foreach (HoverHandler handler in _hoverHandlers)
        {
            if (handler.IsTemporarilyHovering())
            {
                if (!handler.IsCurrentlyHovering)
                {
                    if(!handler.IsCurrentlyWarmingup)
                    {
                        handler.OnHoverWarmupEnter();
                    }
                    else if (handler.IsDoneWarmingup())
                    {
                        handler.OnHoverStart();
                    }
                }
                else if (handler.IsCurrentlyHovering)
                {
                    handler.OnHover();
                }
            }
            else if (handler.IsCurrentlyHovering)
            {
                handler.OnHoverEnd();
            }
            else
            {
                handler.OnHoverWarmupExit();
                handler.IsCurrentlyWarmingup = false;
            }

            handler.LastMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
    }
    
    public void RegisterOnClickCallBack(OnClick callback)
    {
        Debug.Log("registering click");
        OnClickDel += callback;
    }

    //use this so when you click on a ui element it cancles the previous click delegate
    public void RegisterOnClickCallBackWithUICancel(OnClick callback)
    {
        RegisterOnClickCallBack(callback);
        //need to wait for 1 frame so that if we happend to activate this register from a ui click, we dont immediatly cancel it
        StartCoroutine(RegisterOnClickCallBackWithUICancelHelper(callback));
    }

    private IEnumerator RegisterOnClickCallBackWithUICancelHelper(OnClick callback)
    {
        yield return new WaitForEndOfFrame();
        OnUIClickDel += x => { OnClickDel -= callback; };
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
    public void UnRegisterButtonHandler(ButtonHandler handler)
    {
        _buttonHandlers.Remove(handler);
    }

    public void RegisterAxisHandler(AxisHandler handler)
    {
        _axisHandlers.Add(handler);
    }

    public void RegisterHoverHandler(HoverHandler handler)
    {
        _hoverHandlers.Add(handler);
    }
}
