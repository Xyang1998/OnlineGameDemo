using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Random = System.Random;


public class GameSocket //服务器端,每个玩家的连接
{
    public Socket socket;
    public Room whichRoom;
    public int playerid;
    public string buffer="";
    
    public void StartReceiving()
    {
        Task task = new Task(Receiving);
        task.Start();
    }
    public void Receiving()
    {
        byte[] data = new byte[1024 * 1024];
        while (socket.Connected)
        {
            int len=0;
            try
            {
                len = socket.Receive(data, 0, data.Length, SocketFlags.None);
            }
            catch (Exception e)
            {
                len=0;
            }

 
            string input = Encoding.Default.GetString(data, 0, len);
            input = buffer+input ;
            //Data._queue.Enqueue(input);
            //处理input转化为GameSystemInput
            string[] sp = input.Split("#end");
            for (int i = 0; i < sp.Length-1; i++)
            {
                string s = sp[i];
                if (s.Length != 0)
                {
                    ClientInput msgStruct = JsonUtility.FromJson<ClientInput>(s);
                    Debug.Log(msgStruct.data);

                    //当街接受到frameindex前的输入
                    if (msgStruct != null)
                    {
                        Room.playerLastSN[msgStruct.playerid] = msgStruct.frame;
                        GameSystemInput gameSystemInput;
                        if (msgStruct.InputType == InputType.Join)
                        {
                            PlayerJoin playerJoin = JsonUtility.FromJson<PlayerJoin>(msgStruct.data);
                            string rpc = whichRoom.JoinRoom(out playerid,playerJoin.name);
                            Send(rpc);

                        }
                        else if (msgStruct.InputType == InputType.Leave)
                        {
                            gameSystemInput = JsonUtility.FromJson<PlayerLeave>(msgStruct.data);
                            try
                            {
                                PlayerLeave playerLeave=gameSystemInput as PlayerLeave;
                                var playerstate = whichRoom._gameSystem._state.PlayerStates.First(s => s.id == playerLeave.id);
                                whichRoom.AddLog($"玩家ID:{playerLeave.id} 昵称:{playerstate.name} 离开了游戏");
                            }
                            catch (Exception e)
                            {
                            
                                whichRoom.AddLog(e.ToString());
                            }
                            whichRoom.ApplyInput(gameSystemInput);
                            
                        }
                        else if (msgStruct.InputType == InputType.Attack)
                        {
                            gameSystemInput = JsonUtility.FromJson<PlayerAttack>(msgStruct.data);
                            whichRoom.ApplyInput(gameSystemInput);
                        }
                        else if (msgStruct.InputType == InputType.Move)
                        {
                            gameSystemInput = JsonUtility.FromJson<PlayerMove>(msgStruct.data);
                            whichRoom.ApplyInput(gameSystemInput);
                        }
                        else if (msgStruct.InputType == InputType.Anim)
                        {
                            gameSystemInput = JsonUtility.FromJson<AnimGameSystemInput>(msgStruct.data);
                            whichRoom.ApplyInput(gameSystemInput);
                        }
                        else if (msgStruct.InputType == InputType.Damage)
                        {
                            gameSystemInput = JsonUtility.FromJson<DamageGameSystemInput>(msgStruct.data);
                            whichRoom.ApplyInput(gameSystemInput);
                        }
                    }
                }
                
            }
            buffer = sp[^1];

        }
    }
    public void Send(string s)
    {
        if (socket.Connected)
        {
            Debug.Log("send:"+s);
            byte[] bytes = Encoding.Default.GetBytes(s+"#end");
            socket.Send(bytes, 0, bytes.Length, SocketFlags.None);
        }
        
    }

    public void End()
    {
        socket.Close();
    }
}



