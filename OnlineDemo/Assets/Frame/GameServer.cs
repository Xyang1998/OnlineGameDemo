using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class GameServer : MonoBehaviour
{
    public InputField IPInputField;
    public InputField PortInputField;
    public Text LogPrefab;
    public Transform LogContext;
    private Room _room;
    public List<PhysicalCheck> PhysicalChecks;
    private int MsgCount;

    public void Start()
    {
        PhysicalChecks = new List<PhysicalCheck>();
    }

    

    public void CreateRoom()
    {
        if (_room == null)
        {
            _room = new Room(PhysicalChecks);
        }

        if (_room.isSucceed)
        {
            AddLog("房间已创建，无法重复！");
        }
        else
        {
            
            if (_room.Create(IPInputField.text.Trim(),int.Parse(PortInputField.text.Trim())))
            {
                StartCoroutine("SeverUpdate");
            }
            else
            {
                IPInputField.text = "IP或端口号错误！";
            }
        }

    }

    public void CloseRoom()
    {
        _room.CloseRoom();
        _room = null;
        AddLog("关闭房间");
    }



    public void AddLog(string s)
    {
        Text text = Instantiate(LogPrefab, LogContext);
        text.text = s;
    }

    IEnumerator SeverUpdate()
    {
        while (true)
        {
            _room.Update();
            if (_room.LogQueue.Count != 0)
            {
                MsgCount = _room.LogQueue.Count;
                for (int i = 0; i < MsgCount; i++)
                {
                    string log = _room.LogQueue.Dequeue();
                    AddLog(log);
                }
            }
            yield return new WaitForSeconds(GameConfig.SeverUpdateTime);
        }
    }


    

    
}



