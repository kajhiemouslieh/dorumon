﻿using System.Linq;
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using doru;
//using System.Net;

public class Menu : bs
{


    TimerA timer = new TimerA();
    
    public void Start()
    {
        
        //Network.Disconnect();
        string[] levels = new string[Application.levelCount-1];
        for (int i = 0; i < Application.levelCount - 1; i++)
            levels[i] = "Level " + (i + 1);
        _MyGui.levels = levels;
        _MyGui.curwindow = Wind.Menu;
        _MyGui.enabled = false;
    }

    void Update()
    {
        UpdateHostList();
        UpdateOther();
        timer.Update();
    }
    void UpdateHostList()
    {
        if (MasterServer.PollHostList().Length != 0)
        {
            timer.Clear();
            _MyGui.Show(Wind.PopUp);
            _MyGui.popupTitle = "Searching for games";
            Debug.Log("Host List Received");
            HostData[] hostData = MasterServer.PollHostList();
            string[] ips = hostData.SelectMany(a => a.ip).ToArray();
            _MyGui.popupText = "Server List Received " + hostData.Length + ", Connecting";
            Network.Connect(ips, 5300);
            for (int i = 0; i < hostData.Length; i++)
            {
                _MyGui.popupText = "Trying Connect to " + hostData[i].gameName;
                Network.Connect(hostData[i]);
            }
            SetTimeOut();
            MasterServer.ClearHostList();
            
        }
    }
    private void UpdateOther()
    {
         
        if (Input.GetKeyDown(KeyCode.Escape))
            _MyGui.enabled = !_MyGui.enabled;   
    }
    

    public void Action(MenuAction a)
    {
        Debug.Log("Action:" + a);
        if (a == MenuAction.StartServer)
        {
            HostGame();
        }
        if (a == MenuAction.JoinGame)
        {
            MasterServer.RequestHostList("Ropector");
            _MyGui.popupTitle = "Searching for games";
            _MyGui.popupText = "Searching for lan games";
            _MyGui.Show(Wind.PopUp);
            var ips = new List<string>();
            //foreach (IPAddress host in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            //{
            //    var s = host.ToString();
            //    s= s.Substring(0, s.LastIndexOf('.'))+".";
            for (int i = 0; i < 255; i++)
                ips.Add("192.168.30." + i);
            //}            
            Network.Connect(ips.ToArray(), 5300);
            SetTimeOut();

        }
    }

    private static void HostGame()
    {
        bool useNat = !Network.HavePublicAddress();
        Network.InitializeServer(32, 5300, useNat);
        MasterServer.RegisterHost("Ropector", _Loader.nick + "'s game", "Level " + _MyGui.SelectedLevel);
        _Loader.LoadLevel(_MyGui.SelectedLevel+1);
    }

    private void SetTimeOut()
    {
        timer.AddMethod(5000, delegate { _MyGui.popupText = "Connection failed"; timer.AddMethod(1000, delegate { _MyGui.Show(Wind.Menu); }); });
    }
    

}