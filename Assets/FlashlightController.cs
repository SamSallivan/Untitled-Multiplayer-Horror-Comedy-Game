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
        if(light.activeInHierarchy != activated)
        {
            light.SetActive(activated);
        }

        if (GetComponent<I_InventoryItem>() && GetComponent<I_InventoryItem>().owner && GetComponent<I_InventoryItem>().owner == GameNetworkManager.Instance.localPlayerController)
        {
            
            if (GetComponent<I_InventoryItem>().owner.isPlayerControlled && GetComponent<I_InventoryItem>().isCurrentlyEquipped) 
            {

                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    if(IsServer){
                        ToggleLightClientRpc();
                    }
                    else{
                        ToggleLightServerRpc();
                    }
                    /*transform.position = Vector3.Lerp(transform.position, PlayerController.instance.tHead.GetChild(2).position, Time.fixedDeltaTime * 5);
                    transform.rotation = Quaternion.Lerp(transform.rotation, PlayerController.instance.tHead.GetChild(2).rotation, Time.fixedDeltaTime * 5);*/
                }
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
