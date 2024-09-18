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

    public NetworkVariable<bool> isCurrentlyEquipped = new(writePerm: NetworkVariableWritePermission.Owner);

    public NetworkVariable<bool> enableItemMeshes = new(true, writePerm: NetworkVariableWritePermission.Owner);

    public NetworkVariable<bool> enableItemPhysics = new(true, writePerm: NetworkVariableWritePermission.Owner);

    public NetworkVariable<bool> inStorageBox = new(writePerm: NetworkVariableWritePermission.Owner);
    
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
            SyncItemStateClientRpc(ownerPlayerId, itemStatus.amount);
        }
        else
        {
            SyncItemStateServerRpc();
        }
    }

    public void OnEquip()
    {
        EnableItemMeshes(true);
        if (IsServer)
        {
            isCurrentlyEquipped.Value = true;
        }

        if (owner && !string.IsNullOrEmpty(itemData.equipAnimatorParameter))
        {
            owner.playerAnimationController.armAnimator.SetBool(itemData.equipAnimatorParameter, true);
        }
    }
    
    public void OnUnequip()
    {
        EnableItemMeshes(false);
        if (IsServer)
        {
            isCurrentlyEquipped.Value = false;
        }

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
            if (owner.controlledByClient && !owner.isPlayerDead.Value) 
            {
                Transform targetTransform = owner.equippedTransform;
                base.transform.position = targetTransform.TransformPoint(itemData.equipPosition);;
                base.transform.rotation = targetTransform.rotation;
                base.transform.Rotate(itemData.equipRotation);
            }
            else if (IsServer)
            {
                if (!owner.controlledByClient && inStorageBox.Value)
                {
                    InventoryManager.instance.DestoryItemServerRpc(this.NetworkObject);
                }
                else if (!inStorageBox.Value)
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
        if (IsServer)
        {
            enableItemMeshes.Value = enable;
        }

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
        if (IsServer)
        {
            enableItemPhysics.Value = enable;
        }

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
        SyncItemStateClientRpc(ownerPlayerId,itemStatus.amount);
    }

    [Rpc(SendTo.Everyone)]
    public void SyncItemStateClientRpc(int ownerPlayerId, int amount)
    {
        if(ownerPlayerId != -1)
        {
            this.owner = GameSessionManager.Instance.playerControllerList[ownerPlayerId];
        }
        else
        {
            this.owner = null;
        }
        EnableItemMeshes(enableItemMeshes.Value);
        EnableItemPhysics(enableItemPhysics.Value);
        itemStatus.amount = amount;
    }

    public void OnSetInventorySlot()
    {
        if(inventorySlot)
        {
            if(InventoryManager.instance.storageSlotList.Contains(inventorySlot))
            {
                SetInStorageBoxRpc(true);
            }
            else if(InventoryManager.instance.inventorySlotList.Contains(inventorySlot))
            {
                SetInStorageBoxRpc(false);
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void SetInStorageBoxRpc(bool value)
    {
        inStorageBox.Value = value;
    }
}
