using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CameraController : MonoBehaviour
{
    public float CameraMoveSpeed = 5;

    private Vector3 _lastMousePosition = new Vector3();

    private void Start()
    {
        InputController.Instance.RegisterHandler(new DragHandler("MiddleMouse", 
            (handler, x) => { handler.IgnoreUI = false; }, 
            (handler, mousePosition, delta) =>
            {
                transform.position -= delta;
                handler.LastMousePosition = mousePosition - delta;
            },
            (handler,point) => { handler.IgnoreUI = true; }));

        var zoomHandler = new AxisHandler("Mouse ScrollWheel",
            (handler, delta) =>
            {
                Camera.main.transform.localPosition += Vector3.forward * delta * -10f;
            },false);

        InputController.Instance.RegisterHandler(zoomHandler);
    }

    void Update()
    {
        //arrow keys
        Vector3 cameraMove = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0) * CameraMoveSpeed;
        GetComponent<Rigidbody>().velocity = cameraMove;
    }
}
