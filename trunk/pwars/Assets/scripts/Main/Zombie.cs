using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;
public enum ZombieType { Normal, Speed, Life }
public class Zombie : Destroible
{
    public ZombieType[] priority = new ZombieType[] { 0, 0, 0, 0, 0, 0, ZombieType.Life, ZombieType.Speed, ZombieType.Speed, ZombieType.Speed };
    public ZombieType zombieType;
    public float zombieBite;
    public float speed = .3f;
    public float up = 1f;
    float seekPathtm;
    public bool move;
    [FindAsset("scream")]
    public AudioClip[] screamSounds;
    [FindAsset("gib")]
    public AudioClip[] gibSound;
    [FindAsset("Zombie")]
    public AudioClip[] ZombieSound;
    [FindTransform("zombieAlive")]
    public GameObject AliveZombie;
    [FindTransform("zombieDead")]
    public GameObject DeadZombie;
    public Seeker seeker;
    float zombieBiteDist = 3;
    Vector3[] pathPoints;
    public Vector3 oldpos;
    public AnimationCurve zombieSpeedCurve;
    public AnimationCurve zombieLifeCurve;
    public override void Init()
    {        
        base.Init();
        seeker = this.GetComponent<Seeker>();
        
        if (seeker == null) Debug.Log("Could not find seeker");
        velSync = angSync = true;
        posSync = rotSync = true;
        updateLightmapInterval = 500;
        
    }
    public override void Awake()
    {
        seeker.debugPath = _Loader.debugPath;
        base.Awake();        
    }
    protected override void Start()
    {        
        ResetSpawnTm();
        _Game.zombies.Add(this);
        base.Start();

    }
    public void CreateZombie(int stage)
    {   
        zombieType = priority.Random();
        var speed = zombieSpeedCurve.Evaluate(stage) * mapSettings.zombieSpeedFactor;
        speed = Random.Range(speed, speed / 3 * 2);
        var life = zombieLifeCurve.Evaluate(stage)*mapSettings.zombieLifeFactor;
        life = Random.Range(life, life / 3 * 2);
        if (zombieType == ZombieType.Life) life *= 2;
        if (zombieType == ZombieType.Speed) { speed *= 1.3f; life *= .7f; }
        RPCSetup(speed, life, (int)zombieType);
    }
    public void RPCSetup(float zombiespeed, float zombieLife, int priority) { CallRPC("Setup", zombiespeed, zombieLife, priority); }
    [RPC]
    public void Setup(float zombiespeed, float zombieLife, int priority)
    {
        Alive = true;
        Sync = true;
        ResetSpawn();
        SetLayer(gameObject);
        _TimerA.AddMethod(UnityEngine.Random.Range(0, 1000), PlayRandom);
        AliveZombie.renderer.enabled = true;
        DeadZombie.renderer.enabled = false;
        zombieType = (ZombieType)priority;
        CanFreeze = zombieType != ZombieType.Life;
        speed = zombiespeed;        
        maxLife =Life = zombieLife;
        transform.localScale = Vector3.one * Math.Min(Mathf.Max(zombieLife / 300f, 1f), 3);        
    }
    [RPC]
    public override void Die(int killedby)
    {
        if (!Alive) { return; }        
        Sync = false;
        Alive = false;
        SetLayer(LayerMask.NameToLayer("HitLevelOnly"));        
        if (Game.sendto != null)
            PlayRandSound(gibSound);
        AliveZombie.renderer.enabled = false;
        DeadZombie.renderer.enabled = true;
        if (killedby == _localPlayer.OwnerID)
            _localPlayer.AddFrags(1, mapSettings.PointsPerZombie);
    }
    public float tiltTm;
    public float spawninTM;
    public new Quaternion rot;
    protected override void Update()
    {
        base.Update();
        if(isController)
            if (rigidbody.velocity.magnitude > 5 * transform.localScale.x || Physics.gravity != _Game.gravity || Time.timeScale != 1) RPCSetFrozen(true);

        zombieBite += Time.deltaTime;
        seekPathtm -= Time.deltaTime;
        if (!Alive || selected == -1 || frozen) return;
        var ipl = Nearest();
        if (ipl != null)
        {
            Vector3 pathPointDir;
            Vector3 zToPlDir = ipl.transform.position - pos;
            pathPointDir = (GetRay(ipl) ?? GetPlayerPathPoint(ipl) ?? GetNextPathFindPoint(ipl) ?? default(Vector3));
            if (pathPointDir == default(Vector3))
            {
                move = false;
                tiltTm += Time.deltaTime;
            }
            else
            {
                Debug.DrawLine(pos, pos + pathPointDir);
                pathPointDir.y = 0;
                rot = Quaternion.LookRotation(pathPointDir.normalized);
                if (zToPlDir.magnitude > zombieBiteDist)
                {
                    move = true;
                    tiltTm += Time.deltaTime;
                }
                else
                    Bite(ipl);
            }
        }
        else
        {
            move = false;
            tiltTm = 0;
        }
        
        if (tiltTm > spawninTM && isController)
        {
            tiltTm = 0;
            if (Vector3.Distance(oldpos, pos) / spawninTM < .3f)
                ResetSpawn();
            oldpos = pos;
        }

    }
    private void Bite(Destroible ipl)
    {
        move = false;
        tiltTm = 0;
        if (zombieBite > 1)
        {
            zombieBite = 0;
            PlayRandSound(screamSounds);
            if ((build || ipl is Tower) && isController) ipl.RPCSetLife(ipl.Life - Math.Min(mapSettings.ZombieDamage, _Game.stage + 1), -1);
        }
    }
    public float pwait;
    private Vector3? GetRay(Destroible ipl)
    {        
        var r = new Ray(pos, ipl.pos - pos);
        //Debug.Log(pwait);
        RaycastHit h;
        pwait += Time.deltaTime;
        if (Physics.Raycast(r, out h, Vector3.Distance(ipl.pos, pos), 1 << LayerMask.NameToLayer("Level")))
        {
            pwait = 0;
            //Debug.Log(h.transform.gameObject.name);
        }        
        if (pwait > 3)
            return ipl.pos - pos;
        else
            return null;

    }
    Destroible nearest;
    private Destroible Nearest()
    {
        if(nearest == null || _TimerA.TimeElapsed(100))
            //_Game.towers.Where(a => a != null && Vector3.Distance(a.pos, pos) < 10).Cast<Destroible>().Union(players)
            nearest = players.Where(a => a != null && a.Alive).OrderBy(a => Vector3.Distance(a.pos, pos)).FirstOrDefault();
        return nearest;
    }
    void FixedUpdate()
    {
        if (Alive && !frozen)
        {
            transform.rotation = rot;
            if (move)
            {
                Vector3 v = rigidbody.velocity;
                v.x = v.z = 0;
                rigidbody.velocity = v;
                rigidbody.angularVelocity = Vector3.zero;
                var t = rot * new Vector3(0, 0, speed * Time.deltaTime * Time.timeScale * Time.timeScale * rigidbody.mass);
                Ray r = new Ray(pos,t);
                if (Physics.Raycast(r, .3f, 1 << LayerMask.NameToLayer("Level")))
                    t.y++;
                pos += t;                
            }
        }
    }
    private Vector3? GetPlayerPathPoint(Destroible ipl)
    {        
        if (ipl is Player)
        {
            var pathPoints = ((Player)ipl).plPathPoints;
            var np = FindNextPoint(pathPoints);
            return np;
        }
        return null;
    }
    private Vector3? FindNextPoint(IList<Vector3> points)
    {
        if (points == null || points.Count == 0) return null;
        Vector3 nearest = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        bool found = false;
        int ni = 0;
        for (int i = 0; i < points.Count; i++)
        {
            //if (i != 0) //Debug.DrawLine(points[i - 1], points[i]);
            Vector3 newp = points[i] - pos;
            if (newp.magnitude < nearest.magnitude)
            {
                nearest = newp;
                ni = i;
                if (nearest.magnitude < 6)
                    found = true;
            }
        }
        if (found)
            while(true)
            {
                ni++;
                if (ni >= points.Count) break;
                if (Vector3.Distance(points[ni], pos) > 3)
                    return points[ni] - pos;
            }

        return null;
    }
    private Vector3? GetNextPathFindPoint(Destroible ipl)
    {
        if (_Loader.disablePathFinding) return null;
        if (seekPathtm < 0)
        {
            seeker.StartPath(this.transform.position, ipl.transform.position);
            seekPathtm = UnityEngine.Random.Range(3f, 6);
        }
        return FindNextPoint(pathPoints);
        
    }
    void PathComplete(Vector3[] points)
    {
        pathPoints = points;
    }
    public override void RPCSetFrozen(bool value)
    {
        base.RPCSetFrozen(value);
    }
    [RPC]
    public override void SetFrozen(bool value)
    {
        tiltTm = 0;
        base.SetFrozen(value);
    }
    private void PlayRandom()
    {
        if (this != null && Alive)
        {
            _TimerA.AddMethod(UnityEngine.Random.Range(5000, 50000), PlayRandom);            
            PlayRandSound(ZombieSound);
            
        }
    }
    public override void ResetSpawn()
    {
        ResetSpawnTm();
        MapTag[] gs = _Game.spawns.Where(a => a.SpawnType.ToLower() == "zombie").ToArray();        
        Destroible pl = Nearest();
        if (pl == null)
        {
            pos = gs.First().transform.position;
        }
        else
        {
            //var neargs  = gs.Where(a => Vector3.Distance(a.transform.position, pl.pos) < 100 && Math.Abs(a.transform.position.y - pl.pos.y) < 3).ToList();
            var b = gs.Where(a => a.collider == null || a.collider.bounds.Contains(pl.pos)).Random();
            var o = gs.OrderBy(a => Vector3.Distance(a.transform.position, pl.pos));
            pos = (b ?? o.FirstOrDefault(a => Math.Abs(a.transform.position.y - pl.pos.y) < 3) ?? o.First()
                ).transform.position;
        }
        rot = Quaternion.identity;
        rigidbody.velocity = Vector3.zero;
    }
    public void ResetSpawnTm()
    {
        spawninTM = Random.Range(5f, 20f);
    }
    protected override void OnCollisionEnter(Collision collisionInfo)
    {
        base.OnCollisionEnter(collisionInfo);
    }
    public override void OnPlayerConnectedBase(NetworkPlayer np)
    {
        base.OnPlayerConnectedBase(np);
        RPCSetup((float)speed, (float)maxLife, (int)zombieType);
        if (!Alive)
        {
            Debug.Log("send zombie rpc die" + np);
            RPCDie(-1);
        }
    }
    public override void RPCSetLife(float NwLife, int killedby)
    {
        base.RPCSetLife(NwLife, killedby);
    }
    [RPC]
    public override void SetLife(float NwLife, int killedby)
    {
        base.SetLife(NwLife, killedby);
    }
    
    
}