public class Room
{
    public GameSystem _gameSystem;
    private Socket _socket;
    private int nextPlayerID=1;
    public List<GameSocket> ClientProxSocketList = new List<GameSocket>();
    public List<GameSystemInput> bufferInputs;
    private List<GameSystemInput> bufferInputs1;
    private List<GameSystemInput> bufferInputs2;
    private float lastSynTime = 0;
    public static Dictionary<int, int> playerLastSN;
    private TimePast timePast;
    private List<int> waitingRespawn;
    private Mutex UpdateMutex;
    private List<GameSocket> TickList; //掉线的
    public Queue<String> LogQueue;
    public bool isSucceed=false;
    private Thread Listeningthread;
    public void Listening()
    {
        while (true)
        {
            try
            {
                var proxSocket = _socket.Accept();
                GameSocket socketStruct=new GameSocket();
                proxSocket.Blocking = false;
                socketStruct.socket = proxSocket;
                socketStruct.whichRoom = this;
                //socketStruct.receiving.Start(proxSocket);
                ClientProxSocketList.Add(socketStruct);
                socketStruct.StartReceiving();
            }
            catch (Exception e)
            {
            }

        }
    }

    public void ApplyInput(GameSystemInput input)
    {
        bufferInputs.Add(input);
    }
    
    public Room(List<PhysicalCheck> list)
    {
        LogQueue = new Queue<string>();
        TickList = new List<GameSocket>();
        UpdateMutex = new Mutex();
        waitingRespawn = new List<int>();
        playerLastSN = new Dictionary<int, int>();
        _gameSystem = new GameSystem(list,true);
        _gameSystem.room = this;
        bufferInputs = new List<GameSystemInput>();
    }

