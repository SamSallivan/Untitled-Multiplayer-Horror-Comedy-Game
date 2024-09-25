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

    public NetworkVariable<int> ownerPlayerId = new(-1, writePerm: NetworkVariableWritePermission.Owner);

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
    
    public NetworkVariable<bool> firstPickup = new(true, writePerm: NetworkVariableWritePermission.Owner);
    
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
            NetworkObject.DestroyWithScene = false;
            SyncItemStateClientRpc(itemStatus.amount, itemStatus.durability);
        }
        else
        {
            SyncItemStateServerRpc();
        }

        OnEnableItemMeshesChanged(false, enableItemMeshes.Value);
        OnEnableItemPhysicsChanged(false, enableItemPhysics.Value);
        OnOwnerPlayerIdChanged(-1, ownerPlayerId.Value);
        
        enableItemMeshes.OnValueChanged += OnEnableItemMeshesChanged;
        enableItemPhysics.OnValueChanged += OnEnableItemPhysicsChanged;
        ownerPlayerId.OnValueChanged += OnOwnerPlayerIdChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        enableItemMeshes.OnValueChanged -= OnEnableItemMeshesChanged;
        enableItemPhysics.OnValueChanged -= OnEnableItemPhysicsChanged;
        ownerPlayerId.OnValueChanged -= OnOwnerPlayerIdChanged;
    }

    public void OnEnableItemMeshesChanged(bool prevValue, bool newValue)
    {
        MeshRenderer[] meshRenderers = base.gameObject.GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].enabled = enableItemMeshes.Value;
        }

        SkinnedMeshRenderer[] skinnedMeshRenderers = base.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int j = 0; j < skinnedMeshRenderers.Length; j++)
        {
            skinnedMeshRenderers[j].enabled = enableItemMeshes.Value;
            Debug.Log("DISABLING/ENABLING SKINNEDMESH: " + skinnedMeshRenderers[j].gameObject.name);
        }
    }

    public void OnEnableItemPhysicsChanged(bool prevValue, bool newValue)
    {
        base.gameObject.GetComponent<Rigidbody>().isKinematic = !enableItemPhysics.Value;
        Collider[] colliders = base.gameObject.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = enableItemPhysics.Value;
        }
    }

    public void OnOwnerPlayerIdChanged(int prevValue, int newValue)
    {
        if (newValue != -1)
        {
            owner = GameSessionManager.Instance.playerControllerList[ownerPlayerId.Value];
        }
        else
        {
            owner = null;
        }
    }

    public void OnEquip()
    {
        if (IsServer)
        {
            isCurrentlyEquipped.Value = true;
            enableItemMeshes.Value = true;
        }

        /*if (ownerPlayerId.Value != -1 && !string.IsNullOrEmpty(itemData.equipAnimatorParameter))
        {
            owner.playerAnimationController.armAnimator.SetBool(itemData.equipAnimatorParameter, true);
        }*/
    }
    
    public void OnUnequip()
    {
        if (IsServer)
        {
            isCurrentlyEquipped.Value = false;
            enableItemMeshes.Value = false;
        }

        /*if (owner && !string.IsNullOrEmpty(itemData.equipAnimatorParameter))
        {
            owner.playerAnimationController.armAnimator.SetBool(itemData.equipAnimatorParameter, false);
        }*/

        if (TryGetComponent<ItemController>(out var itemController) && itemController.buttonHeld)
        {
            itemController.Cancel();
        }
    }

	public virtual void LateUpdate()
	{
        if (owner != null)
        {
            if (owner.controlledByClient && !owner.isPlayerDead.Value) 
            {
                Transform targetTransform = owner.equippedTransform;
                transform.position = targetTransform.TransformPoint(itemData.equipPosition);;
                transform.rotation = targetTransform.rotation;
                transform.Rotate(itemData.equipRotation);
            }
            else if (IsServer)
            {
                if (!owner.controlledByClient && inStorageBox.Value)
                {
                    InventoryManager.instance.DestoryItemServerRpc(this.NetworkObject);
                }
                else if (!inStorageBox.Value)
                {
                    InventoryManager.instance.UnpocketItemRpc(this.NetworkObject);
                }
            }
        }
    }

    public override IEnumerator InteractionEvent()
    {
        if(itemData != null)
        {
            if ( firstPickup.Value)
            {
                ChangeFirstPickupServerRpc();
                RatingManager.instance.AddScore(itemData.discoverScore,"Found a " + textName + "!");
            }
            I_InventoryItem item = InventoryManager.instance.AddItemToInventory(this);
            OnPickUp?.Invoke();
            if (item != null)
            {
                if (openInventoryOnPickUp)
                {
                    InventoryManager.instance.OpenInventory();
                    InventoryManager.instance.selectedSlot = item.inventorySlot;
                }
            }
        }
        yield return null; 
    }

    
    [Rpc(SendTo.Server)]
    public void ChangeFirstPickupServerRpc()
    {
        firstPickup.Value = false;
    }

	/*public void EnableItemMeshes(bool enable)
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
    }*/

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
        UIManager.instance.interactionName.text += itemData.hasDurability ? " " + itemStatus.durability * 100 + "%" : "";

        if (textPrompt != "" && interactionType != InteractionType.None)
        {
            UIManager.instance.interactionPrompt.text = "[E] ";
            UIManager.instance.interactionPrompt.text += activated ? textPromptActivated : textPrompt;
        }
    }

    [Rpc(SendTo.Server)]
    public void SyncItemStateServerRpc()
    {
        SyncItemStateClientRpc(itemStatus.amount, itemStatus.durability);
    }

    [Rpc(SendTo.Everyone)]
    public void SyncItemStateClientRpc(int amount, float durability)
    {
        itemStatus.amount = amount;
        itemStatus.durability = durability;
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
