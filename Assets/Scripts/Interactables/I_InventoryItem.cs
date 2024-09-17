using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Steamworks;
using Unity.Netcode;
using System.Linq;

public class I_InventoryItem : Interactable
{
    public event Action OnPickUp = delegate { };

    public PlayerController owner;

    public bool isCurrentlyEquipped;

    private InventorySlot _inventorySlot;

    public InventorySlot inventorySlot
    {
        get
        {
            return this._inventorySlot;
        }
        set
        {
            this._inventorySlot = value;
            OnSetInventorySlot();
        }
    }

    public bool enableItemMeshes = true;

    public bool enableItemPhysics = true;

    public bool inStorageBox = false;
    
    void Start()
    {
        if (!IsHost)
        {
            if (!NetworkObject.IsSpawned)
                Destroy(this.gameObject);
        }
    }

    public override void OnNetworkSpawn(){
        base.OnNetworkSpawn();

        if(IsServer)
        {
            int ownerPlayerId = owner == null ? -1 : (int)owner.localPlayerId;
            SyncItemStateClientRpc(ownerPlayerId, isCurrentlyEquipped, enableItemMeshes, enableItemPhysics, itemStatus.amount, inStorageBox);
        }
        else
        {
            SyncItemStateServerRpc();
        }
    }

    public void OnEquip()
    {
        EnableItemMeshes(true);
        isCurrentlyEquipped = true;
        if (owner && !string.IsNullOrEmpty(itemData.equipAnimatorParameter))
        {
            owner.playerAnimationController.armAnimator.SetBool(itemData.equipAnimatorParameter, true);
        }
    }
    
    public void OnUnequip()
    {
        EnableItemMeshes(false);
        isCurrentlyEquipped = false;
        if (owner && !string.IsNullOrEmpty(itemData.equipAnimatorParameter))
        {
            owner.playerAnimationController.armAnimator.SetBool(itemData.equipAnimatorParameter, false);
        }

        if (TryGetComponent<ItemController>(out var itemController))
        {
            itemController.HoldItemClientRpc(false);
        }
    }

	public virtual void LateUpdate()
	{
        if (owner != null)
        {
            if (owner.controlledByClient && !owner.isPlayerDead) 
            {
                Transform targetTransform = owner.equippedTransform;
                base.transform.position = targetTransform.TransformPoint(itemData.equipPosition);;
                base.transform.rotation = targetTransform.rotation;
                base.transform.Rotate(itemData.equipRotation);
            }
            else if (IsServer)
            {
                if (!owner.controlledByClient && inStorageBox)
                {
                    InventoryManager.instance.DestoryItemServerRpc(this.NetworkObject);
                }
                else if (!inStorageBox)
                {
                    InventoryManager.instance.UnpocketItemClientRpc(this.NetworkObject);
                }
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
                    InventoryManager.instance.selectedSlot = item.inventorySlot;
                }
            }
            //UnTarget();
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

    [Rpc(SendTo.Server)]
    public void SyncItemStateServerRpc()
    {
        int ownerPlayerId = owner == null ? -1 : (int)owner.localPlayerId;
        SyncItemStateClientRpc(ownerPlayerId, isCurrentlyEquipped, enableItemMeshes, enableItemPhysics, itemStatus.amount, inStorageBox);
    }

    [Rpc(SendTo.Everyone)]
    public void SyncItemStateClientRpc(int ownerPlayerId, bool isCurrentlyEquipped, bool enableMeshes, bool enablePhysics, int amount, bool inStorageBox)
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
        itemStatus.amount = amount;
        this.inStorageBox = inStorageBox;
    }

    public void OnSetInventorySlot()
    {
        if(inventorySlot)
        {
            if(InventoryManager.instance.storageSlotList.Contains(inventorySlot))
            {
                inStorageBox = true;
                // owner.storageItemList.Add(this);
                // owner.inventoryItemList.Remove(this);
            }
            else if(InventoryManager.instance.inventorySlotList.Contains(inventorySlot))
            {
                inStorageBox = false;
                // owner.storageItemList.Remove(this);
                // owner.inventoryItemList.Add(this);
            }
        }
        else
        {
            // owner.storageItemList.Remove(this);
            // owner.inventoryItemList.Remove(this);
        }
        
        List<NetworkObjectReference> inventoryItemListNetworkObject = new List<NetworkObjectReference>();
        // foreach(I_InventoryItem inventoryItem in owner.inventoryItemList)
        // {
        //     inventoryItemListNetworkObject.Add(inventoryItem.NetworkObject);
        // }

        List<NetworkObjectReference> storageItemListNetworkObject = new List<NetworkObjectReference>();
        // foreach(I_InventoryItem storageItem in owner.storageItemList)
        // {
        //     storageItemListNetworkObject.Add(storageItem.NetworkObject);
        // }

        OnSetInventorySlotClientRpc(inStorageBox, inventoryItemListNetworkObject.ToArray(), storageItemListNetworkObject.ToArray());
    }

    [Rpc(SendTo.Everyone)]
    public void OnSetInventorySlotClientRpc(bool inStorageBox, NetworkObjectReference[] inventoryItemListReference, NetworkObjectReference[] storageItemListReference)
    {
        this.inStorageBox = inStorageBox;

        // List<I_InventoryItem> inventoryItemList = new List<I_InventoryItem>();
        // foreach(NetworkObjectReference inventoryItem in inventoryItemListReference)
        // {
        //     if(inventoryItem.TryGet(out NetworkObject inventoryItemObject))
        //     {
        //         inventoryItemList.Add(inventoryItemObject.GetComponent<I_InventoryItem>());
        //     }
        // }

        // List<I_InventoryItem> storageItemList = new List<I_InventoryItem>();
        // foreach(NetworkObjectReference inventoryItem in storageItemListReference)
        // {
        //     if(inventoryItem.TryGet(out NetworkObject inventoryItemObject))
        //     {
        //         storageItemList.Add(inventoryItemObject.GetComponent<I_InventoryItem>());
        //     }
        // }

        // owner.inventoryItemList = inventoryItemList;
        // owner.storageItemList = storageItemList;
    }
}
