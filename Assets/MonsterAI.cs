using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : NetworkBehaviour
{
    private NavMeshAgent _agent;

    public Transform target;
    // Start is called before the first frame update
    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        target = GameSessionManager.Instance.playerControllerList[0].transform;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTarget();
        Chase();
    }

    public void Chase()
    {
        if (target != null)
        {
            _agent.SetDestination(target.position);
        }
    }

    public void UpdateTarget()
    {
        if (target != null)
        {
            for (int i = 0; i < GameSessionManager.Instance.playerControllerList.Count; i++)
            {
                if (GameSessionManager.Instance.playerControllerList[i].controlledByClient)
                {
                    float dist= Vector3.Distance(transform.position,
                        GameSessionManager.Instance.playerControllerList[i].transform.position);

                    if (dist < Vector3.Distance(transform.position,
                            target.position))
                    {
                        target = GameSessionManager.Instance.playerControllerList[i].transform;
                    }
                }
            }
        }

    }
}
