using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;


//Client端
public class ClientGameManager
{
    
    public Socket _socket;
    public GameSystem gameSystem;
    private float lastRecvSetverStateTime = 0;
    public int selfPlayerId =-1;
    public int  lastSN;
    private GameSystemState lastServerState;
    private string buffer;
    private Mutex _mutex;
    public static Mutex playerStatesMutex;
    private GameController _gameController;
    public Queue<KillGameSystemInput> killQueue;

    //本地输入
    private List<ClientSelfMsg> pendingInputs;


    
    public void Receiving()
    {
        byte[] data = new byte[1024 * 1024*8];
        while (true)
        {
           // Debug.Log("receive");
            int len=0;
            try
            {
                len = _socket.Receive(data, 0, data.Length, SocketFlags.None);
            }
            catch (Exception e)
            {
                len=0;
                return;
            }
            string input = Encoding.Default.GetString(data, 0, len);
            
            Debug.Log("receive:"+input);
            SyncServer(input);
            if (len==0)
            {
                return;
            }
        }
    }

    public ClientGameManager(List<PhysicalCheck> list,GameController gameController)
    {
        killQueue = new Queue<KillGameSystemInput>();
        _gameController = gameController;
        _mutex = new Mutex();
        playerStatesMutex = new Mutex();
        gameSystem = new GameSystem(list);
        GameSystemState temp = new GameSystemState();
        temp.time = gameSystem._state.time;
        temp.nextArrowID = gameSystem._state.nextArrowID;
        temp.PlayerStates = new List<PlayerState>();
        foreach (var playerState in gameSystem._state.PlayerStates)
        {
            temp.PlayerStates.Add(DeepCopy.DeepCopyByReflect<PlayerState>(playerState));
        }
        temp.KDStates = new List<KDState>();
        foreach (var kdState in gameSystem._state.KDStates)
        {
            temp.KDStates.Add(DeepCopy.DeepCopyByReflect<KDState>(kdState));
        }

        lastServerState = temp;
        pendingInputs = new List<ClientSelfMsg>();
        lastSN = 0;
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    public bool JoinRoom(string _name,string ip,int port)
    {
        try
        {
            //_socket.Connect(new IPEndPoint(IPAddress.Parse("192.168.31.244"), 5000));
            _socket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
            PlayerJoin playerJoin = new PlayerJoin(_name);
            ClientInput clientInputStruct=new ClientInput();
            clientInputStruct.frame = 0;
            clientInputStruct.InputType = playerJoin._inputType;
            clientInputStruct.playerid = selfPlayerId;
            clientInputStruct.data = JsonUtility.ToJson(playerJoin);
            //.Log(clientInputStruct.data);
            byte[] bytes = Encoding.Default.GetBytes(JsonUtility.ToJson(clientInputStruct)+"#end");
            _socket.Send(bytes, 0, bytes.Length, SocketFlags.None);
            Task task = new Task(Receiving);
            task.Start();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void SendMsg(GameSystemInput clientInput,bool applySelf=true)
    {

        _mutex.WaitOne();
        Debug.Log("开始发送");
        if (!_socket.Connected)
        {
            return;
        }

        //用于发送服务器
        
        ClientInput clientInputStruct=new ClientInput();
        clientInputStruct.frame = lastSN;
        clientInputStruct.InputType = clientInput._inputType;
        clientInputStruct.playerid = selfPlayerId;
        clientInputStruct.data = JsonUtility.ToJson(clientInput);
        string msg = JsonUtility.ToJson(clientInputStruct);
        
        //用于自身和解
        ClientSelfMsg clientSelfMsg;
        clientSelfMsg.frame = clientInputStruct.frame;
        clientSelfMsg.Input = clientInput;
        
        
        
        
        //向服务器发送输入
        if (applySelf)
        {
            lock (pendingInputs)
            {
                pendingInputs.Add(clientSelfMsg);
            }
        }

        byte[] bytes= Encoding.Default.GetBytes(msg+"#end");
        _socket.Send(bytes, 0, bytes.Length, SocketFlags.None);
        //本地立即应用
        if (applySelf)
        {
            lock (gameSystem)
            {
                //Debug.Log($"应用帧{clientInputStruct.frame}");
                gameSystem.ApplyInput(clientInput);

            }
        }

        _mutex.ReleaseMutex();
        //Debug.Log($"发送帧{clientInputStruct.frame}");
        //Debug.Log($"发送帧后pendinginputs{pendingInputs.Count}");
        Debug.Log("结束发送");
       
        //Done
        
    }

    /// <summary>
    /// 接受到服务器的消息，同步到本地，实现回滚，预测+和解,1/serverframe 调用一次
    /// </summary>
    /// <param name="input"></param>
    public void SyncServer(string input)
    {
        Debug.Log("开始同步");
        input = buffer + input;
        string[] split = input.Split("#end");
        for(int i=0;i<split.Length-1;i++)
        {
            List<PlayerState> checkPre = new List<PlayerState>();
            string s = split[i];
            if (s.Length != 0)
            {
                ServerMsg serverMsg = JsonUtility.FromJson<ServerMsg>(s);
                if (serverMsg.MsgStructs.Count != 0)
                {
                    _mutex.WaitOne();
                    //权威输入
                    lock (gameSystem)
                    {
                     List<GameSystemInput> list = new List<GameSystemInput>();
                     int index = 0;
                     foreach (var item in serverMsg.MsgStructs)
                     {
                        GameSystemInput gameSystemInput;
                        if (item.InputType == InputType.Join)
                        {
                            gameSystemInput = JsonUtility.FromJson<PlayerJoin>(item.data);
                        }
                        else if (item.InputType == InputType.Leave)
                        {
                            gameSystemInput = JsonUtility.FromJson<PlayerLeave>(item.data);
                        }
                        else if (item.InputType == InputType.Attack)
                        {
                            gameSystemInput = JsonUtility.FromJson<PlayerAttack>(item.data);

                        }
                        else if (item.InputType == InputType.Move)
                        {
                            gameSystemInput = JsonUtility.FromJson<PlayerMove>(item.data);
                        }
                        else if(item.InputType==InputType.TimePast)
                        {
                            gameSystemInput = JsonUtility.FromJson<TimePast>(item.data);
                        }
                        else if(item.InputType==InputType.Anim)
                        {
                            gameSystemInput = JsonUtility.FromJson<AnimGameSystemInput>(item.data);
                        }
                        else if (item.InputType == InputType.Damage)
                        {
                            gameSystemInput = JsonUtility.FromJson<DamageGameSystemInput>(item.data);
                        }
                        else if (item.InputType == InputType.Respawn)
                        {
                            
                            gameSystemInput = JsonUtility.FromJson<PlayerRespawn>(item.data);
                        }
                        else if (item.InputType == InputType.Kill)
                        {
                            gameSystemInput = JsonUtility.FromJson<KillGameSystemInput>(item.data);
                            killQueue.Enqueue((KillGameSystemInput)gameSystemInput);
                        }
                        else
                        {
                            gameSystemInput = null;
                        }
                        list.Add(gameSystemInput);
                     }
                     //回滚
                        foreach (var playerState in gameSystem._state.PlayerStates)
                        {
                            checkPre.Add(DeepCopy.DeepCopyByReflect<PlayerState>(playerState));
                        }
                        playerStatesMutex.WaitOne();
                        gameSystem.Reset(lastServerState);
                        //权威状态计算
                        foreach (var systemInput in list)
                        {
                            if (systemInput != null)
                            {
                                if (systemInput._inputType != InputType.Anim)
                                {
                                    gameSystem.ApplyInput(systemInput);
                                }
                                else
                                {
                                    AnimGameSystemInput animGameSystemInput=systemInput as AnimGameSystemInput;
                                    if (animGameSystemInput != null)
                                    {
                                        if (animGameSystemInput.id != selfPlayerId)
                                        {
                                            if (GameConfig.AnimActionDict.ContainsKey(animGameSystemInput.id))
                                            {
                                                GameConfig.AnimActionDict[animGameSystemInput.id]
                                                    .Add(animGameSystemInput.animInputType);
                                            }
                                            else
                                            {
                                                GameConfig.AnimActionDict.Add(animGameSystemInput.id,
                                                    new List<AnimInputType>());
                                                GameConfig.AnimActionDict[animGameSystemInput.id]
                                                    .Add(animGameSystemInput.animInputType);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        GameSystemState temp = new GameSystemState();
                        temp.time = gameSystem._state.time;
                        temp.nextArrowID = gameSystem._state.nextArrowID;
                        temp.PlayerStates = new List<PlayerState>();
                        foreach (var playerState in gameSystem._state.PlayerStates)
                        {
                            temp.PlayerStates.Add(DeepCopy.DeepCopyByReflect<PlayerState>(playerState));
                        }
                        temp.KDStates = gameSystem._state.KDStates;
                        lastServerState = temp;
                        //与服务器同步的帧index
                        int lastSN1 = serverMsg.lastSynFrame;
                        //Debug.Log($"收到时帧数{lastSN}");
                        // Debug.Log($"收到{lastSN1}帧及之前的操作");
                        //Debug.Log($"收到了{list.Count}个操作");
                       // Debug.Log($"pendingInputs剩余{pendingInputs.Count}");
                        

                        //预测=权威状态+本地在同步帧后的输入

                        lock (pendingInputs)
                        {
                           // foreach (var t in pendingInputs)
                           // {
                            //Debug.Log(t.frame);
                       //     }
                            pendingInputs.RemoveAll(s => s.frame <= lastSN1);
                            //Debug.Log($"移除后pendingInputs剩余{pendingInputs.Count}");
                            for (int j = 0; j < pendingInputs.Count; j++)
                            {
                                var pendingInput = pendingInputs[j];
                                //Debug.Log($"应用帧{pendingInput.frame}");
                                gameSystem.ApplyInput(pendingInput.Input);
                            }
                        }
                    }
                    Debug.Log("结束同步");
                    _mutex.ReleaseMutex();
                    playerStatesMutex.ReleaseMutex();
                    List<PlayerState> checkAfter = new List<PlayerState>();
                    foreach (var playerState in gameSystem._state.PlayerStates)
                    {
                        checkAfter.Add(DeepCopy.DeepCopyByReflect<PlayerState>(playerState));
                    }

                    for (int x=0; x < checkPre.Count;x++)
                    {
                        PlayerState t1 = checkPre[x];
                        PlayerState t2 = checkAfter[x];
                        if (t1.input != t2.input)
                        {
                            Debug.Log(t1.input);
                            Debug.Log(t2.input);
                            Debug.Log("input不对");
                        }
                        if (t1.pos != t2.pos)
                        {
                            Debug.Log(t1.pos);
                            Debug.Log(t2.pos);
                            Debug.LogWarning("pos不对");
                        }
                        if (t1.speed != t2.speed)
                        {
                            Debug.Log(t1.speed);
                            Debug.Log(t2.speed);
                            Debug.Log("speed不对");
                        }
                        
                        
                    }
                }
                else
                {
                    PlayerConnect playerConnect = JsonUtility.FromJson<PlayerConnect>(s);
                    if (playerConnect.GameSystemState != null)
                    {
                        selfPlayerId = playerConnect.id;
                        gameSystem.Reset(playerConnect.GameSystemState);
                        lastServerState = playerConnect.GameSystemState;
                        lastRecvSetverStateTime = GameConfig.GetCurrentTime();
                        GameController.IsStart = true;
                    }
                }
            }
        }
        buffer = split[^1];


    }

    public void LocalTimePast()
    {
        TimePast timePast = new TimePast();
        timePast._inputType = InputType.TimePast;
        timePast.dtime = GameConfig.GetCurrentTime() - lastRecvSetverStateTime;
        lastRecvSetverStateTime = GameConfig.GetCurrentTime();
    }

    public bool LeaveRoom()
    {
        try
        {
            PlayerLeave playerLeave = new PlayerLeave();
            playerLeave.id = selfPlayerId;
            ClientInput clientInputStruct=new ClientInput();
            clientInputStruct.frame = lastSN;
            clientInputStruct.InputType = playerLeave._inputType;
            clientInputStruct.playerid = selfPlayerId;
            clientInputStruct.data = JsonUtility.ToJson(playerLeave);
            //.Log(clientInputStruct.data);
            byte[] bytes = Encoding.Default.GetBytes(JsonUtility.ToJson(clientInputStruct)+"#end");
            _socket.Send(bytes, 0, bytes.Length, SocketFlags.None);
            _socket.Close();
            return true;
        }
        catch (Exception e)
        {
            return false;
        }

        
    }
    
}
