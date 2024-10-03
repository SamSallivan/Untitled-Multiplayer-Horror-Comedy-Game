using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Steamworks;
using Unity.Netcode;
using System.Linq;
using UnityEngine.SceneManagement;

public class I_InventoryItem : Interactable
{
    public event Action OnPickUp = delegate { };

    public NetworkVariable<int> ownerPlayerId = new(-1, writePerm: NetworkVariableWritePermission.Owner);

    public PlayerController owner;

    public PlayerController previousOwner;

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

        OnOwnerPlayerIdChanged(-1, ownerPlayerId.Value);
        //OnIsCurrentlyEquippedChanged(false, isCurrentlyEquipped.Value);
        OnEnableItemMeshesChanged(false, enableItemMeshes.Value);
        OnEnableItemPhysicsChanged(false, enableItemPhysics.Value);
        
        //isCurrentlyEquipped.OnValueChanged += OnIsCurrentlyEquippedChanged;
        enableItemMeshes.OnValueChanged += OnEnableItemMeshesChanged;
        enableItemPhysics.OnValueChanged += OnEnableItemPhysicsChanged;
        ownerPlayerId.OnValueChanged += OnOwnerPlayerIdChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        //isCurrentlyEquipped.OnValueChanged -= OnIsCurrentlyEquippedChanged;
        enableItemMeshes.OnValueChanged -= OnEnableItemMeshesChanged;
        enableItemPhysics.OnValueChanged -= OnEnableItemPhysicsChanged;
        ownerPlayerId.OnValueChanged -= OnOwnerPlayerIdChanged;
    }
    
    /*public void OnIsCurrentlyEquippedChanged(bool prevValue, bool newValue)
    {
        if (!isCurrentlyEquipped.Value)
        {
            if (owner != null)
            {
                Debug.Log(gameObject.name + " unequipped");
                owner.currentEquippedItem = null;
                owner.playerAnimationController.armAnimator.SetBool("Equipped", false);
                owner.playerAnimationController.armAnimator.SetTrigger("SwitchItem");
                owner.playerAnimationController.armAnimator.SetBool(itemData.equipAnimatorParameter, false);
            }

            if (TryGetComponent<ItemController>(out var itemController) && itemController.buttonHeld)
            {
                itemController.Cancel();
            }
        }
        else 
        {
            Debug.Log(gameObject.name + " equipped");
            owner.currentEquippedItem = this;
            owner.playerAnimationController.armAnimator.SetBool("Equipped", true);
            owner.playerAnimationController.armAnimator.SetTrigger("SwitchItem");
            owner.playerAnimationController.armAnimator.SetBool(itemData.equipAnimatorParameter, true);
        }
    }*/
    
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


    [Rpc(SendTo.Everyone)]
    public void EquipItemRpc(int playerId)
    {
        if (IsServer)
        {
            isCurrentlyEquipped.Value = true;
            enableItemMeshes.Value = true;
        }
        
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        playerController.currentEquippedItem = this;
        playerController.playerAnimationController.armAnimator.SetBool("Equipped", true);
        playerController.playerAnimationController.armAnimator.SetTrigger("SwitchItem");
        playerController.playerAnimationController.armAnimator.SetBool(itemData.equipAnimatorParameter, true);
    }
    
    [Rpc(SendTo.Everyone)]
    public void UnequipItemRpc(int playerId)
    {
        if (IsServer)
        {
            isCurrentlyEquipped.Value = false;
            enableItemMeshes.Value = false;
        }
        
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        playerController.currentEquippedItem = null;
        playerController.playerAnimationController.armAnimator.SetBool("Equipped", false);
        playerController.playerAnimationController.armAnimator.SetTrigger("SwitchItem");
        playerController.playerAnimationController.armAnimator.SetBool(itemData.equipAnimatorParameter, false);

        if (TryGetComponent<ItemController>(out var itemController) && itemController.buttonHeld)
        {
            itemController.Cancel();
        }
    }

	public virtual void LateUpdate()
	{
        if (owner != null)
        {
            if (owner.controlledByClient.Value && !owner.isPlayerDead.Value) 
            {
                Transform targetTransform = owner.equippedTransform;
                transform.position = targetTransform.TransformPoint(itemData.equipPosition);;
                transform.rotation = targetTransform.rotation;
                transform.Rotate(itemData.equipRotation);
            }
            else if (IsServer)
            {
                if (!owner.controlledByClient.Value && inStorageBox.Value)
                {
                    InventoryManager.instance.DestoryItemServerRpc(this.NetworkObject);
                }
                else if (!inStorageBox.Value)
                {
                    UnpocketItemRpc();
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
        UIManager.instance.interactionName.text += itemData.hasDurability ? " " + Mathf.Round(itemStatus.durability * 100) + "%" : "";

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

    [Rpc(SendTo.Everyone)]
    public void SetItemAmountRpc(int amount)
    { 
        itemStatus.amount = amount;
    }

    [Rpc(SendTo.Everyone)]
    public void SetItemDurarbilityRpc(float durability)
    {
        itemStatus.durability = durability;
    }
    
    [Rpc(SendTo.Server)]
    public void PocketItemRpc(int playerId)
    {
        ownerPlayerId.Value = playerId;
        enableItemMeshes.Value = false;
        enableItemPhysics.Value = false;

        if (GameSessionManager.Instance.gameStarted.Value)
        {
            SceneManager.MoveGameObjectToScene(gameObject,SceneManager.GetSceneAt(0));
        }
    }

    [Rpc(SendTo.Server)]
    public void UnpocketItemRpc()
    {
        transform.position = owner.headTransform.transform.position + owner.headTransform.transform.forward * 0.5f;
        transform.rotation = owner.headTransform.transform.rotation;
        
        ownerPlayerId.Value = -1;
        enableItemMeshes.Value = true;
        enableItemPhysics.Value = true;

        if (GameSessionManager.Instance.gameStarted.Value)
        {
            SceneManager.MoveGameObjectToScene(gameObject,SceneManager.GetSceneAt(1));
        }
    }
}
