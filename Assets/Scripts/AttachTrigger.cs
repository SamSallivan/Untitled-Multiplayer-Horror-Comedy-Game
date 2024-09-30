using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachTrigger : MonoBehaviour
{
    private NightCrawler _nightCrawler;
    // Start is called before the first frame update
    void Start()
    {
        _nightCrawler = GetComponentInParent<NightCrawler>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_nightCrawler.monState.Value != NightCrawler.MonsterState.Attached&&other.GetComponentInParent<PlayerController>()&&!other.GetComponentInParent<PlayerController>().isPlayerGrabbed.Value)
        {
            _nightCrawler.monState.Value = NightCrawler.MonsterState.Attached;
            _nightCrawler.SetAttachedPlayer(other.GetComponentInParent<PlayerController>());
            other.GetComponentInParent<PlayerController>().isPlayerGrabbed.Value = true;

        }
        
    }
}
