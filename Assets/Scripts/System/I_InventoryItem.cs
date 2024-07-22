using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Steamworks;
using Unity.Netcode;

public class I_InventoryItem : Interactable
{
    public event Action OnPickUp = delegate { };

    public PlayerController owner;
    public bool isCurrentlyEquipped;
    public InventorySlot inventorySlot;
    public bool enableItemMeshes = true;
    public bool enableItemPhysics = true;
    
    public override void  OnNetworkSpawn(){
        base.OnNetworkSpawn();

        if(IsServer)
        {
            int ownerPlayerId = owner == null ? -1 : (int)owner.localPlayerId;
            SyncItemStateClientRpc(ownerPlayerId, isCurrentlyEquipped, enableItemMeshes, enableItemPhysics);
        }
        else
        {
            SyncItemStateServerRpc();
        }
    }

	public virtual void LateUpdate()
	{
        //if(IsOwner){
            if (owner != null)
            {
                if (owner.controlledByClient) 
                {
                    Transform targetTransform = owner.equippedTransform;
                    base.transform.position = targetTransform.TransformPoint(itemData.equipPosition);;
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
        //}
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
        enableItemMeshes = enable;

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

		// Light[] lights = base.gameObject.GetComponentsInChildren<Light>();
		// for (int j = 0; j < lights.Length; j++)
		// {
		// 	lights[j].enabled = enable;
		// }
	}
    
	public void EnableItemPhysics(bool enable)
	{
        enableItemPhysics = enable;
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

    [ServerRpc(RequireOwnership = false)]
    public void SyncItemStateServerRpc()
    {
        int ownerPlayerId = owner == null ? -1 : (int)owner.localPlayerId;
        SyncItemStateClientRpc(ownerPlayerId, isCurrentlyEquipped, enableItemMeshes, enableItemPhysics);
    }

    [ClientRpc]
    public void SyncItemStateClientRpc(int ownerPlayerId, bool isCurrentlyEquipped, bool enableMeshes, bool enablePhysics)
    {
        if(ownerPlayerId != -1)
        {
            this.owner = GameSessionManager.Instance.playerControllerList[ownerPlayerId];
        }
        else
        {
            this.owner = null;
        }
        this.isCurrentlyEquipped = isCurrentlyEquipped;
        enableItemMeshes = enableMeshes;
        enableItemPhysics = enablePhysics;
        EnableItemMeshes(enableItemMeshes);
        EnableItemPhysics(enableItemPhysics);
    }
}
