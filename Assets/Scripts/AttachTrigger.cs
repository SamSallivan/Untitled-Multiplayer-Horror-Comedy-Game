using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachTrigger : MonoBehaviour
{
    private MonsterAI _monsterAI;
    // Start is called before the first frame update
    void Start()
    {
        _monsterAI = GetComponentInParent<MonsterAI>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_monsterAI.monState.Value != MonsterAI.MonsterState.Attached&&other.GetComponentInParent<PlayerController>())
        {
            _monsterAI.monState.Value = MonsterAI.MonsterState.Attached;
            _monsterAI.setAttachedPlayer(other.GetComponentInParent<PlayerController>());
            
            
        }
        
    }
}
