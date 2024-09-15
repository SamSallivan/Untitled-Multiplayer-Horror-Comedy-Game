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
    
    public float attackDamage;

    
    public float maxJumpCD=3f;

    public float currentJumpCD = 0f;

    public float maxAttackCD=3f;

    public float currentAttackCD = 0f;

    public Vector3 attackOffset;

    public float attackRadius = 1f;

    public LayerMask attackMask;

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

    public MonsterState monState;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        monState = MonsterState.Idle;
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
            if (monState != MonsterState.Dead)
            {
                if (monState != MonsterState.HitStunned)
                {
                    if (monState != MonsterState.Attached)
                    {
                        if (monState != MonsterState.Attacking)
                        {
                            UpdateTarget();
                            Chase();
                            JumpAttack();
                        }
                    }
                    else
                    {
                        AttachedUpdate();
                    }

                }
            }


        }
        
    }


    public void AttachedUpdate()
    {
        _agent.enabled = false;
        GetComponent<Collider>().isTrigger = true;
        if (attatchedPlayer != null)
        {
            if (attatchedPlayer.isPlayerDead)
            {
                unattatch();
            }
            else
            {
                transform.position = attatchedPlayer.headTransform.position + attatchedPlayer.headTransform.forward * 0.3f + attatchedPlayer.headTransform.up*-0.2f;
                transform.forward = -attatchedPlayer.headTransform.forward;
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
                if (Vector3.Distance(transform.position, target.position) <= 3f)
                {
                    currentJumpCD = maxJumpCD;
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
        
        yield return new WaitForSeconds(0.5f);
        transform.LookAt(target.position);
        _agent.enabled = false;
        monState = MonsterState.Attacking;
        yield return new WaitForSeconds(0.1f);
        rb.AddForce(transform.up*jumpForce+transform.forward*thrustForce,ForceMode.Impulse);
        //AttackClientRpc();
        stuckHitbox.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        stuckHitbox.SetActive(false);
        if (monState == MonsterState.Attached)
            yield break;

        yield return new WaitUntil(() =>
            NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, _agent.areaMask));
        rb.velocity = Vector3.zero;
        _agent.enabled = true;
        monState = MonsterState.Chasing;
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
            if (GameSessionManager.Instance.playerControllerList[i].controlledByClient)
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
        if (monState == MonsterState.Attached)
        {
            GetComponent<Collider>().isTrigger = false;
            attatchedPlayer = null;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, _agent.areaMask))
            {
                _agent.Warp(hit.position+ new Vector3(0,2,0));
            }
        }
        
        rb.AddForce(direction.normalized * damage, ForceMode.Impulse);
        monState = MonsterState.HitStunned;
        yield return new WaitForSeconds(1.5f);
        yield return new WaitUntil(() =>
            NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, _agent.areaMask));
        rb.velocity = Vector3.zero;
        _agent.enabled = true;
        monState = MonsterState.Idle;

    }

    void unattatch()
    {
        GetComponent<Collider>().isTrigger = false;
        attatchedPlayer = null;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, _agent.areaMask))
        {
            _agent.Warp(hit.position+ new Vector3(0,2,0));
            monState = MonsterState.Idle;
        }
        

    }
}
