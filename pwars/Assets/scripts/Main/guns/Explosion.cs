using UnityEngine;
using System.Collections;

public class Explosion : Base
{
    public Box self;
    public int damage;
    public float exp = 500;
    public float radius = 4;
    void Start()
    {
        damage = 61;
        foreach (IPlayer ip in GameObject.FindObjectsOfType(typeof(IPlayer)))
        {            
            
            float dist = Vector3.Distance(ip.transform.position, transform.position);
            if (ip != self && dist < radius && ip.isController && !ip.dead)
            {                
                if (ip.isOwner)
                    _Cam.exp = 1;                
                ip.RPCSetLife(ip.Life - damage,OwnerID);
            }
        }
        foreach (Box b in GameObject.FindObjectsOfType(typeof(IPlayer)))
            if (b != self)
            {
                b.rigidbody.AddExplosionForce(exp, transform.position, radius);
            }
        foreach (Fragment f in FindObjectsOfType(typeof(Fragment)))
        {
            if(f!=null)
                f.Explosion(transform.position, exp, radius);
        }
    }
}