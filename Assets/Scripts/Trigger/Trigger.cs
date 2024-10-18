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
    
    //public bool onceOnly;
    
    public List<int> playerIds = new List<int>();
    

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
        
        if ((triggerLayers.value & 1 << other.gameObject.layer) > 0)
        {
            if (other.gameObject.TryGetComponent(out PlayerController playerController) && !playerIds.Contains(playerController.localPlayerId))
            {
                playerIds.Add(playerController.localPlayerId);
            }
            
            if (IsServer)
            {
                triggered.Value = true;
                StartCoroutine(TriggerEvent());
            }
        }
    }
    public virtual void OnTriggerExit(Collider other)
    {
        
        if ((triggerLayers.value & 1 << other.gameObject.layer) > 0 && triggered.Value)
        {
            if (other.gameObject.TryGetComponent(out PlayerController playerController) && playerIds.Contains(playerController.localPlayerId))
            {
                playerIds.Remove(playerController.localPlayerId);
            }
            
            if (IsServer && playerIds.Count == 0)
            {
                triggered.Value = false;
            }
        }
    }
}