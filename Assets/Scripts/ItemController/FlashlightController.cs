using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class FlashlightController : ItemController
{
    public GameObject light;
    public NetworkVariable<bool> activated;

    public override void  OnNetworkSpawn(){
        base.OnNetworkSpawn();
        //SyncLightServerRpc();
        light.SetActive(activated.Value);
        activated.OnValueChanged += OnActivatedChanged;
    }

    public override void ItemUpdate()
    {   
        if (GetComponent<I_InventoryItem>() && GetComponent<I_InventoryItem>().owner && GetComponent<I_InventoryItem>().owner == GameSessionManager.Instance.localPlayerController)
        {
            if (!GetComponent<I_InventoryItem>().isCurrentlyEquipped & activated.Value) 
            {
                ToggleLightServerRpc();
            }
        }
    }

    public override void Activate()
    {
        ToggleLightServerRpc();
    }

    [Rpc(SendTo.Server)]
    public void ToggleLightServerRpc(){
        activated.Value = !activated.Value;
    }

    public void OnActivatedChanged(bool previous, bool current)
    {
        light.SetActive(activated.Value);
    }

    /*[Rpc(SendTo.Server)]
    public void SyncLightServerRpc(){
        SyncLightClientRpc(activated);
    }

    [Rpc(SendTo.Everyone)]
    public void SyncLightClientRpc(bool state){
        activated = state;
        light.SetActive(activated);
    }*/
}
