using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;



public class Client : MonoBehaviour
{
    public InputField ip;
    public InputField port;
    public InputField input;
    public Transform content;
    private Socket socket;
    private Text textPrefab;
    private Queue<string> _queue;

    public void Start()
    {
        _queue = new Queue<string>();
    }

    public void JoinRoom()
    {
        
        textPrefab = Resources.Load<Text>("Text");
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            socket.Connect(IPAddress.Parse("192.168.31.244"),Int32.Parse("5000") );
            Task task = new Task(Receiving);
            task.Start();
            AddMessageToRoom("连接成功");
        }
        catch (Exception e)
        {
            AddMessageToRoom("连接出错");
            
        }
        
    }
    public void Receiving()
    {
        Debug.Log(Thread.CurrentThread);
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
            _queue.Enqueue(input);
        }
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

    public void Update()
    {
        if (_queue.Count != 0)
        {
            string temp = _queue.Dequeue();
            AddMessageToRoom(temp);
        }
    }

    public void AddMessageToRoom(string text)
    {
        Text textgo = Instantiate(textPrefab, content);
        textgo.text = text;

    }
    
    public void SendMessageToServer()
    {
        byte[] bytes = Encoding.Default.GetBytes(input.text);
        socket.Send(bytes, 0, bytes.Length, SocketFlags.None);

    }
}