    public bool Create(string ip,int port)
    {
        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(IPAddress.Parse( ip),port));
            _socket.Listen(10);
            Listeningthread= new Thread(Listening); //监听连接
            Listeningthread.Start();
            isSucceed = true;
            AddLog("创建房间成功!");
            return true;

        }
        catch (Exception e)
        {
            AddLog(e.ToString());
            isSucceed = false;
            return false;
        }
    }

    public void AddLog(string log)
    {
        LogQueue.Enqueue(log);
    }

    public void Update() //服务器端Tick
    {
        UpdateMutex.WaitOne();
            float curTime = GameConfig.GetCurrentTime();
            timePast = new TimePast();
            timePast.dtime = lastSynTime == 0 ? curTime : curTime - lastSynTime;
            lastSynTime = curTime;
            _gameSystem.ApplyInput(timePast);
            bufferInputs.Add(timePast);
            int num = bufferInputs.Count;
            for (int i = 0; i < num; i++)
            {
                GameSystemInput input = bufferInputs[i];
                _gameSystem.ApplyInput(input);
            }
            RespawnWait();
            ServerMsg serverMsg = new ServerMsg();
            serverMsg.MsgStructs = new List<MsgStruct>();
            for (int i = 0; i < num; i++)
            {
                GameSystemInput item = bufferInputs[i];
                MsgStruct msg = new MsgStruct();
                msg.data = JsonUtility.ToJson(item);
                if (item._inputType == InputType.Join)
                {
                        msg.InputType = InputType.Join;
                }
                else if (item._inputType == InputType.Leave)
                {
                        msg.InputType = InputType.Leave;
                        PlayerLeave playerLeave=item as PlayerLeave;
                        if (playerLeave!=null)
                        {
                            try
                            {
                                var socket = ClientProxSocketList.First(s => s.playerid == playerLeave.id);
                                ClientProxSocketList.Remove(socket);
                                socket.End();
                            }
                            catch (Exception e)
                            {
                                AddLog(e.ToString());
                            }

                        }


                }
                else if (item._inputType == InputType.Attack)
                {
                        msg.InputType = InputType.Attack;

                }
                else if (item._inputType == InputType.Move)
                {
                        msg.InputType = InputType.Move;
                }
                else if (item._inputType == InputType.TimePast)
                {
                        msg.InputType = InputType.TimePast;
                }
                else if (item._inputType == InputType.Anim)
                {
                        msg.InputType = InputType.Anim;
                }
                else if (item._inputType == InputType.Damage)
                {
                        msg.InputType = InputType.Damage;
                }
                else if (item._inputType == InputType.Respawn)
                {
                    msg.InputType = InputType.Respawn;
                }
                else if (item._inputType == InputType.Kill)
                {
                    msg.InputType = InputType.Kill;
                }

                serverMsg.MsgStructs.Add(msg);
                
            }
            bufferInputs.RemoveRange(0,num);
            for (int i = 0; i < ClientProxSocketList.Count; i++)
            {
                var ClientProxSocket = ClientProxSocketList[i];
                if (ClientProxSocket.socket.Connected)
                {
                    serverMsg.lastSynFrame = playerLastSN[ClientProxSocket.playerid];
                    string s = JsonUtility.ToJson(serverMsg);
                    ClientProxSocket.Send(s);
                }
                else
                {
                    TickList.Add(ClientProxSocket);
                }

            }

            foreach (var tick in TickList)
            {
                PlayerLeave playerLeave = new PlayerLeave();
                playerLeave.id = tick.playerid;
                var playerstate = _gameSystem._state.PlayerStates.First(s => s.id == playerLeave.id);
                AddLog($"玩家ID:{playerLeave.id} 昵称:{playerstate.name} 断开了连接");
                ApplyInput(playerLeave);
                tick.End();
                ClientProxSocketList.Remove(tick);
                
            }
            TickList.Clear();
        UpdateMutex.ReleaseMutex();

    }



    public string  JoinRoom(out int  id,string _name)
    {
        UpdateMutex.WaitOne();
        PlayerJoin playerJoin = new PlayerJoin(_name);
        playerJoin.id = nextPlayerID++;
        id = playerJoin.id;
        ApplyInput(playerJoin);
        playerLastSN.Add(playerJoin.id,0);
        PlayerConnect playerConnect = new PlayerConnect();
        playerConnect.id = playerJoin.id;
        playerConnect.GameSystemState = _gameSystem._state;
        string res=JsonUtility.ToJson(playerConnect);
        AddLog($"玩家ID:{playerJoin.id} 昵称:{playerJoin.name} 加入了游戏");
        UpdateMutex.ReleaseMutex();
        return res;

    }

    public void RespawnWait()
    {
        for (int i = 0; i < _gameSystem._state.PlayerStates.Count; i++)
        {
            PlayerState playerState = _gameSystem._state.PlayerStates[i];
            if (playerState.hp <= 0)
            {
                if (!waitingRespawn.Contains(playerState.id))
                {
                    Respawn(playerState.id);
                    waitingRespawn.Add(playerState.id);
                }
            }
        }
    }

    public async void Respawn(int id)
    {
        await Task.Delay(GameConfig.RespawnTime);
        PlayerRespawn playerRespawn = new PlayerRespawn();
        playerRespawn.id = id;
        bufferInputs.Add(playerRespawn);
        waitingRespawn.Remove(playerRespawn.id);
        
    }

    public void CloseRoom()
    {
        Listeningthread.Abort();
        foreach (var Client in ClientProxSocketList)
        {
            Client.End();
        }
        _socket.Close();
    }


    
   
}





[Serializable]
public class GameSystemState
{
    public float time;
    public List<PlayerState> PlayerStates;
    public List<KDState> KDStates;
    public int nextArrowID;

    public GameSystemState()
    {
        time = GameConfig.GetCurrentTime();
        PlayerStates = new List<PlayerState>();
        KDStates = new List<KDState>();

    }
   

   

}

public class MyPhysicalSystem
{
    private List<PhysicalCheck> needToChecks;

    public MyPhysicalSystem(List<PhysicalCheck> _needToChecks)
    {
        needToChecks = _needToChecks;
    }

