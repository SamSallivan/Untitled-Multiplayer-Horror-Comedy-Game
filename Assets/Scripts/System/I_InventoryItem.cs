using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Steamworks;

public class I_InventoryItem : Interactable
{
    public event Action OnPickUp = delegate { };

    public PlayerController owner;
    public bool isCurrentlyEquipped;
    public InventorySlot inventorySlot;
    
	public virtual void LateUpdate()
	{
		if (owner != null)
		{
            if (owner.isPlayerControlled) 
            {
                Transform targetTransform = owner.equippedTransform;
                base.transform.position = targetTransform.position + itemData.equipPosition;
                base.transform.rotation = targetTransform.rotation;
                base.transform.Rotate(itemData.equipRotation);
            }
            else
            {
                owner = null;
                inventorySlot = null;
                EnableItemMeshes(true);
                EnableItemPhysics(true);
            }
        }
    }

    public override IEnumerator InteractionEvent()
    {
        if(itemData != null)
        {
            I_InventoryItem item = InventoryManager.instance.AddItemToInventory(this);
            //InventoryItem newItem = InventoryManager.instance.AddItem(itemData, itemStatus);
            //Destroy(transform.gameObject);
            OnPickUp?.Invoke();
            if (item != null)
            {
                /*if (equipOnPickUp && itemData.isEquippable)
                {
                    InventoryManager.instance.EquipItem(item);
                }*/

                if (openInventoryOnPickUp)
                {
                    InventoryManager.instance.OpenInventory();
                    InventoryManager.instance.selectedPosition =
                    InventoryManager.instance.GetGridPosition(item.inventorySlot.GetIndex());
                }
            }
            UnTarget();
        }
        yield return null; 
    }


	public void EnableItemMeshes(bool enable)
	{
		MeshRenderer[] meshRenderers = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < meshRenderers.Length; i++)
		{
            meshRenderers[i].enabled = enable;
		}

		SkinnedMeshRenderer[] skinnedMeshRenderers = base.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		for (int j = 0; j < skinnedMeshRenderers.Length; j++)
		{
			skinnedMeshRenderers[j].enabled = enable;
			Debug.Log("DISABLING/ENABLING SKINNEDMESH: " + skinnedMeshRenderers[j].gameObject.name);
		}
	}
    
	public void EnableItemPhysics(bool enable)
	{
        base.gameObject.GetComponent<Rigidbody>().isKinematic = !enable;
        Collider[] colliders = base.gameObject.GetComponentsInChildren<Collider>();
		for (int i = 0; i < colliders.Length; i++)
		{
            colliders[i].enabled = enable;
		}
    }

    public override void Target()
    {
        if (highlightTarget != null)
        {
            if (!highlightTarget.GetComponent<OutlineRenderer>())
            {
                OutlineRenderer outline = highlightTarget.AddComponent<OutlineRenderer>();
                outline.OutlineMode = OutlineRenderer.Mode.OutlineVisible;
                outline.OutlineWidth = 10;
            }
        }
        UIManager.instance.interactionName.text = textName;
        UIManager.instance.interactionName.text += !itemData.isStackable ? "" : " x " + itemStatus.amount;
        //UI.instance.interactionPrompt.text = textPrompt;

        if (textPrompt != "" && interactionType != InteractionType.None)
        {
            UIManager.instance.interactionPrompt.text = "[E] ";
            UIManager.instance.interactionPrompt.text += activated ? textPromptActivated : textPrompt;
            //enable button prompt image instead
            //UIManager.instance.interactionPromptAnimation.Play("PromptButtonAppear");
        }
    }
}
