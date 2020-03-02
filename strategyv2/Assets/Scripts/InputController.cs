using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum HandlerType
{
    Button, Hover, Axis
}

public abstract class Handler
{
    public bool IgnoreUI = true;
    public bool OnlyUI = false;

    public bool UseWorldCoordinates;

    public HandlerType HandlerType { protected set; get;}
    /// <summary>
    /// checks to make sure that the the handler with activate with the current ui flag
    /// </summary>
    /// <param name="isOverUI"></param>
    /// <returns></returns>
    public bool IsUIStateValid(bool isOverUI)
    {
        return !(IgnoreUI && isOverUI) && !(OnlyUI && !isOverUI);
    }
}

public class ButtonHandler : Handler
{
    public string ButtonName;
    public Action<ButtonHandler, Vector3> OnButtonDownCallBack;
    public Action<ButtonHandler, Vector3> OnButtonUpCallBack;

    public const string LeftClick = "Fire1";
    public const string RightClick = "Fire2";
    public const string MiddleMouse = "Fire3";

    public ButtonHandler(string buttonName,
        Action<ButtonHandler, Vector3> onButtonDown,
        Action<ButtonHandler, Vector3> onButtonUp,
        bool ignoreUI = true,
        bool onlyUi = false,
        bool useWorldCoordinates = true)
    {
        this.ButtonName = buttonName;
        this.OnButtonDownCallBack = onButtonDown;
        this.OnButtonUpCallBack = onButtonUp;
        this.IgnoreUI = ignoreUI;
        this.OnlyUI = onlyUi;
        this.UseWorldCoordinates = useWorldCoordinates;
        HandlerType = HandlerType.Button;
    }

    public virtual void OnButtonDown()
    {
        this.OnButtonDownCallBack(this, InputController.GetMousePosition2D(this.UseWorldCoordinates));
    }

    public virtual void OnButton()
    {}

    public virtual void OnButtonUp()
    {
        this.OnButtonUpCallBack(this, InputController.GetMousePosition2D(this.UseWorldCoordinates));
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
        HandlerType = HandlerType.Hover;
    }

    public virtual bool IsTemporarilyHovering()
    {
        Vector3 mousePos = InputController.GetMousePosition2D(this.UseWorldCoordinates);
        return mousePos == LastMousePosition;
    }

    public virtual void OnHoverWarmupEnter()
    {
        //Debug.Log("warm");
        this._warmupTimestamp = Time.time;
        IsCurrentlyWarmingup = true;
        this.OnHoverWarmupEnterCallBack(this, InputController.GetMousePosition2D(this.UseWorldCoordinates));
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
        this.OnHoverStartCallBack(this, InputController.GetMousePosition2D(this.UseWorldCoordinates));
    }

    public virtual void OnHover()
    {
        //Debug.Log("hover");
        this.OnHoverCallBack(this, InputController.GetMousePosition2D(this.UseWorldCoordinates));
    }

