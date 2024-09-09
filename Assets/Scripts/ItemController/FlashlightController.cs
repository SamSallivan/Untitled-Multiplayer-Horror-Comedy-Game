using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class FlashlightController : NetworkBehaviour
{
    public GameObject light;
    public bool activated;

    public override void  OnNetworkSpawn(){
        base.OnNetworkSpawn();

        if(IsServer){
            SyncLightClientRpc(activated);
        }
        else{
            SyncLightServerRpc();
        }
    }
    
    void Update()
    {   
        if (GetComponent<I_InventoryItem>() && GetComponent<I_InventoryItem>().owner && GetComponent<I_InventoryItem>().owner == GameSessionManager.Instance.localPlayerController)
        {
            
            if (GetComponent<I_InventoryItem>().isCurrentlyEquipped && GetComponent<I_InventoryItem>().owner.enableMovement) 
            {
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    if(IsServer){
                        ToggleLightClientRpc();
                    }
                    else{
                        ToggleLightServerRpc();
                    }
                }
            }
            else if(light.activeInHierarchy)
            {
                ToggleLightServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleLightServerRpc(){
        ToggleLightClientRpc();
    }

    [ClientRpc]
    public void ToggleLightClientRpc(){
        activated = !activated;
        light.SetActive(activated);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SyncLightServerRpc(){
        SyncLightClientRpc(activated);
    }

    [ClientRpc]
    public void SyncLightClientRpc(bool state){
        activated = state;
        light.SetActive(activated);
    }
}
