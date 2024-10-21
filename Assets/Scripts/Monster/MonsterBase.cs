using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class MonsterBase : NetworkBehaviour , IDamagable
{
    
    //health
    [FoldoutGroup("Health")] 
    public bool isDead = false;
    [FoldoutGroup("Health")] 
    public float maxHealth = 100;
    [FoldoutGroup("Health")] 
    public NetworkVariable<float> health = new NetworkVariable<float>();
    
    
    //references
    protected Animator anim;
    protected NavMeshAgent _agent;

    protected Rigidbody rb;
    
    public Transform target;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //monState = MonsterState.Idle;
        if(IsServer)
            health.Value = maxHealth;
    }
    // Start is called before the first frame update
    public virtual void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public virtual void TakeDamage(float damage, Vector3 direction, float stunTime = 0f)
    {
        if (base.IsOwner && !isDead)
        {
            health.Value -= damage;

            

            if (health.Value <= 0)
            {
                Die();
            }
            //Debug.Log($"{playerUsernameText} took {damage} damage.");
        }
    }

    public virtual void Die()
    {
        if (IsOwner && !isDead)
        {
            isDead = true;
            Destroy(gameObject);
        }
    }


}
