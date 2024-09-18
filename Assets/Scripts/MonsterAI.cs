using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RenownedGames.AITree.Nodes;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : NetworkBehaviour, IDamagable
{
    private NavMeshAgent _agent;

    private Rigidbody rb;

    Transform closestplayer;
    public Transform target;

    public PlayerController attatchedPlayer;

    public GameObject stuckHitbox;



    public float chaseDistance = 10f;

    
    //health
    public bool isDead = false;
    public float maxHealth = 100;
    public NetworkVariable<float> health = new NetworkVariable<float>();
    
    
    
    //Jump Attack


    public float JumpTriggerDist = 4f;
    public float attackDamage;

    
    public float maxJumpCD=3f;

    float currentJumpCD = 0f;

    
    public float maxAttackCD=3f;

    float currentAttackCD = 0f;
    
    public float jumpForce;
    public float thrustForce;

    public enum MonsterState
    {
        Idle,
        Chasing,
        Attacking,
        HitStunned,
        Attached,
        Dead,
    }

    public NetworkVariable<MonsterState> monState;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //monState = MonsterState.Idle;
        if(IsServer)
            health.Value = maxHealth;
    }

    // Start is called before the first frame update
    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            if (monState.Value != MonsterState.Dead)
            {
                if (monState.Value != MonsterState.HitStunned)
                {
                    if (monState.Value != MonsterState.Attached)
                    {
                        if (monState.Value != MonsterState.Attacking)
                        {
                            UpdateTarget();
                            Chase();
                            JumpAttack();
                            if (monState.Value == MonsterState.Attached)
                            {
                                AttachedUpdate();
                            }
                        }
                    }
                }
            }


        }
        
        if (monState.Value == MonsterState.Attached)
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
                
            }
            else
            {
                transform.position = attatchedPlayer.headTransform.position + attatchedPlayer.headTransform.forward * 0.3f + attatchedPlayer.headTransform.up*-0.2f;
                transform.forward = -attatchedPlayer.headTransform.forward;
            }
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
                unattatch();
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
        attatchedPlayer.TakeDamage(attackDamage,Vector3.zero);
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
        
        monState.Value = MonsterState.Attacking;
        yield return new WaitForSeconds(0.5f);
        transform.LookAt(target.position);
        _agent.enabled = false;
        yield return new WaitForSeconds(0.5f);
        rb.velocity = Vector3.zero;
        rb.AddForce(transform.up*jumpForce+transform.forward*thrustForce,ForceMode.Impulse);
        //AttackClientRpc();
        stuckHitbox.SetActive(true);
        yield return new WaitForSeconds(1f);
        stuckHitbox.SetActive(false);
        if (monState.Value == MonsterState.Attached)
            yield break;
        yield return new WaitForSeconds(0.5f);
        yield return new WaitUntil(() =>
            NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 0.5f, _agent.areaMask));
        _agent.enabled = true;
        rb.velocity = Vector3.zero;
        monState.Value = MonsterState.Chasing;
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
                _agent.SetDestination(target.position);
            }
        }

    }

    public void UpdateTarget()
    {
        target = null;
        for (int i = 0; i < GameSessionManager.Instance.playerControllerList.Count; i++)
        {
            if (GameSessionManager.Instance.playerControllerList[i].controlledByClient&&!GameSessionManager.Instance.playerControllerList[i].isPlayerDead.Value)
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
    
    public void TakeDamage(float damage, Vector3 direction)
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

    public void Die()
    {
        if (IsOwner && !isDead)
        {
            isDead = true;
            Destroy(gameObject);
        }
    }

    public IEnumerator Knockback(float damage, Vector3 direction)
    {
        _agent.enabled = false;
        if (monState.Value == MonsterState.Attached)
        {
            GetComponent<Collider>().isTrigger = false;
            UnattachPlayerClientRpc();
        }
        
        rb.AddForce(direction.normalized * damage, ForceMode.Impulse);
        monState.Value = MonsterState.HitStunned;
        yield return new WaitForSeconds(1.5f);
        yield return new WaitUntil(() =>
            NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, _agent.areaMask));
        rb.velocity = Vector3.zero;
        _agent.enabled = true;
        monState.Value = MonsterState.Idle;

    }

    void unattatch()
    {
        GetComponent<Collider>().isTrigger = false;
        UnattachPlayerClientRpc();
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, _agent.areaMask))
        {
            _agent.Warp(hit.position+ new Vector3(0,2,0));
            _agent.enabled = true;
            monState.Value = MonsterState.Idle;
        }
        

    }

    public void setAttachedPlayer(PlayerController playerController)
    {
        SetAttachedPlayerClientRpc(playerController.NetworkObject);
    }

    [Rpc(SendTo.Everyone)]
    public void SetAttachedPlayerClientRpc(NetworkObjectReference playerController)
    {
        if(playerController.TryGet(out NetworkObject playerControllerObject))
        attatchedPlayer = playerControllerObject.GetComponent<PlayerController>();
        ;
    }
    [Rpc(SendTo.Everyone)]
    public void UnattachPlayerClientRpc()
    {
        attatchedPlayer = null;
    }
}
