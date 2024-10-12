using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

public class Wendigo : MonsterBase
{
    
    public enum WendigoState
    {
        Idle,
        Stalking,
        Searching,
        Chasing,
        Grabbing,
        HitStunned,
        Dead,
    }
    [FoldoutGroup("State")] 
    public NetworkVariable<WendigoState> monState;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
