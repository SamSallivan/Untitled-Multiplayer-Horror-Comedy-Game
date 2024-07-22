using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class ChangeOwnershipTrigger : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other) {
        if(base.IsServer && other.gameObject.GetComponent<PlayerController>()){
            PlayerController playerController = other.gameObject.GetComponent<PlayerController>();
            if(playerController.controlledByClient)
                GetComponent<NetworkObject>().ChangeOwnership(playerController.localPlayerId);
        }
    }
}
