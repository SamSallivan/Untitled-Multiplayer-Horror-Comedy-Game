using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Trigger : NetworkBehaviour
{
    public LayerMask triggerLayers;
    
    public NetworkVariable<bool> triggered;
    
    public bool onceOnly;
    
    public GameObject colliderGameObject;
    
    public virtual void Start()
    {
        GetComponent<Rigidbody>().isKinematic = true;
    }

    public virtual IEnumerator TriggerEvent()
    {
        yield break;
    }

    public virtual void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
        {
            return;
        }
        
        if ((triggerLayers.value & 1 << other.gameObject.layer) > 0 && !triggered.Value)
        {
            triggered.Value = true;
            colliderGameObject = other.gameObject;
            StartCoroutine(TriggerEvent());
        }
    }
    public virtual void OnTriggerExit(Collider other)
    {
        if (!IsServer)
        {
            return;
        }
        
        if ((triggerLayers.value & 1 << other.gameObject.layer) > 0 && triggered.Value && !onceOnly)
        {
            triggered.Value = false;
            colliderGameObject = null;
        }
    }
}