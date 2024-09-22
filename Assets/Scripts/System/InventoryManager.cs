using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using Steamworks.Ugc;
using UnityEngine.Serialization;

public class InventoryManager : NetworkBehaviour
{
    public static InventoryManager instance;
    
    [Header("Values")]
    public bool inventoryOpened;

    public InventorySlot hoveredSlot;
    public InventorySlot selectedSlot;
    public I_InventoryItem selectedItem;
    public I_InventoryItem draggedItem;
    
    public int equippedSlotIndex;
    public I_InventoryItem equippedItem = null;

    public bool requireItemType;
    public List<I_InventoryItem> requireItemList;

    private I_InventoryItem detailObject;
    private bool detailRotationFix;
    public bool detailObjectDrag;
    public float inputDelay;

    public int slotPerRow = 8;
    public int slotPerColumn = 4;

    [Header("References")]
    public PlayerController playerController;
    public List<InventorySlot> inventorySlotList;
    public List<InventorySlot> storageSlotList;
    
    public static event Action<ItemData> OnPickUp = delegate{};
    public static event Action<I_InventoryItem> OnReturnRequiredType = delegate { };



    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        foreach(Transform child in UIManager.instance.inventorySlotGrid.transform){
            inventorySlotList.Add(child.GetComponent<InventorySlot>());
        }

        foreach(Transform child in UIManager.instance.storageSlotGrid.transform){
            storageSlotList.Add(child.GetComponent<InventorySlot>());
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerController)
        {
            if(GameSessionManager.Instance.localPlayerController)
            {
                playerController = GameSessionManager.Instance.localPlayerController;
            }
        }
        
        if (inputDelay < 1)
        {
            inputDelay += Time.fixedDeltaTime;
        }
        
        if (!inventoryOpened && GameSessionManager.Instance.localPlayerController)
        {
            if (GameSessionManager.Instance.localPlayerController.enableMovement && Input.GetKeyDown(KeyCode.Tab))
            {
                OpenInventory();
            }
        }
        else if (inventoryOpened)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
            {
                CloseInventory();
            }

            if (selectedItem != null && selectedItem.itemData != null)
            {
                // if (Input.GetKeyDown(KeyCode.G) && inputDelay >= 0.1f)
                // {
                //     DropItemFromInventory(selectedItem, 1);
                // }
            }

            // if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.W))
            // {
            //     selectedPosition.x += Input.GetKeyDown(KeyCode.D) ? 1 : 0;
            //     selectedPosition.x -= Input.GetKeyDown(KeyCode.A) ? 1 : 0;

            //     selectedPosition.y += Input.GetKeyDown(KeyCode.S) ? 1 : 0;
            //     selectedPosition.y -= Input.GetKeyDown(KeyCode.W) ? 1 : 0;

            //     if (selectedPosition.x < 0)
            //     {
            //         selectedPosition.x = slotPerRow - 1;
            //     }
            //     if (selectedPosition.x > slotPerRow - 1)
            //     {
            //         selectedPosition.x = 0;
            //     }
            //     if (selectedPosition.y < 0)
            //     {
            //         selectedPosition.y = slotPerColumn - 1;
            //     }
            //     if (selectedPosition.y > slotPerColumn - 1)
            //     {
            //         selectedPosition.y = 0;
            //     }

            //     selectedIndex = GetGridIndex(selectedPosition);
            //     hoveredIndex = selectedIndex;
            //     UpdateSelection();

            //     foreach (InventorySlot slot in inventorySlotList)
            //     {
            //         slot.background.color = new UnityEngine.Color(0.085f, 0.085f, 0.085f, 0.5f);
            //     }
            //     inventorySlotList[GetGridIndex(selectedPosition)].background.color = new UnityEngine.Color(0.85f, 0.85f, 0.85f, 0.5f);

            // }

            if (detailObject != null)
            {
                RotateDetailObject();
            }

