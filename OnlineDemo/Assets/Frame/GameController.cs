using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cinemachine;
using DG.Tweening;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    private ClientGameManager _clientGameManager;
    private Dictionary<int, Player> _players;
    public GameObject PlayerPrefab;
    private Camera mainCamera;
    private float inputX;
    private float inputY;
    private PlayerMove playerMove;
    private DamageGameSystemInput _damageGameSystemInput;
    public GameObject spawnPos;
    public CanvasGroup GameMenuGroup;
    public CanvasGroup JoinCanvasGroup;
    public CanvasGroup HPCanvasGroup;
    private PlayerState selfPlayerState;
    private Player selfPlayer;
    public HPBar hpBar;
    public static bool IsStart=false;
    public float mousePitchSensitivity=2f;//俯仰的鼠标灵敏度（绕X轴）
    public float mouseYawSensitivity=2f;//偏转的鼠标灵敏度（绕Y轴）
    public float pitchLimit = 30;//俯仰的最大角度
    private Quaternion rotation;
    private Vector3 input;
    private Circle temp;
    private bool canShoot=true;
    public int index1 ;
    public int index2 ;
    public int index3 ;
    private AnimGameSystemInput _animGameSystemInput;
    private Vector3 screenMidPos;
    private int hitid;
    private IEnumerator shootIEnumerator;
    public bool relaoding;
    private bool isMenuOpen=false;
    private  int curBullets=30;
    private  int reserveBullets = 90;

    public InputField PlayerName;
    public InputField IP;
    public InputField Port;
    
    public Transform ScoreTable;
    public Transform KillBoardCast;
    public SingleScore SingleScorePrefab;
    public KillMsg KillMsgPrefab;
    public CanvasGroup ScoreGroupCanvas;
    private Dictionary<int, SingleScore> SingleScores;
    private List<Player> needtoremove;
    KillGameSystemInput k ;
    private SingleScore s;
    private PlayerState kill;
    private PlayerState killed;
    CinemachineImpulseSource source;
    




    public GameObject prefab;

    public void GenerateImpulseShoot()
    {
        if (source)
        {
            source.m_DefaultVelocity = new Vector3(Random.Range(-0.01f, 0.01f), Random.Range(-0.01f, 0.01f), 0);
            source.GenerateImpulse();
        }
    }
    
    public void GenerateImpulseGetShoot()
    {
        if (source)
        {
            source.m_DefaultVelocity = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
            source.GenerateImpulse();
        }
    }


    public void Start()
    {
        source = GetComponent<CinemachineImpulseSource>();
        needtoremove = new List<Player>();
        SingleScores = new Dictionary<int, SingleScore>();
        temp = new Circle(new Vector2(0, 0), GameConfig.PlayerCapsuleRadius);
        Application.targetFrameRate = 50;
        _players = new Dictionary<int, Player>();
        _clientGameManager = new ClientGameManager(GameConfig.FindAllPhysicalChecks(prefab),this);
        mainCamera=Camera.main;
        playerMove = new PlayerMove();
        _damageGameSystemInput = new DamageGameSystemInput();
        screenMidPos = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0);
        
    }
    
    
    private void FixedUpdate()
    {
        if (IsStart)
        {
            if (_clientGameManager != null)
            {
                _clientGameManager.LocalTimePast();
                //输入
                if (selfPlayerState != null && selfPlayerState.hp > 0&&!isMenuOpen)
                {
                    Vector3 forward = mainCamera.transform.forward;
                    forward.y = 0;
                    forward = Vector3.Normalize(forward);
                    Vector3 right = mainCamera.transform.right;
                    right.y = 0;
                    right = Vector3.Normalize(right);
                    inputX = Input.GetAxisRaw("Horizontal");
                    inputY = Input.GetAxisRaw("Vertical");
                    input = inputY * forward + inputX *right;

                    input = input.normalized;
                    selfPlayer.Speed = new Vector3(input.x, 0, input.z);
                    playerMove.id = _clientGameManager.selfPlayerId;
                    playerMove.input = new Vector2(inputX, inputY);
                    playerMove.dtime = Time.fixedDeltaTime;
                    playerMove.rotation = selfPlayer.Animator.transform.rotation;
                    playerMove.speed = new Vector2(input.x, input.z)*GameConfig.moveSpeed;
                    Vector2 newPos = new Vector2(selfPlayerState.pos.x + playerMove.speed.x * playerMove.dtime,
                        selfPlayerState.pos.y + playerMove.speed.y * playerMove.dtime);
                    temp.pos = newPos;
                    if (_clientGameManager.gameSystem.MyPhysicalSystem.IsCubeColl(temp,new Vector2(selfPlayer.transform.position.x,selfPlayer.transform.position.z),ref input))
                    {
 
                        playerMove.speed =new Vector2(input.x, input.z)*GameConfig.moveSpeed;
                    }
                    

                    _clientGameManager.SendMsg(playerMove);

                    #region movetest

                    /*if (selfPlayer)
                    {
                        Vector2 speed = new Vector2(input.z, input.x);
                        Vector3 targetPos = new Vector3(selfPlayer.transform.position.x + speed.y * Time.deltaTime,
                            selfPlayer.transform.position.y,
                            selfPlayer.transform.position.z + speed.x * Time.deltaTime);
                        Vector3 moveDirection = targetPos - selfPlayer.transform.position;
                        moveDirection.Normalize();
                        if (Vector3.Angle(selfPlayer.Animator.transform.forward, moveDirection) > 0.1)
                        {
                            selfPlayer.Animator.transform.forward = Vector3.Slerp(selfPlayer.Animator.transform.forward, moveDirection, 0.1f);

                        }

                        selfPlayer.transform.position = targetPos;//DOMove(targetPos, Time.fixedDeltaTime);
                    }*/

                    #endregion
                }
                else
                {
                    if (selfPlayerState != null && selfPlayer && selfPlayerState.hp <= 0)
                    {
                        selfPlayer.isDead = true;
                    }
                }


            }
            
            if (selfPlayer)
            {
                if (selfPlayerState.hp > 0&&!isMenuOpen)
                {
                    //Quaternion tempRot = selfPlayer.Animator.transform.rotation;
                    if (Input.GetMouseButtonDown(1))
                    {
                        selfPlayer.FPSCameraRotator.rotation = mainCamera.transform.rotation;
                    }

                    if (Input.GetKey(KeyCode.R))
                    {
                        if (canShoot)
                        {
                            if (reserveBullets >= 1 && curBullets != 30)
                            {
                                selfPlayer.Animator.SetLayerWeight(index1, 0);
                                selfPlayer.Animator.SetLayerWeight(index2, 1);
                                selfPlayer.Animator.SetLayerWeight(index3, 1);
                                ReloadStart();
                                _animGameSystemInput.animInputType = AnimInputType.Reload;
                                _clientGameManager.SendMsg(_animGameSystemInput);
                            }
                        }
                    }

                    if (Input.GetMouseButton(0) && Input.GetMouseButton(1)) //射击
                    {
                        if (canShoot)
                        {
                            if (curBullets >= 1)
                            {
                                canShoot = false;
                                StartCoroutine(ShootCD());
                                //射线检测
                                hitid = Shoot();
                                if (hitid != -1)
                                {
                                    _damageGameSystemInput.id = _clientGameManager.selfPlayerId;
                                    _damageGameSystemInput.hitid = hitid;
                                    _clientGameManager.SendMsg(_damageGameSystemInput, false);
                                }
                                curBullets--;
                                hpBar.UpdateBullet(curBullets, reserveBullets);
                                selfPlayer.Animator.SetBool("Shoot", true);
                                _animGameSystemInput.animInputType = AnimInputType.Shoot;
                                _clientGameManager.SendMsg(_animGameSystemInput);
                                //发送操作
                            }
                        }
                    }
                    else
                    {
                        selfPlayer.Animator.SetBool("Shoot", false);
                    }

                    if (Input.GetMouseButton(1))
                    {
                        selfPlayer.fpsCamera.Priority = 2;
                        selfPlayer.tpsCamera.Priority = 1;
                        float pitch = -Input.GetAxis("Mouse Y") * mousePitchSensitivity; //当前帧的俯仰值（变化的量）
                        float yaw = Input.GetAxis("Mouse X") * mouseYawSensitivity; //当前帧的偏转值（变化的量）
                        Vector3 rot = new Vector3(pitch, yaw, 0); //当前帧欧拉角发生的改变量
                        Vector3 afterRot; //改变后的欧拉角
                        afterRot.x = Mathf.Clamp(
                            ClampAngle(selfPlayer.FPSCameraRotator.localEulerAngles.x) + rot.x,
                            -pitchLimit, pitchLimit);
                        afterRot.y = selfPlayer.FPSCameraRotator.transform.localEulerAngles.y + rot.y;
                        afterRot.z = 0;
                        selfPlayer.FPSCameraRotator.localEulerAngles = afterRot;
                    }
                    else
                    {
                        selfPlayer.tpsCamera.m_Transitions.m_InheritPosition = true;
                        selfPlayer.fpsCamera.Priority = 1;
                        selfPlayer.tpsCamera.Priority = 2;
                    }
                }
            }
            if (selfPlayer)
            {
                _clientGameManager.lastSN++;
            }
            UpdatePlayer();
        }
    }

    int  Shoot()
    {
        Ray OneShotRay = mainCamera.ScreenPointToRay(screenMidPos);          // 以屏幕中央点为原点，发射射线
        GenerateImpulseShoot();
        RaycastHit[] Hits = Physics.RaycastAll(OneShotRay, LayerMask.GetMask("Static", "Player"));
        for(int i=Hits.Length-1;i>=0;i--)
        {
            var hit = Hits[i];
            if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Static"))
            {
                break;
            }
            else if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Player hitPlayer = hit.transform.GetComponent<Player>();
                if(hitPlayer==selfPlayer){continue;}
                else
                {
                    
                    
                    return hitPlayer.id;
                }
            }
        }
        return -1;
    }

    public void Update()
    {


        if (selfPlayer)
        {
            if (selfPlayerState.hp > 0&&!isMenuOpen)
            {
                if (Input.GetMouseButton(1)) //tps->fps
                {
                    selfPlayer.Aiming = true;
                    selfPlayer.Animator.SetLayerWeight(index1, 0);
                    selfPlayer.Animator.SetLayerWeight(index2, 1);
                    selfPlayer.Animator.SetLayerWeight(index3, 1);
                    Vector3 temp = mainCamera.transform.eulerAngles;
                    selfPlayer.Spine.localRotation = Quaternion.Euler(selfPlayer.Spine.localRotation.x,
                        selfPlayer.Spine.localRotation.y, -temp.x);
                    selfPlayer.Animator.transform.rotation = Quaternion.Euler(0, temp.y, 0);
                    //Animator.transform.rotation = Quaternion.Euler(0, temp.y, 0);
                }
                else
                {
                    if (!relaoding)
                    {
                        selfPlayer.Animator.SetLayerWeight(index1, 1);
                        selfPlayer.Animator.SetLayerWeight(index2, 0);
                        selfPlayer.Animator.SetLayerWeight(index3, 0);
                    }

                    selfPlayer.Aiming = false;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isMenuOpen == false)
            {
                isMenuOpen = true;
                MenuShow();
                Cursor.lockState=CursorLockMode.None;
                
            }
            else
            {
                isMenuOpen = false;
                MenuHide();
                Cursor.lockState=CursorLockMode.Locked;
            }
        }

        if (Input.GetKey(KeyCode.Tab))
        {
            ScoreGroupCanvas.alpha = 1;
        }
        else
        {
            ScoreGroupCanvas.alpha = 0;
        }
    }


    private void UpdatePlayer()
    {
        //ClientGameManager.playerStatesMutex.WaitOne();
        var playerStates = _clientGameManager.gameSystem._state.PlayerStates;
        //ClientGameManager.playerStatesMutex.ReleaseMutex();
        for (int i = 0; i < playerStates.Count; i++)
        {
            Player player;
            PlayerState playerState = playerStates[i];
            if (playerState != null)
            {
                if (!_players.ContainsKey(playerState.id))
                {
                    player = Instantiate(PlayerPrefab).GetComponent<Player>();
                    player.Init(playerState, playerState.id == _clientGameManager.selfPlayerId);
                    player.transform.position=new Vector3(playerState.pos.x, player.transform.position.y+2, playerState.pos.y);
                    player.Animator.transform.rotation = playerState.rotation;
                    player.LerpTargetPos = player.transform.position;
                    _players.Add(playerState.id, player);
                    hpBar.UpdateBullet(curBullets,reserveBullets);
                    SingleScore singleScore = Instantiate(SingleScorePrefab, ScoreTable);
                    KDState kdState = _clientGameManager.gameSystem._state.KDStates.First(s => s.id == playerState.id);
                    singleScore.Init(playerState.name,kdState.kill,kdState.dead);
                    SingleScores.Add(kdState.id,singleScore);

                }

                if (playerState.id == _clientGameManager.selfPlayerId)
                {
                    selfPlayer = _players[playerState.id];
                    selfPlayerState = playerState;
                    hpBar.Updatehp(playerState.hp);
                    _animGameSystemInput = new AnimGameSystemInput();
                    _animGameSystemInput.id = _clientGameManager.selfPlayerId;

                }

                player = _players[playerState.id];
                player.UpdateSelf(playerState, _clientGameManager.gameSystem._state.time);
            }
        }
        foreach (var p in _players.Values.ToList())
        {
            try
            {
                PlayerState playerState = _clientGameManager.gameSystem._state.PlayerStates.First(s => s.id == p.id);
            }
            catch (Exception e)
            {
                needtoremove.Add(p);
            }
        } //退出房间
        foreach (var p in needtoremove)
        {
            _players.Remove(p.id);
            SingleScore singleScore = SingleScores[p.id];
            Destroy(singleScore.gameObject);
            Destroy(p.gameObject);
        }
        needtoremove.Clear();
        //更新KD
        while (_clientGameManager.killQueue.Count != 0)
        {
            k = _clientGameManager.killQueue.Dequeue();
            SingleScores.TryGetValue(k.id,out s);
            if (s)
            {
                s.AddKill();
            }            
            SingleScores.TryGetValue(k.hitid,out s);
            if (s)
            {
                s.AddDeath();
            }
            try
            {
                kill = playerStates.First(s => s.id == k.id);
                killed= playerStates.First(s => s.id == k.hitid);
                KillMsg killMsg = Instantiate(KillMsgPrefab, KillBoardCast);
                killMsg.Init(kill.name,killed.name);
                
            }
            catch
            {
                
            }

        }
        
    }

    public void JoinRoomButton()
    {
             
             bool res=false;
             try
             {
                 res=_clientGameManager.JoinRoom(PlayerName.text.Trim(),IP.text.Trim(),int.Parse(Port.text.Trim()));
             }
             catch (Exception e)
             {

             }
             if (res)
             {
                 Cursor.lockState=CursorLockMode.Locked;
                 JoinCanvasGroup.alpha = 0;
                 JoinCanvasGroup.interactable = false;
                 JoinCanvasGroup.blocksRaycasts = false;
                 HPCanvasGroup.alpha = 1;
             }
             else
             {
                 IP.text = "错误，请重试！";
             }
    }

    
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

    IEnumerator ShootCD()
    {
        yield return new WaitForSeconds(GameConfig.shootCD);
        canShoot = true;
    }

    public void PlayAnim(GameSystemInput gameSystemInput)
    {

        AnimGameSystemInput animInput=gameSystemInput as AnimGameSystemInput;
        if (animInput != null)
        {
            Player _player;
            _players.TryGetValue(animInput.id, out _player);
            if (_player)
            {
                if (_player.id != selfPlayer.id)
                {
                    AnimInput _animInput = new AnimInput();
                    _animInput.AnimInputType = animInput.animInputType;
                    _player.Anim(_animInput);

                }
            }
        }



    }

    public void ReloadStart()
    {
        relaoding = true;
        selfPlayer.Animator.SetBool("Reloading", true);
        canShoot = false;
    }

    public void ReloadFinish()
    {
        relaoding = false;
        canShoot = true;
        int temp = Mathf.Min(30 - curBullets,reserveBullets);
        curBullets += temp;
        reserveBullets -= temp;
        hpBar.UpdateBullet(curBullets,reserveBullets);

    }

    private void MenuShow()
    {
        GameMenuGroup.alpha = 1;
        GameMenuGroup.interactable = true;
        GameMenuGroup.blocksRaycasts = transform;
    }

    private void MenuHide()
    {
        GameMenuGroup.alpha = 0;
        GameMenuGroup.interactable = false;
        GameMenuGroup.blocksRaycasts = false;
    }

    public void LeaveButton()
    {
        if (_clientGameManager.LeaveRoom())
        {
            Application.Quit();
        }
    }
    
    
    
}