using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RenownedGames.AITree.Nodes;
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class NightCrawler : MonsterBase
{

    
    

    public PlayerController attatchedPlayer;

    public GameObject stuckHitbox;



    [FoldoutGroup("Chase")] 
    public float chaseDistance = 10f;
    
    
    //Jump Attack

    [FoldoutGroup("AttackVars")] 
    public float JumpTriggerDist = 4f;
    [FoldoutGroup("AttackVars")] 
    public float attackDamage;

    [FoldoutGroup("AttackVars")] 
    public float maxJumpCD=3f;
    
    [FoldoutGroup("AttackVars")] 
    public float maxAttackCD=3f;
    
    float currentJumpCD = 0f;
    float currentAttackCD = 0f;
    
    [FoldoutGroup("AttackVars")] 
    public float jumpForce;
    [FoldoutGroup("AttackVars")] 
    public float thrustForce;
    
    //patrol
    float patrolTimer = 0;
    [FoldoutGroup("Patrol")] 
    public float patrolTime;
    
    public enum NightCrawlerState
    {
        Idle,
        Chasing,
        Attacking,
        HitStunned,
        Attached,
        Dead,
    }
    [FoldoutGroup("State")] 
    public NetworkVariable<NightCrawlerState> monState;
    [FoldoutGroup("State")] 
    public bool jumping = false;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        monState.Value = NightCrawlerState.Idle;
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            if (monState.Value == NightCrawlerState.Dead)
            {
                
            }
            else if (monState.Value == NightCrawlerState.HitStunned)
            {
                
            }
            else if (monState.Value == NightCrawlerState.Attached)
            {
                AttachedUpdate();

            }
            else if (monState.Value != NightCrawlerState.Attacking)
            {
                if (_agent.isOnNavMesh)
                {
                    if (_agent.velocity != Vector3.zero)
                    {
                        anim.SetBool("Walking",true);
                    }
                    else
                    {
                        anim.SetBool("Walking",false);
                    }
                    Chase();

                }
                else
                {
                    TeleportToNearestNavmesh();
                }

                UpdateTarget();
                JumpAttack();
                if (monState.Value==NightCrawlerState.Idle)
                {
                    if (_agent.isOnNavMesh)
                    {
                        if (_agent.velocity != Vector3.zero)
                        {
                            anim.SetBool("Walking",true);
                        }
                        else
                        {
                            anim.SetBool("Walking",false);
                        }
                        Patrol();
                    }

                    else
                    {
                        TeleportToNearestNavmesh();
                    }
                }


            }

        }
    }

    public void LateUpdate()
    {
        if (monState.Value == NightCrawlerState.Attached)
        {
            AttachedPositionUpdate();
        }
    }

    public void AttachedPositionUpdate()
    {
        if (attatchedPlayer != null)
        {
            if (attatchedPlayer.isPlayerDead.Value)
            {
                attatchedPlayer = null;
            }
            else
            {
                GetComponent<Collider>().isTrigger = true;
                transform.position = attatchedPlayer.headTransform.position + attatchedPlayer.headTransform.forward * 0.3f + attatchedPlayer.headTransform.up*-0.2f;
                transform.forward = -attatchedPlayer.headTransform.forward;
            }
        }

    }

    public void Patrol()
    {
        if (patrolTimer <= 0)
        {
            patrolTimer = patrolTime + Random.Range(-4,4);
            if (_agent.isOnNavMesh)
            {
                _agent.SetDestination(transform.position + new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5)));
            }
        }
        else
        {
            patrolTimer -= Time.deltaTime;
        }
    }
    
    public void AttachedUpdate()
    {
        _agent.enabled = false;
        GetComponent<Collider>().isTrigger = true;
        if (attatchedPlayer != null)
        {
            if (attatchedPlayer.isPlayerDead.Value)
            {
                Unattatch();
            }
            else
            {

                if(currentAttackCD <= 0f)
                {
                    currentAttackCD = maxAttackCD;
                    AttackClientRpc();
                }
                else
                {
                    currentAttackCD -= Time.deltaTime;
                }
            }
        }

    }

    [ClientRpc]
    public void AttackClientRpc()
    {
        attatchedPlayer.TakeDamage(attackDamage, Vector3.Normalize(attatchedPlayer.transform.position - transform.position));
    }
    
    public void JumpAttack()
    {
        if(currentJumpCD <= 0f)
        {
            if (target != null)
            {
                if (Vector3.Distance(transform.position, target.position) <= JumpTriggerDist)
                {
                    
                    StartCoroutine(Jump());
                }
            }
        }
        else
        {
            currentJumpCD -= Time.deltaTime;
        }
    }

    public IEnumerator Jump()
    {
        
        monState.Value = NightCrawlerState.Attacking;
        yield return new WaitForSeconds(0.5f);
        transform.LookAt(target.position);
        _agent.enabled = false;
        yield return new WaitForSeconds(0.1f);
        rb.velocity = Vector3.zero;
        rb.AddForce(transform.up*jumpForce+transform.forward*thrustForce,ForceMode.Impulse);
        jumping = true;
        //AttackClientRpc();
        stuckHitbox.SetActive(true);
        yield return new WaitForSeconds(1f);
        stuckHitbox.SetActive(false);
        jumping = false;
        if (monState.Value == NightCrawlerState.Attached)
            yield break;
        yield return new WaitForSeconds(0.5f);
        
        yield return new WaitUntil(() =>
            NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, _agent.areaMask));
        _agent.enabled = true;
        rb.velocity = Vector3.zero;
        monState.Value = NightCrawlerState.Idle;
        currentJumpCD = maxJumpCD;
    }
    
    //[ClientRpc]
    //public void AttackClientRpc()
    //{
    //    Collider[] hits = Physics.OverlapSphere(transform.TransformPoint(attackOffset), attackRadius, attackMask);
    //    foreach (IDamagable targetDamagable in hits.Select(hit => hit.GetComponentInParent<IDamagable>()).Where(obj => obj != null).Where(obj => obj != this))
    //    {
    //        Vector3 direction = Vector3.zero;
    //        targetDamagable.TakeDamage(attackDamage, direction);
    //   }
    //}

    // [ServerRpc]
    // public void DamageServerRpc(IDamagable target, float damage)
    // {
    //     DamageClientRpc(target, damage);
    // }

    // [ClientRpc]
    // public void DamageClientRpc(IDamagable target, float damage)
    // {
    //     if (target.gameObject.)
    // }

    public void Chase()
    {
        if (currentJumpCD>0f)
        {
            _agent.isStopped = true;
        }
        else
        {
            _agent.isStopped = false;
            if (target != null)
            {
                monState.Value = NightCrawlerState.Chasing;
                _agent.SetDestination(target.position);
            }
        }

    }

    public void UpdateTarget()
    {
        target = null;
        for (int i = 0; i < GameSessionManager.Instance.playerControllerList.Count; i++)
        {
            if (GameSessionManager.Instance.playerControllerList[i].controlledByClient.Value &&!GameSessionManager.Instance.playerControllerList[i].isPlayerDead.Value&&!GameSessionManager.Instance.playerControllerList[i].isPlayerGrabbed.Value)
            {
                float dist= Vector3.Distance(transform.position,
                    GameSessionManager.Instance.playerControllerList[i].transform.position);

                if (dist<chaseDistance)
                {
                    if(target == null)
                        target = GameSessionManager.Instance.playerControllerList[i].transform;
                    else if(dist < Vector3.Distance(transform.position,target.position))
                        target = GameSessionManager.Instance.playerControllerList[i].transform;
                }
            }
        }

    } 
    
    public override void TakeDamage(float damage, Vector3 direction, float stunTime = 0f)
    {
        if (base.IsOwner && !isDead)
        {
            health.Value -= damage;


            StartCoroutine(Knockback(damage,direction));

            if (health.Value <= 0)
            {
                Die();
            }
            //Debug.Log($"{playerUsernameText} took {damage} damage.");
        }
    }

    public IEnumerator Knockback(float damage, Vector3 direction)
    {
        if(attatchedPlayer!=null)
            attatchedPlayer.isPlayerGrabbed.Value = false;
        _agent.enabled = false;
        if (monState.Value == NightCrawlerState.Attached)
        {
            GetComponent<Collider>().isTrigger = false;
            UnattachPlayerClientRpc();
        }

        rb.constraints = RigidbodyConstraints.None;
        
        rb.AddForce(direction.normalized * damage, ForceMode.Impulse);
        monState.Value = NightCrawlerState.HitStunned;
        yield return new WaitForSeconds(1.5f);
        yield return new WaitUntil(() =>
            NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 0.5f, _agent.areaMask));
        rb.velocity = Vector3.zero;
        _agent.enabled = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        monState.Value = NightCrawlerState.Idle;

    }

    void Unattatch()
    {
       
        attatchedPlayer.isPlayerGrabbed.Value = false;
        GetComponent<Collider>().isTrigger = false;
        UnattachPlayerClientRpc();
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, _agent.areaMask))
        {
            _agent.Warp(hit.position+ new Vector3(0,2,0));
            _agent.enabled = true;
            monState.Value = NightCrawlerState.Idle;
        }
        

    }

    public void SetAttachedPlayer(PlayerController playerController)
    {
        SetAttachedPlayerClientRpc(playerController.NetworkObject);
    }

    [Rpc(SendTo.Everyone)]
    public void SetAttachedPlayerClientRpc(NetworkObjectReference playerController)
    {
        if(playerController.TryGet(out NetworkObject playerControllerObject))
            attatchedPlayer = playerControllerObject.GetComponent<PlayerController>();
        GetComponent<Collider>().isTrigger = true;
    }
    [Rpc(SendTo.Everyone)]
    public void UnattachPlayerClientRpc()
    {
        attatchedPlayer = null;
        GetComponent<Collider>().isTrigger = false;
    }


}
