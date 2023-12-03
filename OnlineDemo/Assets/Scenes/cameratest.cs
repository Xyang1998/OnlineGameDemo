using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class cameratest : MonoBehaviour
{
    private Camera mainCamera;
    private float inputX;
    private float inputY;
    void Start()
    {
        Application.targetFrameRate = 60;
        mainCamera = Camera.main;
        Cursor.lockState=CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 forward = mainCamera.transform.forward;
        forward.y = 0;
        forward = Vector3.Normalize(forward);
        Vector3 right = mainCamera.transform.right;
        right.y = 0;
        right = Vector3.Normalize(right);
        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");
        Vector3 input = inputY * GameConfig.moveSpeed * forward + inputX * GameConfig.moveSpeed * right;
        Vector2 speed = new Vector2(input.z, input.x);
        Vector3 targetPos = new Vector3(transform.position.x + speed.y*Time.fixedDeltaTime, transform.position.y,
            transform.position.z + speed.x*Time.fixedDeltaTime);
        Vector3 moveDirection = targetPos - transform.position;
        moveDirection.Normalize();
        /*if (Vector3.Angle(transform.forward, moveDirection) > 0.1)
        {
            transform.forward = Vector3.Slerp(transform.forward, moveDirection, 0.1f);

        }*/

        transform.DOMove(targetPos, Time.fixedDeltaTime);
        //transform.position = targetPos;
    }
}
