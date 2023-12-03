using System;
using System.Collections;
using Cinemachine;
using DG.Tweening;
using UnityEngine;


public class Player :MonoBehaviour
{
        private bool isLocal=false;
        public bool Aiming;
        public int id;
        private float inputX;
        private float inputY;
        public bool isDead = false;
        public CinemachineFreeLook tpsCamera;
        public CinemachineVirtualCamera fpsCamera;
        public GameObject lookAt;
        public GameObject fpslookat;
        public Animator Animator;
        public Transform FPSCameraRotator;
        public Transform FPSCameraPoint;
        public Transform Spine;
        private Camera mainCamera;
        private GameController GameController;
        private AnimMoveInput _animMoveInput;
        public Vector3 LerpTargetPos;
        public int index1 ;
        public int index2 ;
        public int index3 ;
        public AnimController animController;
        public float mousePitchSensitivity=2f;//俯仰的鼠标灵敏度（绕X轴）
        public float mouseYawSensitivity=2f;//偏转的鼠标灵敏度（绕Y轴）
        public float pitchLimit = 30;//俯仰的最大角度
        public Vector3 Speed;
        private float PreHp;


        public void Start()
        {
                PreHp = -1;
                mainCamera=Camera.main;
  

        }

        //将角度限制在（-180，180）
        public float ClampAngle(float angle)
        {
                if ( angle<=180 && angle>=-180)
                {
                        return angle;
                }

                while (angle>180)
                {
                        angle -= 360;
                }

                while (angle<-180)
                {
                        angle += 360;
                }

                return angle;
        }
        
        public void SelfUpdate()
        {
                if (isLocal)
                {
                        if (!isDead)
                        {
                                if (Input.GetMouseButton(1))
                                {
                                        Aiming = true;
                                        Vector3 temp = mainCamera.transform.eulerAngles;
                                        Spine.localRotation = Quaternion.Euler(0, 0, -temp.x);
                                        Animator.transform.rotation = Quaternion.Euler(0, temp.y, 0);
                                        //Animator.transform.rotation = Quaternion.Euler(0, temp.y, 0);
                                }
                                else
                                {
                                        Aiming = false;
                                }

                                if (Input.GetMouseButtonDown(1))
                                {
                                        FPSCameraRotator.rotation = mainCamera.transform.rotation;

                                }
                                if (Input.GetMouseButton(1))
                                {
                                        fpsCamera.Priority = 2;
                                        tpsCamera.Priority = 1;
                                        float pitch = -Input.GetAxis("Mouse Y") * mousePitchSensitivity; //当前帧的俯仰值（变化的量）
                                        float yaw = Input.GetAxis("Mouse X") * mouseYawSensitivity; //当前帧的偏转值（变化的量）
                                        Vector3 rot = new Vector3(pitch, yaw, 0); //当前帧欧拉角发生的改变量
                                        Vector3 afterRot; //改变后的欧拉角
                                        afterRot.x = Mathf.Clamp(
                                                ClampAngle(FPSCameraRotator.localEulerAngles.x) + rot.x,
                                                -pitchLimit, pitchLimit);
                                        afterRot.y = FPSCameraRotator.transform.localEulerAngles.y + rot.y;
                                        afterRot.z = 0;
                                        FPSCameraRotator.localEulerAngles = afterRot;
                                }
                                else
                                {
                                        fpsCamera.Priority = 1;
                                        tpsCamera.Priority = 2;
                                }
                        }
                }

        }
        
        public void Init(PlayerState playerState,bool _islocal)
        {

                id = playerState.id;
                animController.selfid = id;
                _animMoveInput = new AnimMoveInput();
                isLocal = _islocal;
                animController.islocal = isLocal;
                index1=Animator.GetLayerIndex("LegsLayer");
                index2=Animator.GetLayerIndex("RifleActions");
                index3=Animator.GetLayerIndex("AimLayer");
                animController.index1 = index1;
                animController.index2 = index2;
                animController.index3 = index3;
                if (_islocal)
                {
                        fpsCamera = FindObjectOfType<CinemachineVirtualCamera>();
                        fpsCamera.transform.parent = FPSCameraPoint;
                        fpsCamera.transform.localPosition=Vector3.zero;
                        fpsCamera.LookAt = fpslookat.transform;
                        tpsCamera = FindObjectOfType<CinemachineFreeLook>();
                        tpsCamera.Follow = transform;
                        tpsCamera.LookAt = lookAt.transform;
                        GameController = FindObjectOfType<GameController>();
                        GameController.index1 = index1;
                        GameController.index2 = index2;
                        GameController.index3 = index3;

                }
                else
                {
                        Animator.SetLayerWeight(index1,1);
                        Animator.SetLayerWeight(index2,1);
                        Animator.SetLayerWeight(index3,0);
                }
      

                
        }

