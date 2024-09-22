using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Sirenix.OdinInspector;

public class ItemController : NetworkBehaviour
{
    [ReadOnly]
    public I_InventoryItem inventoryItem;
    [ReadOnly]
    public  bool buttonHeld;
    [ReadOnly]
    public float heldTime;
    public float minHeldTime;
    [ReadOnly]
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
            HoldItemClientRpc(true);
            OnButtonHeld();
        }
        else if (!buttonDown)
        {
            buttonHeld = false;
            HoldItemClientRpc(false);
            OnButtonReleased();
        }
    }

    public virtual void OnButtonHeld()
    {
    }
    
    public virtual void OnButtonReleased()
    {
    }

    public virtual void Activate()
    {
        ActivateItemClientRpc();
    }
    
    [Rpc(SendTo.Everyone)]
    public void ActivateItemClientRpc()
    {
        inventoryItem.owner.playerAnimationController.armAnimator.SetTrigger("Activate");
    }
    
    [Rpc(SendTo.Everyone)]
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

    public virtual void Cancel()
    {
        buttonHeld = false;
        HoldItemClientRpc(false);
    }
}
