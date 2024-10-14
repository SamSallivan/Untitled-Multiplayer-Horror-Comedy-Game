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
    public  bool buttonHeldSecondary;
    [ReadOnly]
    public float heldTime;
    public float minHeldTime;
    [ReadOnly]
    public float cooldown;
    public float cooldownSetting;
    
    public bool holdItemWithSecondaryButton;
    
    
    public void Awake()
    {
        inventoryItem = GetComponent<I_InventoryItem>();
    }
        
    public void UseItem(bool buttonDown = true)
    {
        if(buttonDown)
        {
            buttonHeld = true;
            OnButtonHeld();
        }
        else if (!buttonDown)
        {
            buttonHeld = false;
            OnButtonReleased();
        }

        if (!holdItemWithSecondaryButton)
        {
            PlayHoldAnimationClientRpc(buttonDown);
        }
    }
        
    public void UseItemSecondary(bool buttonDown = true)
    {
        if(buttonDown)
        {
            buttonHeldSecondary = true;
            OnButtonHeldSecondary();
        }
        else if (!buttonDown)
        {
            buttonHeldSecondary = false;
            OnButtonReleasedSecondary();
        }

        if (holdItemWithSecondaryButton)
        {
            PlayHoldAnimationClientRpc(buttonDown);
        }
    }

    public virtual void OnButtonHeld()
    {
    }
    
    public virtual void OnButtonReleased()
    {
    }

    public virtual void OnButtonHeldSecondary()
    {
    }
    
    public virtual void OnButtonReleasedSecondary()
    {
    }

    public virtual void Update()
    {
        if (!holdItemWithSecondaryButton)
        {
            if (buttonHeld)
            {
                heldTime += Time.deltaTime;
            }
            else if (heldTime > 0)
            {
                heldTime = 0;
            }
        }
        else
        {
            if (buttonHeldSecondary)
            {
                heldTime += Time.deltaTime;
            }
            else if (heldTime > 0)
            {
                heldTime = 0;
            }
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

    public virtual void Activate()
    {
        PlayActivateAnimationClientRpc();
    }
    
    [Rpc(SendTo.Everyone)]
    public void PlayHoldAnimationClientRpc(bool buttonDown)
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
    
    [Rpc(SendTo.Everyone)]
    public void PlayActivateAnimationClientRpc()
    {
        inventoryItem.owner.playerAnimationController.armAnimator.SetTrigger("Activate");
    }

    public virtual void Cancel()
    {
        buttonHeld = false;
        buttonHeldSecondary = false;
        PlayHoldAnimationClientRpc(false);
    }
}