        public void UpdateSelf(PlayerState playerState,float time)
        {
                if (isLocal)
                {
                        if (playerState.hp < PreHp)
                        {
                                GameController.GenerateImpulseGetShoot();
                        }
                }
                if (PreHp<0)
                {
                        PreHp = playerState.hp;
                }
                else
                {
                        if (PreHp < 100 && playerState.hp >= 100)
                        {
                                animController.Respawn();
                        }

                        PreHp = playerState.hp;
                }
                

                if (playerState.hp > 0)
                {
                        Vector3 targetPos = new Vector3(playerState.pos.x, transform.position.y, playerState.pos.y);
                        _animMoveInput.xy = playerState.input;
                        Anim(_animMoveInput);
                        if (isLocal) //本地直接操作
                        {
                                if (!Aiming)
                                {
                                        if (Speed.sqrMagnitude != 0)
                                        {

                                                Animator.transform.forward =
                                                        Vector3.Slerp(Animator.transform.forward, Speed.normalized,
                                                                0.2f);
                                        }
                                        /*if (Animator.transform.rotation != playerState.rotation)
                                        {
                                                Animator.transform.DOKill();
                                                Animator.transform.DORotateQuaternion(playerState.rotation,0.1f);
                                        }*/

                                        /* if (targetPos != transform.position)
                                        {
                                                Vector3 moveDirection = targetPos - transform.position;
                                                moveDirection.Normalize();
                                                if (Vector3.Angle(Animator.transform.forward, moveDirection) > 0.1)
                                                {
                                                        Animator.transform.forward =
                                                                Vector3.Slerp(Animator.transform.forward, moveDirection, 0.5f);
                                                }
                                        }*/

                                }

                                //Debug.Log(Animator.transform.rotation);
                                transform.position = targetPos;
                                //Rigidbody.velocity=new Vector3(speed.)

                        }
                        else
                        {
                                OtherLerp(playerState);
                        }
                }
                else
                {
                        if (isLocal)
                        {
                                animController.Death();
                        }
                        else
                        {
                                animController.Death();
                        }
                }


        }

        public void OtherLerp(PlayerState playerState)
        {
                Vector3 curTargetPos=new Vector3(playerState.pos.x, transform.position.y, playerState.pos.y);
                if (curTargetPos != LerpTargetPos)
                {
                        transform.DOKill();
                        transform.position = LerpTargetPos;
                        transform.DOMove(curTargetPos, GameConfig.SeverUpdateTime);
                        //Quaternion temp = Quaternion.FromToRotation(transform.forward,
                        //        (curTargetPos - transform.position).normalized);
                        LerpTargetPos = curTargetPos;
                        Vector3 curPos = transform.position;
                        Vector3 moveDirection = new Vector3(curTargetPos.x - curPos.x,0,curTargetPos.z-curPos.z);
                        /*if (moveDirection != Vector3.zero)
                        {
                                moveDirection.Normalize();
                                if (Vector3.Angle(Animator.transform.forward, moveDirection) > 0.1)
                                {

                                        Animator.transform.forward =
                                                Vector3.Slerp(Animator.transform.forward, moveDirection, 0.5f);

                                }
                        }*/
                }
                if (Animator.transform.rotation != playerState.rotation)
                {
                        Animator.transform.DOKill();
                        Animator.transform.DORotateQuaternion(playerState.rotation,0.1f);
                }

                /*if (Animator.transform.rotation != playerState.rotation)
                {
                        Animator.transform.DOKill();
                        Animator.transform.DORotateQuaternion(playerState.rotation,0.1f);
                }*/
                
                
        }
        

        /// <summary>
        /// 状态机
        /// </summary>
        public void Anim(AnimInput animInput)
        {
                if (animInput.AnimInputType == AnimInputType.Move)
                {
                        AnimMoveInput temp=animInput as AnimMoveInput;
                        if (temp.xy != Vector2.zero)
                        {
                                Animator.SetFloat("Y",1.0f);
                        }
                        else
                        {
                                Animator.SetFloat("Y",0.0f);
                        }
                }

                
                
        }


        
}

public  class AnimInput
{
        public AnimInputType AnimInputType ;
}

public class AnimMoveInput :AnimInput
{
        public AnimMoveInput()
        {
                AnimInputType = AnimInputType.Move;
        }

        public Vector2 xy;
}

public enum AnimInputType
{
        Move,
        Shoot,
        Reload
}