﻿using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using doru;
#if UNITY_EDITOR && UNITY_STANDALONE_WIN
using gui = UnityEditor.EditorGUILayout;
#endif

public class Game : bs
{
    public hostDebug hostDebug;
    public AnimationCurve SpeedCurv;
    
    [FindAsset("Player")]
    public GameObject PlayerPrefab;
    [FindAsset("Zombie")]
    public Zombie ZombiePrefab;

    public new Player _PlayerOwn;
    public new Player _PlayerOther;
    internal bool singlePlayer;
    internal List<bs> networkItems = new List<bs>();
    internal List<Zombie> Zombies = new List<Zombie>();
    internal List<ZombieSpawn> ZombieSpawns = new List<ZombieSpawn>();
    internal TimerA timer = new TimerA();
    public override void Awake()
    {
        
        base.Awake();
        AddToNetwork();        
        //if (hostDebug == hostDebug.singlePlayer)
        //    Action(MenuAction.single);
    }
    
    void Start()
    {

        Screen.lockCursor = true;
        Network.Instantiate(PlayerPrefab, Vector3.zero, Quaternion.identity, (int)NetworkGroup.Player);
        _MenuGui.enabled = false;
    }
   
    void Update()
    {
        if (timer.TimeElapsed(2000) && Network.isServer && Zombies.Count < 10 && enableZombies)
        {
            var zsp = ZombieSpawns.Random();
            Network.Instantiate(ZombiePrefab, zsp.pos, zsp.rot, (int)NetworkGroup.Zombie);
        }
        if (DebugKey(KeyCode.Q))
        {
            Debug.Log("Disconnect");
            Network.Disconnect();
        }
        timer.Update();
    }
    public bool enableZombies;
#if UNITY_EDITOR && UNITY_STANDALONE_WIN
    public override void OnEditorGui()
    {
        //hostDebug = (hostDebug)UnityEditor.EditorGUILayout.EnumPopup(hostDebug);
        enableZombies = gui.Toggle("zombies", enableZombies);
        base.OnEditorGui();
    }
#endif
    void Action(MenuAction a)
    {
        if (a == MenuAction.wait)
        {
            Network.InitializeServer(2, 5300, !Network.HavePublicAddress());
            _Loader.WriteDebug("Waiting For Players");
            _MenuGui.enabled = false;
        }
        if (a == MenuAction.join)
        {
            _Loader.WriteDebug("Connecting");
            var ips = new List<string>();

            for (int i = 0; i < 255; i++)
                ips.Add("192.168.30." + i);
            Network.Connect(ips.ToArray(), 5300);
        }
        if (a == MenuAction.single)
        {
            singlePlayer = true;
            Network.InitializeServer(1, 5300, true);
        }
    }
    private void OnConnect()
    {
        _Loader.WriteDebug("Connected");
        var nws = networkItems.Where(a => a is Game).Union(networkItems);
        foreach (var n in nws)
            if (n != null)
                n.enabled = true;
    }
    void onDisconnect()
    {
        Application.LoadLevel(Application.loadedLevel);
    }
    void OnConnectedToServer() { OnConnect(); }
    void OnServerInitialized() { if (singlePlayer) OnConnect(); }
    void OnPlayerConnected() { OnConnect(); }
    void OnDisconnectedFromServer() { onDisconnect(); }
    void OnPlayerDisconnected() { onDisconnect(); }
    
    
    
}