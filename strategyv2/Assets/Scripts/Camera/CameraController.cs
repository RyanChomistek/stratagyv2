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
        InputController.Instance.RegisterButtonHandler(new DragHandler("MiddleMouse", 
            (handler, x) => { handler.IgnoreUI = false; }, 
            (handler, mousePosition, delta) =>
            {
                if (Camera.main.transform.position != transform.position)
                {
                    transform.position = Camera.main.transform.position;
                }
                transform.position -= delta;
                handler.LastMousePosition = mousePosition - delta;
            },
            (handler,point) => { handler.IgnoreUI = true; }));

        var zoomHandler = new AxisHandler("Mouse ScrollWheel",
            (handler, delta) =>
            {
                GetComponent<Cinemachine.CinemachineVirtualCamera>().m_Lens.OrthographicSize += delta * -2;
            },false);
        InputController.Instance.RegisterAxisHandler(zoomHandler);
    }

    void Update()
    {
        //arrow keys
        Vector3 cameraMove = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0) * CameraMoveSpeed;
        GetComponent<Rigidbody>().velocity = cameraMove;

       
    }
}
