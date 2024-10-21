using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Wendigo : MonsterBase, IHear
{


    public float alertTime = 3f;
    public float alertTimer = 0;
    public float searchTime = 8f;
    

    public float alertDelay = 1f;
    private float alertDelayTimer = 0;
    
    public float fovAngle = 90f;
    public Transform fovPoint;
    public float range = 8;
    public LayerMask visionLayer;

    private PlayerController closestVisiblePlayer;
    public enum WendigoState
    {
        Idle,
        Alert,
        Searching,
        Chasing,
        HitStunned,
        Dead,
    }
    [FoldoutGroup("State")] 
    public NetworkVariable<WendigoState> monState;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        monState.Value = WendigoState.Idle;
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            if (monState.Value == WendigoState.Idle)
            {
                SeePlayer();
                
            }
            else if (monState.Value == WendigoState.Alert)
            {
                SeePlayer();
                if (alertTimer > 0)
                {
                    alertTimer -= Time.deltaTime;
                }
                else
                {
                    monState.Value = WendigoState.Idle;
                }

                if (alertDelayTimer > 0)
                {
                    alertDelayTimer -= Time.deltaTime;
                }
            }
            else if (monState.Value == WendigoState.Searching)
            {
                SeePlayer();
                if(alertTimer>0)
                {
                    alertTimer -= Time.deltaTime;
                    if (!_agent.hasPath)
                    {
                        SearchArea();
                    }
                }
                else
                {
                    monState.Value = WendigoState.Idle;
                }
                
            }
            else if (monState.Value == WendigoState.Chasing)
            {
                Chase();
            }
        }
    }


    public void FindPlayerInVision()
    {
        closestVisiblePlayer = null;
        for (int i = 0; i < GameSessionManager.Instance.playerControllerList.Count; i++)
        {
            PlayerController player = GameSessionManager.Instance.playerControllerList[i];
            Vector3 dir = player.transform.position - transform.position;
            float angle = Vector3.Angle(dir, fovPoint.forward);
            RaycastHit r;
            if (player.controlledByClient.Value &&!player.isPlayerDead.Value&&!player.isPlayerGrabbed.Value)
            {
                float dist= Vector3.Distance(transform.position,
                    GameSessionManager.Instance.playerControllerList[i].transform.position);
                
                if (Physics.Raycast(fovPoint.position, player.transform.position-transform.position,out r,range, visionLayer)&&angle < fovAngle / 2)
                {
                    if (r.collider.CompareTag("Player"))
                    {
                        // WE SPOTTED THE PLAYER!
                        print("SEEN!");
                        if (closestVisiblePlayer == null)
                        {
                            closestVisiblePlayer = player;
                        }
                        else if (Vector3.Distance(transform.position,
                                closestVisiblePlayer.transform.position) >= dist)
                        {
                            closestVisiblePlayer = player;
                        }
                        Debug.DrawRay(fovPoint.position, dir, Color.red);
                
                    }
                    else
                    {
                        print("we dont seen");
                    }
                }

            }

        }

        if (closestVisiblePlayer != null)
        {
            target = closestVisiblePlayer.transform;
        }
        else
        {
            target = null;
        }
        

    }
    public void SeePlayer()
    {
        FindPlayerInVision();
        if (target != null)
        {
            monState.Value = WendigoState.Chasing;
        }
    }
    

    public void Chase()
    {
        UpdateAttackTarget();

        FindPlayerInVision();
        if(target!=null)
            _agent.SetDestination(target.position);
        else
        {
            monState.Value = WendigoState.Idle;
        }
    }

    public void UpdateAttackTarget()
    {
        
    }


    public void SearchArea()
    {
        
    }

    public void RespondToSound(Noise noise)
    {
        
        if (target == null)
        {
            if (monState.Value == WendigoState.Idle)
            {

                if (noise.soundType == Noise.SoundType.Interesting)
                {
                    alertDelayTimer = alertDelay;
                    alertTimer = alertTime;
                    monState.Value = WendigoState.Alert;
                }
                else if (noise.soundType == Noise.SoundType.Dangerous)
                {
                    alertTimer = searchTime;
                    monState.Value = WendigoState.Searching;
                    _agent.SetDestination(noise.pos);
                }
            }
            else if (monState.Value == WendigoState.Alert)
            {
                if (alertDelayTimer <= 0)
                {
                    if (noise.soundType == Noise.SoundType.Interesting||noise.soundType == Noise.SoundType.Dangerous)
                    {
                        alertTimer = searchTime;
                        monState.Value = WendigoState.Searching;
                        _agent.SetDestination(noise.pos);
                    }
                }

            }
            else if (monState.Value == WendigoState.Searching)
            {
                if (noise.soundType == Noise.SoundType.Interesting||noise.soundType == Noise.SoundType.Dangerous)
                {
                    alertTimer = searchTime;
                    _agent.SetDestination(noise.pos);
                }
            }
            
        }

        
    }
}
