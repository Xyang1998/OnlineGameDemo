using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SocketStruct
{
    public Socket socket;

    public void StartReceiving()
    {
        Task task = new Task(Receiving);
        task.Start();
    }
    public void Receiving()
    {
 
        byte[] data = new byte[1024 * 1024];
        while (true)
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
           
            if (len <= 0)
            {
                break;
            }
            string input = Encoding.Default.GetString(data, 0, len);
            Server.Instance.AddMessageToRoom(input);

        }



    }
}

public class Data
{
    public static  Queue<string> _queue=new Queue<string>();
}

[Serializable]
public class A
{
   public List<B> blist;

   public A()
   {
       blist = new List<B>();
   }
}

[Serializable]
public class B
{
    public InputType InputType;
    public float id;
}
public class Server : MonoBehaviour
{
    public InputField ip;
    public InputField port;
    public Transform content;
    private Socket _socket;
    private Text textPrefab;
    public List<SocketStruct> ClientProxSocketList = new List<SocketStruct>();
    private static Server instance;
    public static Server Instance
    {
        get
        {
            
            return instance;
        }
    }

    public void Listening()
    {
        AddToQueue("开始监听");
        while (true)
        {
            var proxSocket = _socket.Accept();
            SocketStruct socketStruct=new SocketStruct();
            socketStruct.socket = proxSocket;
            //socketStruct.receiving.Start(proxSocket);
            ClientProxSocketList.Add(socketStruct);
            socketStruct.StartReceiving();
            AddToQueue("连接!");
            SendMessageToClients("这是服务器给你的消息");
        }
    }

    public void RemoveSocket(Socket socket)
    {
        var Struct = Server.Instance.ClientProxSocketList.First(s => s.socket == socket);
        ClientProxSocketList.Remove(Struct);
        try
        {
            if (socket.Connected)
            {
                socket.Close(10);
            }
        }
        catch
        {
            
        }
    }


    
    public void CreateRoom()
    {
        
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.Bind(new IPEndPoint(IPAddress.Parse( "192.168.31.244"),int.Parse("5000")));
        _socket.Listen(10);
        Thread thread= new Thread(Listening);
        thread.Start();
        textPrefab = Resources.Load<Text>("Text");
    }

    public void Start()
    {
        instance = FindObjectOfType<Server>();
        Data._queue = new Queue<string>();
        A a = new A();
        B playerLeave1=new B();
        playerLeave1.InputType = InputType.Join;
        playerLeave1.id = 2;
        B playerLeave2=new B();
        playerLeave2.InputType = InputType.Attack;
        playerLeave2.id = 3;
        a.blist.Add(playerLeave1);
        a.blist.Add(playerLeave2);


    }

    public void Update()
    {
        if (Data._queue.Count != 0)
        {
            string temp = Data._queue.Dequeue();
            AddMessageToRoom(temp);
            SendMessageToClients(temp);
        }
        
    }

    public void FixedUpdate()
    {
        
    }

    public void AddToQueue(string text)
    {
        Data._queue.Enqueue(text);
    }
    public void AddMessageToRoom(string text)
    {
        Text textgo = Instantiate(textPrefab, content);
        textgo.text = text;

    }



    public void SendMessageToClients(string text)
    {
        byte[] bytes = Encoding.Default.GetBytes(text);
        foreach (var client in ClientProxSocketList)
        {
            client.socket.Send(bytes, 0, bytes.Length, SocketFlags.None);
        }
    }
    
    
}
