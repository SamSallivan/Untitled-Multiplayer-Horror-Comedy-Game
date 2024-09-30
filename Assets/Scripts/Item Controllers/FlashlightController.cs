using System.Collections;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using UnityEngine;
using Unity.Netcode;

public class FlashlightController : ItemController
{
    public GameObject light;
    public NetworkVariable<bool> activated;
    public float durabilityCostPerSecond = 0.1667f;


    public AudioClip on;
    public AudioClip off;
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
            if (!GetComponent<I_InventoryItem>().isCurrentlyEquipped.Value & activated.Value) 
            {
                ToggleLightServerRpc();
            }
        }
        
        if (activated.Value) 
        {
            if (inventoryItem.itemStatus.durability <= 0)
            {
                ToggleLightServerRpc();
            }
            else
            {
                inventoryItem.itemStatus.durability -= durabilityCostPerSecond * Time.deltaTime;
                InventoryManager.instance.SetItemDurarbilityRpc(inventoryItem.NetworkObject, inventoryItem.itemStatus.durability);
            }
        }
    }
    
    public override void OnButtonHeld()
    {
        if (cooldown <= 0)
        {
            cooldown = cooldownSetting;
            Activate();
        }
    }

    public override void Activate()
    {
        base.Activate();
        ToggleLightServerRpc();
    }

    [Rpc(SendTo.Server)]
    public void ToggleLightServerRpc(){
        activated.Value = !activated.Value;
    }

    public void OnActivatedChanged(bool previous, bool current)
    {
        light.SetActive(activated.Value);
        if (activated.Value)
        {
            SoundManager.Instance.PlayClientSoundEffect(on,transform.position);
        }
        else
        {
            SoundManager.Instance.PlayClientSoundEffect(off,transform.position);
        }
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