    public virtual void OnHoverEnd()
    {
        //Debug.Log("end");
        this.IsCurrentlyHovering = false;
        this.OnHoverEndCallBack(this, InputController.GetMousePosition2D(this.UseWorldCoordinates));
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

    public DragHandler(string buttonName,
        Action<ButtonHandler, Vector3> onButtonDown,
        Action<DragHandler, Vector3, Vector3> onButtonDrag,
        Action<ButtonHandler, Vector3> onButtonUp,
        bool ignoreUI = true,
        bool onlyUi = false,
        bool useWorldCoordinates = true)
        : base(buttonName, onButtonDown, onButtonUp, ignoreUI, onlyUi, useWorldCoordinates)
    {
        this.OnDragCallBack = onButtonDrag;
    }

    public override void OnButtonDown()
    {
        if(InputController.TryGetMousePosition2D(this.UseWorldCoordinates, out this.LastMousePosition))
        {
            this.IsCurrentlyDragging = true;
            base.OnButtonDown();
        }
    }

    public override void OnButton()
    {
        if (this.IsCurrentlyDragging)
        {
            var mousePosition = InputController.GetMousePosition2D(this.UseWorldCoordinates);
            var mouseDelta = mousePosition - this.LastMousePosition;
            //Debug.Log($"last {LastMousePosition}, current {mousePosition}, delta {mouseDelta}");

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

public class AxisHandler : Handler
{
    public string AxisName;
    public Action<AxisHandler, float> OnChangeCallBack;
    public AxisHandler(string axisName, Action<AxisHandler, float> onChangeCallBack, bool ignoreUI = true, bool onlyUi = false)
    {
        this.AxisName = axisName;
        this.OnChangeCallBack = onChangeCallBack;
        this.IgnoreUI = ignoreUI;
        this.OnlyUI = onlyUi;
        HandlerType = HandlerType.Axis;
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

    private List<ButtonHandler> _buttonHandlers;
    private List<AxisHandler> _axisHandlers;
    private List<HoverHandler> _hoverHandlers;

    private enum HandlerQueueAction
    {
        Add, Remove
    }

    private struct HandlerQueueItem
    {
        public HandlerQueueAction Action;
        public Handler Item;

        public HandlerQueueItem(HandlerQueueAction action, Handler item)
        {
            Action = action;
            Item = item;
        }
    }
    private List<HandlerQueueItem> m_HandlerQueue;

    private void Awake()
    {
        _buttonHandlers = new List<ButtonHandler>();
        _axisHandlers = new List<AxisHandler>();
        _hoverHandlers = new List<HoverHandler>();
        m_HandlerQueue = new List<HandlerQueueItem>();
        Instance = this;
    }
	
	// Update is called once per frame
	void Update ()
    {
        ProcessHandlerQueue();
        bool isOverUI = EventSystem.current.IsPointerOverGameObject();

        foreach(ButtonHandler handler in _buttonHandlers)
        {
            if (!handler.IsUIStateValid(isOverUI))
            {
                continue;
            }

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
            if (!handler.IsUIStateValid(isOverUI))
            {
                continue;
            }

            if (Input.GetAxis(handler.AxisName) != 0)
            {
                handler.OnChange();
            }
        }

        foreach (HoverHandler handler in _hoverHandlers)
        {
            if (!handler.IsUIStateValid(isOverUI))
            {
                continue;
            }

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

            handler.LastMousePosition = GetMousePosition2D(handler.UseWorldCoordinates);
        }

        ProcessHandlerQueue();
    }

    public static bool TryGetMouseWorldPosition(out Vector3 position)
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            position = hit.point;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    public static bool TryGetMousePosition2D(bool useWorldCoordinates, out Vector3 position)
    {
        if (useWorldCoordinates)
        {
            bool foundPoint = TryGetMouseWorldPosition(out position);
            position = new Vector3(position.x, 0, position.z);
            return foundPoint;
        }
        else
        {
            position = Input.mousePosition;
            return true;
        }
    }

    public static Vector3 GetMouseWorldPosition()
    {
        Vector3 position;
        TryGetMouseWorldPosition(out position);
        return position;
    }

    public static Vector3 GetMousePosition2D(bool useWorldCoordinates)
    {
        Vector3 position;
        TryGetMousePosition2D(useWorldCoordinates, out position);
        return position;
    }

    public void RegisterHandler(Handler handler)
    {
        m_HandlerQueue.Add(new HandlerQueueItem(HandlerQueueAction.Add, handler));
    }

    public void UnRegisterHandler(Handler handler)
    {
        m_HandlerQueue.Add(new HandlerQueueItem(HandlerQueueAction.Remove, handler));
    }

    /// <summary>
    /// all adds/removes to handlers are processed in here so that we never add/remove a handler while processing others
    /// </summary>
    protected void ProcessHandlerQueue()
    {
        foreach(var handler in m_HandlerQueue)
        {
            if(handler.Item == null)
            {
                continue;
            }

            if(handler.Action == HandlerQueueAction.Add)
            {
                switch (handler.Item.HandlerType)
                {
                    case HandlerType.Button:
                        RegisterButtonHandler(handler.Item as ButtonHandler);
                        break;
                    case HandlerType.Hover:
                        RegisterHoverHandler(handler.Item as HoverHandler);
                        break;
                    case HandlerType.Axis:
                        RegisterAxisHandler(handler.Item as AxisHandler);
                        break;
                }
            }
            else
            {
                switch (handler.Item.HandlerType)
                {
                    case HandlerType.Button:
                        UnRegisterButtonHandler(handler.Item as ButtonHandler);
                        break;
                    case HandlerType.Hover:
                        UnRegisterHoverHandler(handler.Item as HoverHandler);
                        break;
                    case HandlerType.Axis:
                        UnRegisterAxisHandler(handler.Item as AxisHandler);
                        break;
                }
            }
        }

        // TODO may need to clean handlers of any nulls (only do this if it becomes a problem)

        m_HandlerQueue.Clear();
    }

    // Button
    protected void RegisterButtonHandler(ButtonHandler handler)
    {
        _buttonHandlers.Add(handler);
    }

    protected void UnRegisterButtonHandler(ButtonHandler handler)
    {
        _buttonHandlers.Remove(handler);
    }

    // Axis
    protected void RegisterAxisHandler(AxisHandler handler)
    {
        _axisHandlers.Add(handler);
    }

    protected void UnRegisterAxisHandler(AxisHandler handler)
    {
        _axisHandlers.Remove(handler);
    }

    // Hover
    protected void RegisterHoverHandler(HoverHandler handler)
    {
        _hoverHandlers.Add(handler);
    }

    protected void UnRegisterHoverHandler(HoverHandler handler)
    {
        _hoverHandlers.Remove(handler);
    }
}
