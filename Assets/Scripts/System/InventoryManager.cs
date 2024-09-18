using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using Steamworks.Ugc;

public class InventoryManager : NetworkBehaviour
{
    public static InventoryManager instance;
    public bool activated;

    public InventorySlot slotPrefab;

    public List<InventorySlot> inventorySlotList;
    public List<InventorySlot> storageSlotList;

    //public int hoveredIndex;
    public InventorySlot hoveredSlot;
    //public int selectedIndex;
    public InventorySlot selectedSlot;
    public int2 selectedPosition;
    public I_InventoryItem selectedItem;
    public I_InventoryItem draggedItem;
    public int equippedSlotIndex;

    public I_InventoryItem equippedItem = null;

    public int slotPerRow = 8;
    public int slotPerColumn = 4;

    private I_InventoryItem detailObject;
    private bool detailRotationFix;
    public bool detailObjectDrag;
    public float inputDelay;

    public static event Action<ItemData> OnPickUp = delegate{};
    public static event Action<I_InventoryItem> OnReturnRequiredType = delegate { };

    public bool requireItemType;
    public List<I_InventoryItem> requireItemList;

    public PlayerController playerController;


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
        
        if (!activated && GameSessionManager.Instance.localPlayerController)
        {
            if (GameSessionManager.Instance.localPlayerController.enableMovement && Input.GetKeyDown(KeyCode.Tab))
            {
                OpenInventory();
            }

            // if (Input.GetKeyDown(KeyCode.G))
            // {
            //     if (equippedItem != null && equippedItem.itemData != null)
            //     {
            //         DropItemFromInventory(equippedItem, 1);
            //     }
            // }

            // if (Input.GetAxis("Mouse ScrollWheel") != 0f && inputDelay > 0.1f) // forward
            // {
            //     inputDelay = 0f;
            //     equippedSlotIndex += -Math.Sign(Input.GetAxis("Mouse ScrollWheel"));

            //     if (equippedSlotIndex < 0)
            //     {
            //         equippedSlotIndex = 3;
            //     }
            //     else if (equippedSlotIndex > 3)
            //     {
            //         equippedSlotIndex = 0;
            //     }

            //     UpdateEquippedItem();

            // }

        }
        else if (activated)
        {
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
            {
                CloseInventory();
            }

            if (selectedItem != null && selectedItem.itemData != null)
            {
                // if (Input.GetKeyDown(KeyCode.E) && inputDelay >= 0.1f)
                // {
                //     if (!requireItemType)
                //     {
                //         //EquipItem(selectedItem);
                //     }
                //     else
                //     {
                //         //OnReturnRequiredType?.Invoke(requireItemList[selectedIndex]); 
                //         //CloseInventory();
                //     }
                // }
                // if (Input.GetKeyDown(KeyCode.G) && inputDelay >= 0.1f)
                // {
                //     if (!requireItemType)
                //     {
                //         DropItemFromInventory(selectedItem, 1);
                //     }
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

    // public void RequireItemType(ItemData.ItemType type, Action<InventoryItem> action)
    // {
    //     requireItemType = true;
    //     requireItemList.Clear();
    //     foreach (I_InventoryItem item in inventoryItemList)
    //     {
    //         if (item.itemData.type == type)
    //         {
    //             requireItemList.Add(item);
    //             Instantiate(item.slot.gameObject, UIManager.instance.inventoryTypeGrid.transform);
    //             UIManager.instance.inventoryBackGrid.transform.GetChild(requireItemList.Count-1).GetComponent<InventoryBackSlot>().OnReturnRequiredType += action;
    //         }
    //     }
    //     OnReturnRequiredType += action;

    //     OpenInventory();
    //     UIManager.instance.inventoryItemGrid.SetActive(false);
    //     UIManager.instance.inventoryTypeGrid.SetActive(true);
    //     UIManager.instance.inventoryUI.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().text = type.ToString();
    // }

    public void OpenInventory()
    {
        //playerController.inventoryAudio.PlayInventoryOpen();
        activated = true;
        inputDelay = 0;

        //Time.timeScale = activated ? 0.0f : 1.0f;
        playerController.LockMovement(true);
        playerController.LockCamera(true);

        UIManager.instance.gameplayUI.SetActive(false);
        UIManager.instance.inventoryUI.SetActive(true);
        
        UIManager.instance.detailPanel.SetActive(true);
        UIManager.instance.StoragePanel.SetActive(false);

        UIManager.instance.inventorySlotGrid.SetActive(true);
        UIManager.instance.inventoryTypeGrid.SetActive(false);
        UIManager.instance.inventoryUI.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().text = "Inventory";

        LockCursor(false);

        selectedSlot = inventorySlotList[0];
        UpdateSelection();

        //play the fade in effect
        //UIManager.instance.inventoryAnimation.Play("Basic Fade-in");

        /*for (int i = 0; i < UIManager.instance.inventoryItemGrid.transform.childCount; i++)
        {
            UIManager.instance.inventoryBackGrid.transform.GetChild(i).GetChild(0).gameObject.SetActive(false);
            UIManager.instance.inventoryBackGrid.transform.GetChild(i).GetComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(0.085f, 0.085f, 0.085f, 0.5f);
        }*/
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
        activated = false;
        //Time.timeScale = activated ? 0.0f : 1.0f;

        playerController.LockMovement(false);

        playerController.LockCamera(false);

        UIManager.instance.inventoryUI.SetActive(false);
        UIManager.instance.gameplayUI.SetActive(true);

        LockCursor(true);

        //play the fade out effect
        //UIManager.instance.inventoryAnimation.Play("Basic Fade-out");

        requireItemType = false;
        UIManager.instance.inventorySlotGrid.SetActive(false);
        UIManager.instance.inventoryTypeGrid.SetActive(true);
        OnReturnRequiredType = null;

        /*foreach (I_InventoryItem item in inventoryItemList)
        {
            UIManager.instance.inventoryBackGrid.transform.GetChild(0).GetComponent<InventoryBackSlot>().ClearDelegate();
        }*/
        foreach (Transform child in UIManager.instance.inventoryTypeGrid.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void SelectItem()
    {
        //TODO: check if new item is hovered, play sound

    }

    public I_InventoryItem AddItemToInventory(I_InventoryItem inventoryItem)
    {
        RatingManager.instance.AddScore(50,playerController);

        PocketItemClientRpc(inventoryItem.NetworkObject, playerController.NetworkObject);

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
                        ModifyItemAmountClientRpc(item.NetworkObject, item.itemStatus.amount);
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
            ModifyItemAmountClientRpc(inventoryItem.NetworkObject, inventoryItem.itemStatus.amount);

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
                UnpocketItemClientRpc(inventoryItem.NetworkObject, playerController.NetworkObject);
                return null;
            }
        }

        return null;
        
        // while (temp > 0)
        // {
        //     InventorySlot slot = GetFirstEmptyInventorySlot();

        //     if (slot != null)
        //     {
        //         slot.inventoryItem = inventoryItem;
        //         slot.image.sprite = inventoryItem.itemData.sprite;
        //         slot.name.text = inventoryItem.itemData.name;
        //         slot.amount.text = $"{temp}";
                
        //         inventoryItemList.Add(inventoryItem);
        //         inventoryItem.inventorySlot = slot;
        //         inventoryItem.itemStatus.amount = (temp > inventoryItem.itemData.maxStackAmount) ? inventoryItem.itemData.maxStackAmount : temp;
        //         temp -= inventoryItem.itemStatus.amount;

        //         UpdateIcons();
        //     }
        //     else
        //     {
        //         //DropItem(newItem1, temp);
        //         inventoryItem.itemStatus.amount = temp;
        //         return null;
        //     }
        // }
        // return inventoryItem;
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

        UnpocketItemClientRpc(inventoryItem.NetworkObject, playerController.NetworkObject);
    }

    public void DropItemFromInventory(I_InventoryItem inventoryItem, int amount = 1)
    {
        //playerController.inventoryAudio.PlayItemDrop();

        if (inventoryItem.itemData.isStackable)
        {
            if (inventoryItem.itemStatus.amount > amount)
            {
                inventoryItem.itemStatus.amount -= amount;
                ModifyItemAmountClientRpc(inventoryItem.NetworkObject, inventoryItem.itemStatus.amount);
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
                UnpocketItemClientRpc(inventoryItem.NetworkObject, playerController.NetworkObject);
            }
        }
        else
        {
            if (equippedItem == inventoryItem)
            {
                UnequipItem();
            }

            ClearInventorySlot(inventoryItem.inventorySlot);
            UnpocketItemClientRpc(inventoryItem.NetworkObject, playerController.NetworkObject);
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
                        ModifyItemAmountClientRpc(item.NetworkObject, item.itemStatus.amount);
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
                        UnpocketItemClientRpc(item.NetworkObject, playerController.NetworkObject);

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
                ModifyItemAmountClientRpc(inventoryItem.NetworkObject, inventoryItem.itemStatus.amount);
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

    [Rpc(SendTo.Server)]
    public void InstantiateUnpocketedItemServerRpc(NetworkObjectReference inventoryItem, int amount)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject))
        {
            var gameObject = Instantiate(inventoryItemObject.gameObject);
            gameObject.GetComponent<I_InventoryItem>().itemStatus.amount = amount;
            gameObject.GetComponent<NetworkObject>().Spawn();
            UnpocketItemClientRpc(gameObject.GetComponent<NetworkObject>(), playerController.NetworkObject);
        }
    }

    [Rpc(SendTo.Server)]
    public void InstantiateUnpocketedItemServerRpc(int itemIndex, int amount, NetworkObjectReference playerController)
    {
        if (playerController.TryGet(out NetworkObject playerControllerObject))
        {
            var gameObject = Instantiate(GameSessionManager.Instance.itemList.itemDataList[itemIndex].dropObject, position: playerControllerObject.GetComponent<PlayerController>().headTransform.transform.position + playerControllerObject.GetComponent<PlayerController>().headTransform.transform.forward * 0.5f, playerControllerObject.GetComponent<PlayerController>().headTransform.transform.rotation);
            gameObject.GetComponent<I_InventoryItem>().itemStatus.amount = amount;
            gameObject.GetComponent<NetworkObject>().Spawn();
            InstantiateUnpocketedItemClientRpc(gameObject.GetComponent<NetworkObject>(), playerControllerObject);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void InstantiateUnpocketedItemClientRpc(NetworkObjectReference inventoryItem, NetworkObjectReference playerController)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject) && this.playerController == playerControllerObject.GetComponent<PlayerController>())
        {
            UnpocketItemClientRpc(inventoryItemObject.GetComponent<NetworkObject>(), playerControllerObject);
        }
    }

