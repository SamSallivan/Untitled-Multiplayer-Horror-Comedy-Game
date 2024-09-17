using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ItemController : NetworkBehaviour
{
    public I_InventoryItem inventoryItem;
    public  bool buttonHeld;
    public float heldTime;
    public float minHeldTime;
    public float cooldown;
    public float cooldownSetting;

    public void Awake()
    {
        inventoryItem = GetComponent<I_InventoryItem>();
    }

    public virtual void Update()
    {
        if (buttonHeld)
        {
            heldTime += Time.deltaTime;
        }
        else if(heldTime > 0)
        {
            heldTime = 0;
        }
        
        if (cooldown >= 0)
        {
            cooldown -= Time.deltaTime;
        }

        ItemUpdate();
    }

    public virtual void ItemUpdate()
    {
    }
        
    public virtual void UseItem(bool buttonDown = true)
    {
        if(buttonDown)
        {
            buttonHeld = true;
            OnButtonHeld();
            HoldItemServerRpc(true);
        }
        else if (!buttonDown)
        {
            buttonHeld = false;
            OnButtonReleased();
            HoldItemServerRpc(false);
        }
    }

    public virtual void OnButtonHeld()
    {
    }
    
    public virtual void OnButtonReleased()
    {
        if (heldTime > minHeldTime && cooldown <= 0)
        {
            cooldown = cooldownSetting;
            ActivateItemServerRpc();
            Activate();
        }
    }

    public virtual void Activate()
    {
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateItemServerRpc()
    {
        ActivateItemClientRpc();
    }
    
    [ClientRpc]
    public void ActivateItemClientRpc()
    {
        inventoryItem.owner.playerAnimationController.armAnimator.SetTrigger("Activate");
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void HoldItemServerRpc(bool buttonDown)
    {
        HoldItemClientRpc(buttonDown);
    }
    
    [ClientRpc]
    public void HoldItemClientRpc(bool buttonDown)
    {
        if (inventoryItem.owner != null)
        {
            if (buttonDown)
            {
                inventoryItem.owner.playerAnimationController.armAnimator.ResetTrigger("Activate");
            }

            inventoryItem.owner.playerAnimationController.armAnimator.SetBool("Held", buttonDown);
        }
    }
}