    public bool IsCubeCollSimpleWithOut(Circle c,PhysicalCheck ignore)
    {
        foreach (var cube in needToChecks)
        {
            if (ignore != cube)
            {
                var r = cube as Rectangle;
                if (c.Check(r))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsCubeColl(Circle c,Vector2 oldPos,ref Vector3 input)
    {
        foreach (var cube in needToChecks)
        {
            var r = cube as Rectangle;
            if (c.Check(r))
            {
                c.pos = oldPos; //调整前位置
                if (c.Check(r)) //旧点也碰撞(卡在里面了)
                {
                    //STEP1旋转
                    float cos = Mathf.Cos(r.rad / 180 * 3.1415F);
                    float sin = Mathf.Sin(r.rad / 180 * 3.1415F);
                    Vector2 temp = new Vector2((c.pos.x - r.center.x) * cos - (c.pos.y - r.center.y) * sin + r.center.x,
                        (c.pos.x - r.center.x) * sin + (c.pos.y - r.center.y) * cos + r.center.y);
                    Vector2 test = temp;
                    if (temp.x > r.TopRight.x)
                    {
                        temp.x = r.TopRight.x + c.radius + 0.001f;
                    }
                    else if (temp.x < r.BottomLeft.x)
                    {
                        temp.x = r.BottomLeft.x - c.radius - 0.001f;
                    }
                    if (temp.y > r.TopRight.y)
                    {
                        temp.y = r.TopRight.y + c.radius + 0.001f;
                    }
                    else if (temp.y < r.BottomLeft.y)
                    {
                        temp.y = r.BottomLeft.y - c.radius - 0.001f;
                    }
                    Vector2 temp1 = new Vector2((temp.x - r.center.x) * cos + (temp.y - r.center.y) * sin + r.center.x,
                        -(temp.x - r.center.x) * sin + (temp.y - r.center.y) * cos + r.center.y); //调整后位置

                        input.x = (temp1.x - oldPos.x);
                        input.z = (temp1.y - oldPos.y);
                        input = input.normalized;
                        Circle tempC = new Circle(temp1, GameConfig.PlayerCapsuleRadius);
                        if (IsCubeCollSimpleWithOut(tempC, r))
                        {
                            input.x = 0;
                            input.z = 0;
                        }
                }
                else
                {

                    float lr = Mathf.Min(GameConfig.Point2SegmentDistance(oldPos, r.RealTopLeft, r.RealTopRight),
                        GameConfig.Point2SegmentDistance(oldPos, r.RealBottomLeft, r.RealBottomRight));
                    float tb = Mathf.Min(GameConfig.Point2SegmentDistance(oldPos, r.RealBottomLeft, r.RealTopLeft),
                        GameConfig.Point2SegmentDistance(oldPos, r.RealBottomRight, r.RealTopRight));
                    Vector2 temp = new Vector2(input.x, input.z).normalized;
                    Vector2 lrVector2 = (r.RealTopLeft - r.RealTopRight).normalized;
                    Vector2 tbVector2 = (r.RealTopRight - r.RealBottomRight).normalized;
                    if (lr < tb)
                    {

                        temp = Vector2.Dot(temp, lrVector2) * lrVector2;
                    }
                    else if (lr > tb)
                    {

                        temp = Vector2.Dot(temp, tbVector2) * tbVector2;
                    }
                    else
                    {
                        float tb_angle = Mathf.Abs(Vector2.Dot(temp, tbVector2));
                        float lr_angle = Mathf.Abs(Vector2.Dot(temp, lrVector2));
                        if (tb_angle < lr_angle)
                        {
                            temp = Vector2.Dot(temp, tbVector2) * tbVector2; //调整后输入
                            //Vector2 speed = temp*GameConfig.moveSpeed;
                            //Vector2 newPos = new Vector2(oldPos.x + speed.x * Time.fixedDeltaTime,
                            //    oldPos.y + speed.y * Time.fixedDeltaTime);

                        }
                        else if (tb_angle > lr_angle)
                        {
                            temp = Vector2.Dot(temp, lrVector2) * lrVector2;
                        }
                        else
                        {
                            temp.x = 0;
                            temp.y = 0;
                        }
                    }
                    input.x = temp.x;
                    input.z = temp.y;
                    Vector2 speed = new Vector2(input.x, input.z)*GameConfig.moveSpeed;
                    Vector2 newPos=new Vector2(oldPos.x + speed.x * Time.fixedDeltaTime,
                        oldPos.y +speed.y * Time.fixedDeltaTime);
                    Circle tempC = new Circle(newPos, GameConfig.PlayerCapsuleRadius);
                    if (IsCubeCollSimpleWithOut(tempC, r))
                    {
                        input.x = 0;
                        input.z = 0;
                    }
                    
                }

                return true;
            }
        }
        return false;
    }

    public bool IsPlayerColl(Circle player, List<Circle> others)
    {
        foreach (var other in others)
        {
            if (player.Check(other as Circle))
            {
                return true;
            }
        }
        return false;
    }
}


public class GameSystem
{
    public GameSystemState _state
    {
        get;
        private set;
    }

    private bool isServer = false;
    public Room room;

    public MyPhysicalSystem MyPhysicalSystem;

    public GameSystem(List<PhysicalCheck> list,bool _isServer=false)
    {
        isServer = _isServer;
        _state = new GameSystemState();
        MyPhysicalSystem = new MyPhysicalSystem(list);
    }

    public void Reset(GameSystemState state)
    { 
        _state = state;
    }
    public void ApplyInput(GameSystemInput input)
    {
        if (input._inputType == InputType.Move)
        {
            PlayerMove playerMove=input as PlayerMove;
            if (playerMove != null)
            {
                PlayerState player;
                try
                {
                    player = _state.PlayerStates.First(s => s.id == playerMove.id);
                    if (player.hp > 0)
                    {
                        player.pos.x += playerMove.speed.x * playerMove.dtime;
                        player.pos.y += playerMove.speed.y * playerMove.dtime;
                        player.rotation = playerMove.rotation;
                        player.input = playerMove.input;
                    }
                }
                catch (Exception e)
                {
 
                }

                //player.speed = playerMove.speed;
                //player.position = playerMove.pos;
 
            }
        }
        else if (input._inputType == InputType.Attack)
        {
            PlayerAttack playerAttack=input as PlayerAttack;
            if (playerAttack != null)
            {
                
            }
        }
        else if (input._inputType==InputType.Join) 
        {
            PlayerJoin playerJoin=input as PlayerJoin;
            if (playerJoin!=null)
            {
                PlayerState playerState = new PlayerState(playerJoin.id,playerJoin.name,playerJoin.randomPos,playerJoin.hp);
                KDState kdState = new KDState(playerJoin.id);
                _state.KDStates.Add(kdState);
                _state.PlayerStates.Add(playerState);
            }
        }
        else if (input._inputType == InputType.Leave)
        {
            PlayerLeave playerLeave=input as PlayerLeave;
            if (playerLeave != null)
            {
                try
                {
                    PlayerState player=_state.PlayerStates.First(s => s.id == playerLeave.id);
                    _state.PlayerStates.Remove(player);
                    KDState state=_state.KDStates.First(s => s.id == playerLeave.id);
                    _state.KDStates.Remove(state);

                }
                catch (Exception e)
                {
    
                }
                
            }
        }
        else if (input._inputType==InputType.TimePast)
        {
            TimePast timePast=input as TimePast;
            if (timePast != null)
            {

            }
        }
        else if (input._inputType == InputType.Damage)
        {
            DamageGameSystemInput damageGameSystemInput=input as DamageGameSystemInput;
            if (damageGameSystemInput!=null)
            {
                PlayerState player;
                try
                {
                    player = _state.PlayerStates.First(s => s.id == damageGameSystemInput.hitid);
                    if (player.hp > 0)
                    {
                        player.hp = Mathf.Clamp(player.hp - GameConfig.Damage, 0, 100);
                        if (isServer)
                        {
                            if (player.hp <= 0)
                            {
                                KillGameSystemInput killGameSystemInput = new KillGameSystemInput();
                                killGameSystemInput.id = damageGameSystemInput.id;
                                killGameSystemInput.hitid = damageGameSystemInput.hitid;
                                if (room != null)
                                {
                                    room.ApplyInput(killGameSystemInput);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    return;
                }
               
            }
        }
        else if (input._inputType == InputType.Respawn)
        {
            PlayerRespawn playerRespawn=input as PlayerRespawn;
            if (playerRespawn != null)
            {
                PlayerState player;
                try
                {
                    player = _state.PlayerStates.First(s => s.id == playerRespawn.id);
                    player.hp = 100f;
                    player.pos = playerRespawn.randomPos;
                }
                catch (Exception e)
                {

                }

  
            }
        }
        else if (input._inputType == InputType.Kill)
        {
            KillGameSystemInput killGameSystemInput=input as KillGameSystemInput;
            if (killGameSystemInput != null)
            {
                KDState killplayer;
                try
                {
                    killplayer = _state.KDStates.First(s => s.id == killGameSystemInput.id);
                    killplayer.kill += 1;
                }
                catch (Exception e)
                {

                }
                try
                {
                    killplayer = _state.KDStates.First(s => s.id == killGameSystemInput.hitid);
                    killplayer.dead += 1;
                }
                catch (Exception e)
                {

                }
            }
        }
        
    }


    
    
}

public enum InputType
{
    Move,
    Attack,
    Join,
    Leave,
    TimePast,
    Connect,
    Anim,
    Damage,
    Death,
    Respawn,
    Kill
}
[Serializable]
public abstract class GameSystemInput
{
    public InputType _inputType;
}
[Serializable]
public class PlayerMove : GameSystemInput
{
    public int id;
    public Vector2 speed;
    public Vector2 input;
    public Quaternion rotation;
    public float dtime;
    
    public PlayerMove()
    {
        _inputType = InputType.Move;
    }
}


//首次连接
[Serializable]
public class PlayerConnect : GameSystemInput
{
    public int id;
    public GameSystemState GameSystemState;

    public PlayerConnect()
    {
        _inputType = InputType.Connect;
    }
}

[Serializable]
public class PlayerAttack : GameSystemInput
{
    public int id;
    public Vector2 targetPos;
    public float targetTime;

    public PlayerAttack()
    {
        _inputType = InputType.Attack;
    }
}
[Serializable]
public class PlayerJoin : GameSystemInput
{
    public int id;
    public string name;
    public float hp;
    public Vector2 randomPos;

    public PlayerJoin(string _name)
    {
        name = _name;
        randomPos = GameConfig.SpawnPosList[GameConfig.Random.Next(0,GameConfig.SpawnPosList.Count)];
        _inputType = InputType.Join;
        hp = 100.0f;
    }
}
[Serializable]
public class PlayerLeave: GameSystemInput
{
    public int id;

    public PlayerLeave()
    {
        _inputType = InputType.Leave;
    }
}
[Serializable]
public class TimePast : GameSystemInput
{
    public float dtime;

    public TimePast()
    {
        _inputType = InputType.TimePast;
    }
}

[Serializable]
public class AnimGameSystemInput : GameSystemInput
{
    public int id;
    public AnimInputType animInputType;
    public AnimGameSystemInput()
    {
        _inputType = InputType.Anim;
    }
}
[Serializable]
public class DamageGameSystemInput : GameSystemInput
{
    public int id; //攻击者
    public int hitid; //被攻击者
    public DamageGameSystemInput()
    {
        _inputType = InputType.Damage;
    }
}

[Serializable]
public class KillGameSystemInput : GameSystemInput
{
    public int id; //攻击者
    public int hitid; //被攻击者
    public KillGameSystemInput()
    {
        _inputType = InputType.Kill;
    }
}

[Serializable]
public class PlayerRespawn : GameSystemInput
{
    public int id;
    public Vector2 randomPos;
    public PlayerRespawn()
    {
        randomPos = GameConfig.SpawnPosList[GameConfig.Random.Next(0,GameConfig.SpawnPosList.Count)];
        _inputType = InputType.Respawn;
    }
}


