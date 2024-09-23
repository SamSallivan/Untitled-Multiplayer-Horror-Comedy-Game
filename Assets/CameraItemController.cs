using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public class CameraItemController : ItemController
{
    public Camera cam;
    public GameObject light;
    public NetworkVariable<bool> activated;
    public LayerMask lm;
    

    
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

    
    public override void OnButtonHeld()
    {
        if (cooldown <= 0)
        {
            cooldown = cooldownSetting;
            Activate();
        }
    }
    
    IEnumerator StartFlash()
    {
        ToggleLightServerRpc();
        
        yield return new WaitForSeconds(0.1f);
        if (inventoryItem.owner!=GameSessionManager.Instance.localPlayerController&& CheckVisibility())
        {
            PostProcessEffects.Instance.FlashBlind();
        }
        yield return new WaitForSeconds(0.2f);
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