    [Rpc(SendTo.Server)]
    public void InstantiatePocketedItemServerRpc(int itemIndex, int amount, int storageSlotIndex, ulong clientId)
    {
        var gameObject = Instantiate(GameSessionManager.Instance.itemList.itemDataList[itemIndex].dropObject);
        gameObject.GetComponent<I_InventoryItem>().itemStatus.amount = amount;
        gameObject.GetComponent<NetworkObject>().Spawn();
        InstantiatePocketedItemClientRpc(storageSlotIndex, gameObject.GetComponent<NetworkObject>(), clientId);
    }
    
    [Rpc(SendTo.Everyone)]
    public void InstantiatePocketedItemClientRpc(int storageSlotIndex, NetworkObjectReference inventoryItem, ulong clientId)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && NetworkManager.Singleton.LocalClientId == clientId)
        {
            PocketItemClientRpc(inventoryItemObject.GetComponent<NetworkObject>(), playerController.NetworkObject);
            storageSlotList[storageSlotIndex].inventoryItem = inventoryItemObject.GetComponent<I_InventoryItem>();
            storageSlotList[storageSlotIndex].UpdateInventorySlotDisplay();
            inventoryItemObject.GetComponent<I_InventoryItem>().inventorySlot = storageSlotList[storageSlotIndex];
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

    [Rpc(SendTo.Everyone)]
    public void PocketItemClientRpc(NetworkObjectReference inventoryItem, NetworkObjectReference playerController)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject))
        {
            inventoryItemObject.GetComponent<I_InventoryItem>().owner = playerControllerObject.GetComponent<PlayerController>();
            inventoryItemObject.GetComponent<I_InventoryItem>().EnableItemMeshes(false);
            inventoryItemObject.GetComponent<I_InventoryItem>().EnableItemPhysics(false);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void UnpocketItemClientRpc(NetworkObjectReference inventoryItem)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject))
        {
            inventoryItemObject.GetComponent<I_InventoryItem>().owner = null;
            inventoryItemObject.GetComponent<I_InventoryItem>().inventorySlot = null;
            inventoryItemObject.GetComponent<I_InventoryItem>().EnableItemMeshes(true);
            inventoryItemObject.GetComponent<I_InventoryItem>().EnableItemPhysics(true);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void UnpocketItemClientRpc(NetworkObjectReference inventoryItem, NetworkObjectReference playerController)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject))
        {
            inventoryItemObject.GetComponent<I_InventoryItem>().owner = null;
            inventoryItemObject.GetComponent<I_InventoryItem>().inventorySlot = null;
            inventoryItemObject.GetComponent<I_InventoryItem>().EnableItemMeshes(true);
            inventoryItemObject.GetComponent<I_InventoryItem>().EnableItemPhysics(true);
            inventoryItemObject.transform.position = playerControllerObject.GetComponent<PlayerController>().headTransform.transform.position + playerControllerObject.GetComponent<PlayerController>().headTransform.transform.forward * 0.5f;
            inventoryItemObject.transform.rotation = playerControllerObject.GetComponent<PlayerController>().headTransform.transform.rotation;
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
        
        // if (!inventoryItem.itemData.isEquippable)
        // {
        //     return;
        // }
        
        UnequipItem();

        equippedItem = inventoryItem;
        playerController.currentEquippedItem = inventoryItem;
        playerController.playerAnimationController.armAnimator.SetBool("Equipped", true);
        playerController.playerAnimationController.armAnimator.SetTrigger("SwitchItem");
        //inventoryItem.inventorySlot.rightHandIcon.enabled = true;

        //playerController.inventoryAudio.PlayItemEquip();
        
        EquipItemClientRpc(inventoryItem.NetworkObject, playerController.NetworkObject);

        if (playerController.targetInteractable != null)
        {
            playerController.targetInteractable.Target();
        }
    }

    [Rpc(SendTo.Everyone)]
    public void EquipItemClientRpc(NetworkObjectReference inventoryItem, NetworkObjectReference playerController)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject))
        {
            inventoryItemObject.GetComponent<I_InventoryItem>().OnEquip();
            playerControllerObject.GetComponent<PlayerController>().currentEquippedItem = inventoryItemObject.GetComponent<I_InventoryItem>();
            playerControllerObject.GetComponent<PlayerController>().playerAnimationController.armAnimator.SetBool("Equipped", true);
            playerControllerObject.GetComponent<PlayerController>().playerAnimationController.armAnimator.SetTrigger("SwitchItem");
        }
    }

    public void UnequipItem()//(ItemData.EquipType type)
    {
        //TODO: get unequip sound
        //playerController.inventoryAudio.PlayItemUnequip();

        if (equippedItem != null && equippedItem.inventorySlot != null)
        {
            //equippedItem.inventorySlot.rightHandIcon.enabled = false;
            UnequipItemClientRpc(equippedItem.NetworkObject, playerController.NetworkObject);
        }
        equippedItem = null;
        playerController.currentEquippedItem = null;
        playerController.playerAnimationController.armAnimator.SetBool("Equipped", false);
        playerController.playerAnimationController.armAnimator.SetTrigger("SwitchItem");
    }

    [Rpc(SendTo.Everyone)]
    public void UnequipItemClientRpc(NetworkObjectReference inventoryItem, NetworkObjectReference playerController)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject))
        {
            inventoryItemObject.GetComponent<I_InventoryItem>().OnUnequip();
            playerControllerObject.GetComponent<PlayerController>().currentEquippedItem = null;
            playerControllerObject.GetComponent<PlayerController>().playerAnimationController.armAnimator.SetBool("Equipped", false);
            playerControllerObject.GetComponent<PlayerController>().playerAnimationController.armAnimator.SetTrigger("SwitchItem");
        }
    }

    [Rpc(SendTo.Everyone)]
    public void ModifyItemAmountClientRpc(NetworkObjectReference inventoryItem, int amount)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject))
        {
            inventoryItemObject.GetComponent<I_InventoryItem>().itemStatus.amount = amount;
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
