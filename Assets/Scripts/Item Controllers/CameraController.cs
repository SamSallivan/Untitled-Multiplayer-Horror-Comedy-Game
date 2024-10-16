using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public class CameraController : ItemController
{
    public Camera cam;
    public GameObject light;
    public NetworkVariable<bool> activated;
    public LayerMask lm;
    public float durabilityCostPerUse = 0.5f;

    public AudioClip flashSound;

    
    public override void  OnNetworkSpawn(){
        base.OnNetworkSpawn();
        //SyncLightServerRpc();
        activated.OnValueChanged += OnActivatedChanged;
    }

    public override void ItemUpdate()
    {
        
    }

    public override void Activate()
    {
        StartCoroutine(StartFlash());
    }

    
    public override void OnButtonReleased()
    {
        if (heldTime > minHeldTime && cooldown <= 0&& inventoryItem.itemStatus.durability >= durabilityCostPerUse)
        {
            cooldown = cooldownSetting;
            inventoryItem.itemStatus.durability -= durabilityCostPerUse;
            inventoryItem.SetItemDurarbilityRpc(inventoryItem.itemStatus.durability);
            Activate();
        }
    }
    
    /*public override void OnButtonHeld()
    {
        if (cooldown <= 0 )
        {
            cooldown = cooldownSetting;
            inventoryItem.itemStatus.durability -= durabilityCostPerUse;
            InventoryManager.instance.SetItemDurarbilityRpc(inventoryItem.NetworkObject, inventoryItem.itemStatus.durability);
            Activate();
        }
    }*/
    
    IEnumerator StartFlash()
    {
        ToggleLightServerRpc();
        SoundManager.Instance.PlayServerSoundEffect(flashSound,transform.position);
        yield return new WaitForSeconds(0.1f);
        CheckBlindClientRpc();
        yield return new WaitForSeconds(0.2f);
        ToggleLightServerRpc();
    }
    
    [Rpc(SendTo.Everyone)]
    public void CheckBlindClientRpc(){
        if (inventoryItem.owner!=GameSessionManager.Instance.localPlayerController&& CheckVisibility())
        {
            PostProcessEffects.Instance.FlashBlind();
        }
    }

    [Rpc(SendTo.Server)]
    public void FlashSoundServerRpc(){
        SoundManager.Instance.PlayServerSoundEffect(flashSound,transform.position);
    }



    [Rpc(SendTo.Server)]
    public void ToggleLightServerRpc(){
        activated.Value = !activated.Value;
    }

    public void OnActivatedChanged(bool previous, bool current)
    {
        light.SetActive(activated.Value);
    }

    private bool CheckVisibility()
    {
        cam = Camera.main;
        var planes = GeometryUtility.CalculateFrustumPlanes(cam);
        Vector3 point = light.transform.position;

        foreach (var p in planes)
        {
            if (p.GetDistanceToPoint(point) > 0)
            {
                Ray ray = new Ray(cam.transform.position, light.transform.position - cam.transform.position);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit,lm))
                {
                    Debug.Log(hit.transform.gameObject.name);
                    return hit.transform.gameObject == this.gameObject;
                }
                else return false;
            }
            else return false;

        }

        return false;
    }
}
