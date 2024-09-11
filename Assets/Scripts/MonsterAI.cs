using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : NetworkBehaviour
{
    private NavMeshAgent _agent;

    Transform closestplayer;
    public Transform target;

    public float attackDamage;

    public float maxAttackCD=3f;

    public float currentAttackCD = 0f;

    public Vector3 attackOffset;

    public float attackRadius = 1f;

    public LayerMask attackMask;

    public float chaseDistance = 10f;
    
    
    // Start is called before the first frame update
    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        target = GameSessionManager.Instance.playerControllerList[0].transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            UpdateTarget();
            Chase();
            Attack();
        }
        
    }
    
    public void Attack()
    {
        if(currentAttackCD <= 0f)
        {
            if (target != null)
            {
                if (Vector3.Distance(transform.position, target.position) <= 1f)
                {
                    currentAttackCD = maxAttackCD;
                    AttackClientRpc();
                }
            }
        }
        else
        {
            currentAttackCD -= Time.deltaTime;
        }
    }
    
    [ClientRpc]
    public void AttackClientRpc()
    {
        Collider[] hits = Physics.OverlapSphere(transform.TransformPoint(attackOffset), attackRadius, attackMask);
        foreach (var targetDamagable in hits.Select(hit => hit.GetComponentInParent<IDamagable>()).Where(obj => obj != null).Where(obj => obj != this))
        {
            targetDamagable.TakeDamage(attackDamage);
        }
    }

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
        if (currentAttackCD>0f)
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
}