            if (draggedItem != null)
            {
                UpdateDraggedItem();
            }
        }
    }

    public void UpdateDraggedItem()
    {
        Vector2 pos;
        Canvas myCanvas = UIManager.instance.gameplayUI.transform.parent.GetComponent<Canvas>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(myCanvas.transform as RectTransform, Input.mousePosition, myCanvas.worldCamera, out pos);
        UIManager.instance.draggedItemDisplay.transform.position = myCanvas.transform.TransformPoint(pos);
    }

    public void UpdateEquippedItem()
    {
        if (inventorySlotList[equippedSlotIndex].inventoryItem)
        {
            EquipItem(inventorySlotList[equippedSlotIndex].inventoryItem);
        }
        else
        {
            UnequipItem();
        }
    }

    public void OpenInventory()
    {
        //playerController.inventoryAudio.PlayInventoryOpen();
        inventoryOpened = true;
        inputDelay = 0;
        
        playerController.LockMovement(true);
        playerController.LockCamera(true);

        UIManager.instance.gameplayUI.SetActive(false);
        UIManager.instance.inventoryUI.SetActive(true);
        
        UIManager.instance.detailPanel.SetActive(true);
        UIManager.instance.StoragePanel.SetActive(false);

        UIManager.instance.inventorySlotGrid.SetActive(true);
        UIManager.instance.inventoryUI.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().text = "Inventory";

        LockCursor(false);

        selectedSlot = inventorySlotList[0];
        UpdateSelection();
        
        foreach (InventorySlot slot in inventorySlotList)
        {
            slot.outline.SetActive(false);
        }
        foreach (InventorySlot slot in storageSlotList)
        {
            slot.outline.SetActive(false);
        }
    }

    public void CloseInventory()
    {
        //playerController.inventoryAudio.PlayInventoryClose();
        inventoryOpened = false;

        playerController.LockMovement(false);
        playerController.LockCamera(false);

        UIManager.instance.inventoryUI.SetActive(false);
        UIManager.instance.gameplayUI.SetActive(true);

        LockCursor(true);
        
        UIManager.instance.inventorySlotGrid.SetActive(false);
    }

    public I_InventoryItem AddItemToInventory(I_InventoryItem inventoryItem)
    {
        RatingManager.instance.AddScore(50,"Found a " + inventoryItem.textName + "!");

        PocketItemRpc(inventoryItem.NetworkObject, playerController.NetworkObject);

        int temp = inventoryItem.itemStatus.amount;

        foreach (InventorySlot slot in inventorySlotList)
        {
            if(slot.inventoryItem != null)
            {
                I_InventoryItem item = slot.inventoryItem;

                if (item.itemData == inventoryItem.itemData && item.itemData.isStackable)
                {
                    while (item.itemStatus.amount < item.itemData.maxStackAmount && temp > 0)
                    {
                        item.itemStatus.amount++;
                        SetItemAmountRpc(item.NetworkObject, item.itemStatus.amount);
                        temp--;
                        item.inventorySlot.amount.text = "" + item.itemStatus.amount;
                    }

                    if (temp <= 0)
                    {
                        DestoryItemServerRpc(inventoryItem.NetworkObject);
                        return item;
                    }
                }
            }
        }

        if (temp > 0)
        {
            inventoryItem.itemStatus.amount = temp;
            SetItemAmountRpc(inventoryItem.NetworkObject, inventoryItem.itemStatus.amount);

            InventorySlot slot;
            if (inventorySlotList[equippedSlotIndex].inventoryItem == null)
            {
                slot = inventorySlotList[equippedSlotIndex];
            }
            else
            {
                slot = GetFirstEmptyInventorySlot();
            }

            /*InventorySlot slot;
            slot = GetFirstEmptyInventorySlot();
            if (inventorySlotList[equippedSlotIndex].inventoryItem == null)
            {
                equippedSlotIndex = slot.GetIndex();
            }*/

            if (slot != null)
            {
                inventoryItem.inventorySlot = slot;

                slot.inventoryItem = inventoryItem;
                slot.UpdateInventorySlotDisplay();
                
                if (slot.GetIndex() <= 3)
                {
                    equippedSlotIndex = slot.GetIndex();
                }
                
                if (slot.GetIndex() == equippedSlotIndex)
                {
                    UpdateEquippedItem();
                }

                return inventoryItem;
            }
            else
            {
                UnpocketItemRpc(inventoryItem.NetworkObject, playerController.NetworkObject);
                return null;
            }
        }

        return null;
    }

    public InventorySlot GetFirstEmptyInventorySlot()
    {
        foreach(InventorySlot slot in inventorySlotList){
            if(slot.inventoryItem == null){
                return slot;
            }
        }
        return null;
    }

    public void DropAllItemsFromInventory()
    {
        foreach (InventorySlot slot in inventorySlotList)
        {
            if (slot.inventoryItem)
            {
                DropItemFromInventory(slot.inventoryItem);
            }
        }
    }

    public void DropItemFromInventory(I_InventoryItem inventoryItem)
    {
        if (equippedItem == inventoryItem)
        {
            UnequipItem();
        }

        ClearInventorySlot(inventoryItem.inventorySlot);
        
        UpdateSelection();

        inventoryItem.inventorySlot = null;
        UnpocketItemRpc(inventoryItem.NetworkObject, playerController.NetworkObject);
    }

    public void DropItemFromInventory(I_InventoryItem inventoryItem, int amount = 1)
    {
        //playerController.inventoryAudio.PlayItemDrop();

        if (inventoryItem.itemData.isStackable)
        {
            if (inventoryItem.itemStatus.amount > amount)
            {
                inventoryItem.itemStatus.amount -= amount;
                SetItemAmountRpc(inventoryItem.NetworkObject, inventoryItem.itemStatus.amount);
                inventoryItem.inventorySlot.amount.text = "" + inventoryItem.itemStatus.amount;
                InstantiateUnpocketedItemServerRpc(GameSessionManager.Instance.GetItemIndex(inventoryItem.itemData), amount, playerController.NetworkObject);

            }
            else
            {
                if (equippedItem == inventoryItem)
                {
                    UnequipItem();
                }

                ClearInventorySlot(inventoryItem.inventorySlot);
                inventoryItem.inventorySlot = null;
                UnpocketItemRpc(inventoryItem.NetworkObject, playerController.NetworkObject);
            }
        }
        else
        {
            if (equippedItem == inventoryItem)
            {
                UnequipItem();
            }

            ClearInventorySlot(inventoryItem.inventorySlot);
            inventoryItem.inventorySlot = null;
            UnpocketItemRpc(inventoryItem.NetworkObject, playerController.NetworkObject);
            //droppedObject.GetComponentInChildren<I_InventoryItem>().itemStatus.durability = inventoryItem.status.durability;
        }

        UpdateSelection();
    }

    public void DropItemFromInventory(ItemData itemData, int amount)
    {
        //materialCount -= x;

        int temp = amount;

        foreach (InventorySlot slot in inventorySlotList)
        {
            if(slot.inventoryItem)
            {
                I_InventoryItem item = slot.inventoryItem;
                if (item.itemData == itemData && item.itemData.isStackable)
                {
                    while (item.itemStatus.amount > 0 && temp > 0)
                    {
                        item.itemStatus.amount--;
                        SetItemAmountRpc(item.NetworkObject, item.itemStatus.amount);
                        temp--;
                        item.inventorySlot.amount.text = "" + item.itemStatus.amount;

                        //remove from inventory if <= 0
                    }

                    if (temp <= 0)
                    {
                        return;
                    }

                    if (item.itemStatus.amount <= 0)
                    {
                        if (equippedItem == item)
                        {
                            UnequipItem();
                        }

                        ClearInventorySlot(item.inventorySlot);
                        item.inventorySlot = null;
                        UnpocketItemRpc(item.NetworkObject, playerController.NetworkObject);

                    }
                    break;
                }
            }
        }
    }

    public void RemoveItem(I_InventoryItem inventoryItem, int amount = 1)
    {
        if (inventoryItem.itemData.isStackable)
        {
            if (inventoryItem.itemStatus.amount > amount)
            {
                inventoryItem.itemStatus.amount -= amount;
                SetItemAmountRpc(inventoryItem.NetworkObject, inventoryItem.itemStatus.amount);
                inventoryItem.inventorySlot.amount.text = "" + inventoryItem.itemStatus.amount;

            }
            else
            {
                if (equippedItem == inventoryItem)
                {
                    UnequipItem();
                }

                ClearInventorySlot(inventoryItem.inventorySlot);
                //Destory inventoryItem on server
            }
        }
        else
        {
            if (equippedItem == inventoryItem)
            {
                UnequipItem();
            }

            ClearInventorySlot(inventoryItem.inventorySlot);
            //Destory inventoryItem on server
        }

        UpdateSelection();
    }

    /*[Rpc(SendTo.Server)]
    public void InstantiateUnpocketedItemServerRpc(NetworkObjectReference inventoryItem, int amount)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject))
        {
            var gameObject = Instantiate(inventoryItemObject.gameObject);
            gameObject.GetComponent<I_InventoryItem>().itemStatus.amount = amount;
            gameObject.GetComponent<NetworkObject>().Spawn();
            UnpocketItemRpc(gameObject.GetComponent<NetworkObject>(), playerController.NetworkObject);
        }
    }*/

    [Rpc(SendTo.Server)]
    public void InstantiateUnpocketedItemServerRpc(int itemIndex, int amount, NetworkObjectReference playerController)
    {
        if (playerController.TryGet(out NetworkObject playerControllerObject))
        {
            var gameObject = Instantiate(GameSessionManager.Instance.itemList.itemDataList[itemIndex].dropObject, position: playerControllerObject.GetComponent<PlayerController>().headTransform.transform.position + playerControllerObject.GetComponent<PlayerController>().headTransform.transform.forward * 0.5f, playerControllerObject.GetComponent<PlayerController>().headTransform.transform.rotation);
            gameObject.GetComponent<I_InventoryItem>().itemStatus.amount = amount;
            gameObject.GetComponent<NetworkObject>().Spawn();
            UnpocketItemRpc(gameObject.GetComponent<NetworkObject>(), playerControllerObject);
        }
    }

    [Rpc(SendTo.Server)]
    public void InstantiatePocketedItemServerRpc(int itemIndex, int amount, int storageSlotIndex, NetworkObjectReference playerController)
    {
        if (playerController.TryGet(out NetworkObject playerControllerObject))
        {
            var gameObject = Instantiate(GameSessionManager.Instance.itemList.itemDataList[itemIndex].dropObject);
            gameObject.GetComponent<I_InventoryItem>().itemStatus.amount = amount;
            gameObject.GetComponent<NetworkObject>().Spawn();
            PocketItemRpc(gameObject.GetComponent<NetworkObject>(), playerControllerObject);
            InstantiatePocketedItemClientRpc(storageSlotIndex, gameObject.GetComponent<NetworkObject>(), playerControllerObject);
        }
    }
    
    [Rpc(SendTo.Everyone)]
    public void InstantiatePocketedItemClientRpc(int storageSlotIndex, NetworkObjectReference inventoryItem, NetworkObjectReference playerController)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject) && playerControllerObject.GetComponent<PlayerController>() == GameSessionManager.Instance.localPlayerController)
        {
            storageSlotList[storageSlotIndex].inventoryItem = inventoryItemObject.GetComponent<I_InventoryItem>();
            storageSlotList[storageSlotIndex].UpdateInventorySlotDisplay();
            inventoryItemObject.GetComponent<I_InventoryItem>().inventorySlot = storageSlotList[storageSlotIndex];
        }
    }

    [Rpc(SendTo.Server)]
    public void InstantiateReplaceItemServerRpc(int itemIndex, NetworkObjectReference playerController)
    {
        if (playerController.TryGet(out NetworkObject playerControllerObject))
        {
            var gameObject = Instantiate(GameSessionManager.Instance.itemList.itemDataList[itemIndex].dropObject);
            gameObject.GetComponent<NetworkObject>().Spawn();
            InstantiateReplaceItemClientRpc(gameObject.GetComponent<NetworkObject>(), playerControllerObject);
        }
    }
    
    [Rpc(SendTo.Everyone)]
    public void InstantiateReplaceItemClientRpc(NetworkObjectReference inventoryItem, NetworkObjectReference playerController)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject) && playerControllerObject.GetComponent<PlayerController>() == GameSessionManager.Instance.localPlayerController)
        {
            AddItemToInventory(inventoryItemObject.GetComponent<I_InventoryItem>());
        }
    }

    [Rpc(SendTo.Server)]
    public void DestoryItemServerRpc(NetworkObjectReference inventoryItem)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject))
        {
            inventoryItemObject.Despawn();
            Destroy(inventoryItemObject.gameObject);
        }
    }

    [Rpc(SendTo.Server)]
    public void PocketItemRpc(NetworkObjectReference inventoryItem, NetworkObjectReference playerController)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject))
        {
            //Debug.Log(inventoryItemObject.GetComponent<I_InventoryItem>());
            //inventoryItemObject.GetComponent<I_InventoryItem>().owner = playerControllerObject.GetComponent<PlayerController>();
            if (inventoryItemObject.GetComponent<I_InventoryItem>().IsOwner)
            {
                inventoryItemObject.GetComponent<I_InventoryItem>().ownerPlayerId.Value = playerControllerObject.GetComponent<PlayerController>().localPlayerId;
                inventoryItemObject.GetComponent<I_InventoryItem>().enableItemMeshes.Value = false;
                inventoryItemObject.GetComponent<I_InventoryItem>().enableItemPhysics.Value = false;
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    public void UnpocketItemRpc(NetworkObjectReference inventoryItem)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject))
        {
            if (inventoryItemObject.GetComponent<I_InventoryItem>().IsOwner)
            {
                inventoryItemObject.GetComponent<I_InventoryItem>().ownerPlayerId.Value = -1;
                inventoryItemObject.GetComponent<I_InventoryItem>().enableItemMeshes.Value = true;
                inventoryItemObject.GetComponent<I_InventoryItem>().enableItemPhysics.Value = true;
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void UnpocketItemRpc(NetworkObjectReference inventoryItem, NetworkObjectReference playerController)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject))
        {
            if (inventoryItemObject.GetComponent<I_InventoryItem>().IsOwner)
            {
                inventoryItemObject.GetComponent<I_InventoryItem>().ownerPlayerId.Value = -1;
                inventoryItemObject.GetComponent<I_InventoryItem>().enableItemMeshes.Value = true;
                inventoryItemObject.GetComponent<I_InventoryItem>().enableItemPhysics.Value = true;
                inventoryItemObject.transform.position = playerControllerObject.GetComponent<PlayerController>().headTransform.transform.position + playerControllerObject.GetComponent<PlayerController>().headTransform.transform.forward * 0.5f;
                inventoryItemObject.transform.rotation = playerControllerObject.GetComponent<PlayerController>().headTransform.transform.rotation;
            }
        }
    }

    public void ClearInventorySlot(InventorySlot slot)
    {
        slot.inventoryItem = null;
        slot.image.sprite = null;
        slot.image.color = new UnityEngine.Color(1, 1, 1, 0);
        slot.name.text = "";
        slot.amount.text = "";
    }

    public void EquipItem(I_InventoryItem inventoryItem)
    {
        if (!inventoryItem)
        {
            return;
        }
        
        UnequipItem();

        equippedItem = inventoryItem;
        //playerController.currentEquippedItem = inventoryItem;
        /*playerController.playerAnimationController.armAnimator.SetBool("Equipped", true);
        playerController.playerAnimationController.armAnimator.SetTrigger("SwitchItem");
        playerController.playerAnimationController.armAnimator.SetBool(inventoryItem.itemData.equipAnimatorParameter, true);*/

        //playerController.inventoryAudio.PlayItemEquip();
        
        EquipItemRpc(inventoryItem.NetworkObject, playerController.NetworkObject);

        if (playerController.targetInteractable != null)
        {
            playerController.targetInteractable.Target();
        }
    }

    [Rpc(SendTo.Everyone)]
    public void EquipItemRpc(NetworkObjectReference inventoryItem, NetworkObjectReference playerController)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject))
        {
            inventoryItemObject.GetComponent<I_InventoryItem>().OnEquip();
            playerControllerObject.GetComponent<PlayerController>().currentEquippedItem = inventoryItemObject.GetComponent<I_InventoryItem>();
            playerControllerObject.GetComponent<PlayerController>().playerAnimationController.armAnimator.SetBool("Equipped", true);
            playerControllerObject.GetComponent<PlayerController>().playerAnimationController.armAnimator.SetTrigger("SwitchItem");
            playerControllerObject.GetComponent<PlayerController>().playerAnimationController.armAnimator.SetBool(inventoryItemObject.GetComponent<I_InventoryItem>().itemData.equipAnimatorParameter, true);
        }
    }

    public void UnequipItem()//(ItemData.EquipType type)
    {
        //playerController.inventoryAudio.PlayItemUnequip();
        
        /*playerController.playerAnimationController.armAnimator.SetBool("Equipped", false);
        playerController.playerAnimationController.armAnimator.SetTrigger("SwitchItem");*/

        if (equippedItem != null && equippedItem.inventorySlot != null)
        {
            //playerController.playerAnimationController.armAnimator.SetBool(equippedItem.itemData.equipAnimatorParameter, false);
            UnequipItemRpc(equippedItem.NetworkObject, playerController.NetworkObject);
        }
        
        equippedItem = null;
    }

    [Rpc(SendTo.Everyone)]
    public void UnequipItemRpc(NetworkObjectReference inventoryItem, NetworkObjectReference playerController)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject))
        {
            inventoryItemObject.GetComponent<I_InventoryItem>().OnUnequip();
            playerControllerObject.GetComponent<PlayerController>().currentEquippedItem = null;
            playerControllerObject.GetComponent<PlayerController>().playerAnimationController.armAnimator.SetBool("Equipped", false);
            playerControllerObject.GetComponent<PlayerController>().playerAnimationController.armAnimator.SetTrigger("SwitchItem");
            playerControllerObject.GetComponent<PlayerController>().playerAnimationController.armAnimator.SetBool(inventoryItemObject.GetComponent<I_InventoryItem>().itemData.equipAnimatorParameter, false);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void SetItemAmountRpc(NetworkObjectReference inventoryItem, int amount)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject))
        {
            inventoryItemObject.GetComponent<I_InventoryItem>().itemStatus.amount = amount;
        }
    }

    [Rpc(SendTo.Everyone)]
    public void SetItemDurarbilityRpc(NetworkObjectReference inventoryItem, float durability)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject))
        {
            inventoryItemObject.GetComponent<I_InventoryItem>().itemStatus.durability = durability;
        }
    }

    public I_InventoryItem FindInventoryItem(string name)
    {
        foreach (InventorySlot slot in inventorySlotList)
        {
            if (slot.inventoryItem && slot.inventoryItem.itemData.title == name)
            {
                return slot.inventoryItem;
            }
        }

        return null;
    }

    public void UpdateSelection(bool deleteOnUnselect = true)
    {

        foreach (InventorySlot slot in inventorySlotList)
        {
            slot.background.color = new UnityEngine.Color(0.085f, 0.085f, 0.085f, 0.5f);
        }
        foreach (InventorySlot slot in storageSlotList)
        {
            slot.background.color = new UnityEngine.Color(0.085f, 0.085f, 0.085f, 0.5f);
        }

        if(selectedSlot)
        {
            selectedSlot.background.color = new UnityEngine.Color(0.85f, 0.85f, 0.85f, 0.5f);

            if (selectedSlot.inventoryItem != null)
            {
                selectedItem = selectedSlot.inventoryItem;
            }
            else
            {
                selectedItem = null;
            }
        }

        if (selectedItem != null && selectedItem.itemData != null)
        {
            CreateDetailObject();
        }
        else if(deleteOnUnselect)
        {
            DeleteDetailObject();
        }
    }

    public void CreateDetailObject()
    {
        //if (UIManager.instance.detailName.text != selectedItem.itemData.name)
        if (detailObject != selectedItem)
        {
            detailObject = selectedItem;
            UIManager.instance.detailName.text = selectedItem.itemData.title;
            UIManager.instance.detailDescription.text = selectedItem.itemData.description;

            /*while (UIManager.instance.detailObjectPivot.childCount > 0)
            {
                Destroy(UIManager.instance.detailObjectPivot.GetChild(0).gameObject);
            }*/

            foreach (Transform child in UIManager.instance.detailObjectPivot)
            {
                Destroy(child.gameObject);
            }

            GameObject detailGameObject = Instantiate(selectedItem.itemData.dropObject, UIManager.instance.detailObjectPivot);
            detailGameObject.transform.localScale *= 1200;
            detailGameObject.transform.localScale *= selectedItem.itemData.examineScale;
            detailGameObject.transform.localRotation = selectedItem.itemData.examineRotation;
            foreach (Transform child in UIManager.instance.detailObjectPivot.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = 0;
            }

            Destroy(detailGameObject.transform.GetComponentInChildren<Interactable>());
            Destroy(detailGameObject.GetComponent<NetworkRigidbody>());
            Destroy(detailGameObject.GetComponent<NetworkObject>());
            Destroy(detailGameObject.GetComponent<NetworkTransform>());
            Destroy(detailGameObject.GetComponent<Rigidbody>());
            foreach (Collider collider in detailGameObject.GetComponents<Collider>())
            {
                Destroy(collider);
            }
        }
    }

    public void DeleteDetailObject()
    {
        detailObject = null;
        if (UIManager.instance.detailObjectPivot.childCount > 0)
        {
            Destroy(UIManager.instance.detailObjectPivot.GetChild(0).gameObject);
        }
        UIManager.instance.detailName.text = "";
        UIManager.instance.detailDescription.text = "";
    }

    public void RotateDetailObject()
    {
        Vector2 lookVector = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        Vector2 rotateValue = new Vector2();

        if (UIManager.instance.detailObjectInBound && Input.GetMouseButtonDown(0))
        {
            detailObjectDrag = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            detailObjectDrag = false;

        }

        if (detailObjectDrag)
        {
            rotateValue.x = -(lookVector.x * 2.5f);
            rotateValue.y = lookVector.y * 2.5f;
            UIManager.instance.detailObjectPivot.GetChild(0).transform.Rotate(playerController.headTransform.GetChild(0).transform.up, rotateValue.x, Space.World);
            UIManager.instance.detailObjectPivot.GetChild(0).transform.Rotate(playerController.headTransform.GetChild(0).transform.right, rotateValue.y, Space.World);
            // UIManager.instance.detailObjectPivot.GetChild(0).transform.Rotate(Vector3.up, rotateValue.x, Space.World);
            // UIManager.instance.detailObjectPivot.GetChild(0).transform.Rotate(Vector3.right, rotateValue.y, Space.World);
            detailRotationFix = true;
        }
        else
        {
            Quaternion currentRotation = UIManager.instance.detailObjectPivot.GetChild(0).transform.localRotation;

            if (Quaternion.Angle(currentRotation, detailObject.itemData.examineRotation) > 2f && detailRotationFix)
            {
                UIManager.instance.detailObjectPivot.GetChild(0).transform.localRotation = Quaternion.Slerp(currentRotation, detailObject.itemData.examineRotation, Time.deltaTime * 5f);
            }
            else
            {
                detailRotationFix = false;
                UIManager.instance.detailObjectPivot.GetChild(0).transform.Rotate(playerController.headTransform.GetChild(0).transform.up, 0.2f, Space.World);
                //UIManager.instance.detailObjectPivot.GetChild(0).transform.Rotate(Vector3.up, 0.2f, Space.World);
            }
        }
    }

    public int2 GetGridPosition(int index)
    {
        return (new int2(index % slotPerRow, index / slotPerRow));
    }

    public int GetGridIndex(int2 position)
    {
        return ((position.y) * slotPerRow + position.x);
    }

    public void LockCursor(bool lockCursor)
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
    }

    public void DiscardEquippedItem()
    {
        if (equippedItem != null && instance.equippedItem.itemData != null)
        {
            DropItemFromInventory(instance.equippedItem, 1);
        }
    }

    public void DiscardSelectedItem()
    {
        if (selectedItem != null && selectedItem.itemData != null)
        {
            if (inputDelay >= 0.1f)
            {
                DropItemFromInventory(selectedItem, 1);
            }
        }
    }

    public void SwitchEquipedItem(int value)
    {
        if (inputDelay > 0.1f) // forward
        {
            inputDelay = 0f;
            equippedSlotIndex += -value;

            if (equippedSlotIndex < 0)
            {
                equippedSlotIndex = 3;
            }
            else if (equippedSlotIndex > 3)
            {
                equippedSlotIndex = 0;
            }

            UpdateEquippedItem();
        }
}
}
