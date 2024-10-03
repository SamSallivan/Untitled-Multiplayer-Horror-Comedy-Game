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
using UnityEngine.SceneManagement;

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

    private I_InventoryItem detailObject;
    private bool detailRotationFix;
    public bool detailObjectDrag;
    public float inputDelay;

    /*public int slotPerRow = 8;
    public int slotPerColumn = 4;*/

    [Header("References")]
    public PlayerController playerController;
    public List<InventorySlot> inventorySlotList;
    public List<InventorySlot> storageSlotList;

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
        
        if (inventoryOpened)
        {
            RotateDetailObject();
            UpdateDraggedItem();
        }
        
        /*if (inventoryOpened)
        {
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
        }*/
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

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        UIManager.instance.inventorySlotGrid.SetActive(false);
    }

    public I_InventoryItem AddItemToInventory(I_InventoryItem inventoryItem)
    {
        inventoryItem.PocketItemRpc(playerController.localPlayerId);

        int temp = inventoryItem.itemStatus.amount;

        if (inventoryItem.itemData.isStackable)
        {
            foreach (InventorySlot slot in inventorySlotList)
            {
                if (slot.inventoryItem != null)
                {
                    I_InventoryItem item = slot.inventoryItem;

                    if (item.itemData == inventoryItem.itemData)
                    {
                        while (item.itemStatus.amount < item.itemData.maxStackAmount && temp > 0)
                        {
                            item.itemStatus.amount++;
                            item.SetItemAmountRpc(item.itemStatus.amount);
                            temp--;
                            item.inventorySlot.amount.text = "" + item.itemStatus.amount;
                        }

                        if (temp <= 0)
                        {
                            inventoryItem.DestoryItemServerRpc();
                            return item;
                        }
                    }
                }
            }
        }

        if (temp > 0)
        {
            inventoryItem.itemStatus.amount = temp;
            inventoryItem.SetItemAmountRpc(inventoryItem.itemStatus.amount);

            InventorySlot slot;
            if (inventorySlotList[equippedSlotIndex].inventoryItem == null)
            {
                slot = inventorySlotList[equippedSlotIndex];
            }
            else
            {
                slot = GetFirstEmptyInventorySlot();
            }


            if (slot != null)
            {
                if (inventoryItem.itemData.isHeavy && slot.GetIndex() > 3)
                {
                    slot = null;
                }
            }

            if (slot != null)
            {
                inventoryItem.inventorySlot = slot;

                slot.inventoryItem = inventoryItem;
                
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
                inventoryItem.UnpocketItemRpc();
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
        inventoryItem.UnpocketItemRpc();
    }

    public void DropItemFromInventory(I_InventoryItem inventoryItem, int amount = 1)
    {
        //playerController.inventoryAudio.PlayItemDrop();

        if (inventoryItem.itemData.isStackable)
        {
            if (inventoryItem.itemStatus.amount > amount)
            {
                inventoryItem.itemStatus.amount -= amount;
                inventoryItem.SetItemAmountRpc(inventoryItem.itemStatus.amount);
                inventoryItem.inventorySlot.amount.text = "" + inventoryItem.itemStatus.amount;
                InstantiateUnpocketedItemServerRpc(GameSessionManager.Instance.GetItemIndex(inventoryItem.itemData), amount, playerController.localPlayerId);

            }
            else
            {
                if (equippedItem == inventoryItem)
                {
                    UnequipItem();
                }

                ClearInventorySlot(inventoryItem.inventorySlot);
                inventoryItem.inventorySlot = null;
                inventoryItem.UnpocketItemRpc();
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
            inventoryItem.UnpocketItemRpc();
            //droppedObject.GetComponentInChildren<I_InventoryItem>().itemStatus.durability = inventoryItem.status.durability;
        }

        UpdateSelection();
    }

    /*public void DropItemFromInventory(ItemData itemData, int amount)
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
                        item.SetItemAmountRpc(item.itemStatus.amount);
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
                        item.UnpocketItemRpc();

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
                inventoryItem.SetItemAmountRpc(inventoryItem.itemStatus.amount);
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
    }*/

    [Rpc(SendTo.Server)]
    public void InstantiateUnpocketedItemServerRpc(int itemIndex, int amount, int playerId)
    {
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        var gameObject = Instantiate(GameSessionManager.Instance.itemList.itemDataList[itemIndex].dropObject, position: playerController.headTransform.transform.position + playerController.headTransform.transform.forward * 0.5f, playerController.headTransform.transform.rotation);
        gameObject.GetComponent<I_InventoryItem>().itemStatus.amount = amount;
        gameObject.GetComponent<NetworkObject>().Spawn();
        gameObject.GetComponent<I_InventoryItem>().UnpocketItemRpc();
    }

    [Rpc(SendTo.Server)]
    public void InstantiatePocketedItemServerRpc(int itemIndex, int amount, float durability, int storageSlotIndex, int playerId)
    {
            var gameObject = Instantiate(GameSessionManager.Instance.itemList.itemDataList[itemIndex].dropObject);
            gameObject.GetComponent<I_InventoryItem>().itemStatus.amount = amount;
            gameObject.GetComponent<I_InventoryItem>().itemStatus.durability = durability;
            gameObject.GetComponent<NetworkObject>().Spawn();
            gameObject.GetComponent<I_InventoryItem>().PocketItemRpc(playerId);
            InstantiatePocketedItemClientRpc(storageSlotIndex, gameObject.GetComponent<NetworkObject>(), playerId);
    }
    
    [Rpc(SendTo.Everyone)]
    public void InstantiatePocketedItemClientRpc(int storageSlotIndex, NetworkObjectReference inventoryItem, int playerId)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerId == GameSessionManager.Instance.localPlayerController.localPlayerId)
        {
            storageSlotList[storageSlotIndex].inventoryItem = inventoryItemObject.GetComponent<I_InventoryItem>();
            inventoryItemObject.GetComponent<I_InventoryItem>().inventorySlot = storageSlotList[storageSlotIndex];
        }
    }

    [Rpc(SendTo.Server)]
    public void InstantiateReplaceItemServerRpc(int itemIndex, int playerId)
    {
        var gameObject = Instantiate(GameSessionManager.Instance.itemList.itemDataList[itemIndex].dropObject);
        gameObject.GetComponent<NetworkObject>().Spawn();
        InstantiateReplaceItemClientRpc(gameObject.GetComponent<NetworkObject>(), playerId);
    }
    
    [Rpc(SendTo.Everyone)]
    public void InstantiateReplaceItemClientRpc(NetworkObjectReference inventoryItem, int playerId)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerId == GameSessionManager.Instance.localPlayerController.localPlayerId)
        {
            AddItemToInventory(inventoryItemObject.GetComponent<I_InventoryItem>());
        }
    }

    public void ClearInventorySlot(InventorySlot slot)
    {
        if (slot == null)
        {
            return;
        }
        
        slot.inventoryItem = null;
        slot.image.sprite = null;
        slot.image.color = new UnityEngine.Color(1, 1, 1, 0);
        slot.amount.text = "";
        slot.durabilityFill.fillAmount = 0;
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
    
    public void EquipItem(I_InventoryItem inventoryItem)
    {
        if (!inventoryItem)
        {
            return;
        }
        
        UnequipItem();

        equippedItem = inventoryItem;

        //playerController.inventoryAudio.PlayItemEquip();
        
        inventoryItem.EquipItemRpc(playerController.localPlayerId);

        if (playerController.targetInteractable != null)
        {
            playerController.targetInteractable.Target();
        }
    }
    
    public void UnequipItem()
    {
        //playerController.inventoryAudio.PlayItemUnequip();

        if (equippedItem != null && equippedItem.inventorySlot != null)
        {
            equippedItem.UnequipItemRpc(playerController.localPlayerId);

            I_InventoryItem item = equippedItem;
            equippedItem = null;
        
            if (item.itemData.isHeavy)
            {
                DropItemFromInventory(item);
                UIManager.instance.shortcutSlotHeavyBackground.enabled = false;
            }
        }
    }

    /*public I_InventoryItem FindInventoryItem(string name)
    {
        foreach (InventorySlot slot in inventorySlotList)
        {
            if (slot.inventoryItem && slot.inventoryItem.itemData.title == name)
            {
                return slot.inventoryItem;
            }
        }

        return null;
    }*/

    public void UpdateSelection()
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
        else
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
        if (detailObject == null)
        {
            return;
        }

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

    public void UpdateDraggedItem()
    {
        if (draggedItem == null)
        {
            return;
        }

        Vector2 pos;
        Canvas myCanvas = UIManager.instance.gameplayUI.transform.parent.GetComponent<Canvas>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(myCanvas.transform as RectTransform, Input.mousePosition, myCanvas.worldCamera, out pos);
        UIManager.instance.draggedItemDisplay.transform.position = myCanvas.transform.TransformPoint(pos);
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
