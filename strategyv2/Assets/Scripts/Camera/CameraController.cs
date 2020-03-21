using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CameraController : MonoBehaviour
{
    public float CameraMoveSpeed = 5;

    // rotations
    public float CameraRotateSensitivity = .25f;
    public float MaxYAngle = 80f;
    private Vector2 CurrentRotation;

    private Vector3 _lastMousePosition = new Vector3();

    public float CameraZoomSensitivity = 10f;
    private void Start()
    {
        InputController.Instance.RegisterHandler(new DragHandler("MiddleMouse",
            (handler, x) => { handler.IgnoreUI = false; },
            (handler, mousePosition, delta) =>
            {
                transform.position -= delta;
                handler.LastMousePosition = mousePosition - delta;
            },
            (handler, point) => { handler.IgnoreUI = true; }));

        InputController.Instance.RegisterHandler(new DragHandler("Fire2",
            (handler, x) => { handler.IgnoreUI = false; },
            (handler, mousePosition, delta) =>
            {
                UpdateRotation(delta);
            },
            (handler, point) => { handler.IgnoreUI = true; },
            useWorldCoordinates:false));

        InputController.Instance.RegisterHandler(new AxisHandler("Mouse ScrollWheel",
            (handler, delta) =>
            {
                //delta *= 1 + Camera.main.transform.position.y;
                //Camera.main.transform.localPosition += Vector3.forward * delta * CameraZoomSensitivity;
                delta *= Camera.main.transform.position.y * CameraZoomSensitivity;

                transform.position += transform.forward * delta;

            }, false));

        UpdateRotation(Vector3.forward);
    }

    private void UpdateRotation(Vector3 delta)
    {
        //Debug.Log(delta);
        //transform.Rotate(new Vector3(-delta.y, delta.x), Space.Self);

        CurrentRotation.x += delta.x * CameraRotateSensitivity;
        CurrentRotation.y -= delta.y * CameraRotateSensitivity;
        CurrentRotation.x = Mathf.Repeat(CurrentRotation.x, 360);
        CurrentRotation.y = Mathf.Clamp(CurrentRotation.y, -MaxYAngle, MaxYAngle);
        transform.rotation = Quaternion.Euler(CurrentRotation.y, CurrentRotation.x, 0);
    }

    void Update()
    {
        //arrow keys
        Vector3 cameraMove = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0) * CameraMoveSpeed;
        GetComponent<Rigidbody>().velocity = cameraMove;
    }
}
