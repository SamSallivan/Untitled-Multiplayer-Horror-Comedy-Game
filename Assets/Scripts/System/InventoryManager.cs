using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using System;
using Unity;
using Unity.Netcode;
using Unity.Netcode.Components;
using Steamworks;
using Steamworks.Data;
using UnityEngine.UIElements;

public class InventoryManager : NetworkBehaviour//MonoBehaviour
{
    public static InventoryManager instance;
    public bool activated;

    public InventorySlot slotPrefab;

    public List<I_InventoryItem> inventoryItemList;
    public List<InventorySlot> inventorySlotList;

    public int hoveredIndex;
    public int selectedIndex;
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

        foreach(Transform child in UIManager.instance.inventoryItemGrid.transform){
            inventorySlotList.Add(child.GetComponent<InventorySlot>());
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerController)
        {
            if(GameSessionManager.Instance.localPlayerController)
                playerController = GameSessionManager.Instance.localPlayerController;
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

            if (Input.GetKeyDown(KeyCode.G))
            {
                if (equippedItem != null && equippedItem.itemData != null)
                {
                    DropItemFromInventory(equippedItem);
                }
            }

            if (Input.GetAxis("Mouse ScrollWheel") != 0f && inputDelay > 0.1f) // forward
            {
                inputDelay = 0f;
                equippedSlotIndex += -Math.Sign(Input.GetAxis("Mouse ScrollWheel"));

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

            /*if (Input.GetKeyDown(KeyCode.M))
            {
                InventoryItem map = FindInventoryItem("Map");
                InventoryItem compass = FindInventoryItem("Compass");
                if (map != null)
                {
                    if (equippedItemLeft != null && equippedItemLeft == map)
                    {
                        UnequipItem(map);
                        if (compass != null)
                        {
                            UnequipItem(compass);
                        }
                    }
                    else
                    {
                        EquipItem(map, false);
                        if (compass != null)
                        {
                            EquipItem(compass, false);
                        }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                InventoryItem flashlight = FindInventoryItem("Flashlight");
                if (flashlight != null)
                {
                    if (equippedItemLeft != null && equippedItemLeft == flashlight)
                    {
                        UnequipItem(flashlight);
                    }
                    else
                    {
                        EquipItem(flashlight, false);
                    }
                }
            }*/
        }
        else if (activated)
        {
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
            {
                CloseInventory();
            }

            if (selectedItem != null && selectedItem.itemData != null)
            {
                if (Input.GetKeyDown(KeyCode.E) && inputDelay >= 0.1f)
                {
                    if (!requireItemType)
                    {
                        //EquipItem(selectedItem);
                    }
                    else
                    {
                        OnReturnRequiredType?.Invoke(requireItemList[hoveredIndex]); 
                        CloseInventory();
                    }
                }
                if (Input.GetKeyDown(KeyCode.G) && inputDelay >= 0.1f)
                {
                    if (!requireItemType)
                    {
                        DropItemFromInventory(selectedItem, 1);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.W))
            {
                selectedPosition.x += Input.GetKeyDown(KeyCode.D) ? 1 : 0;
                selectedPosition.x -= Input.GetKeyDown(KeyCode.A) ? 1 : 0;

                selectedPosition.y += Input.GetKeyDown(KeyCode.S) ? 1 : 0;
                selectedPosition.y -= Input.GetKeyDown(KeyCode.W) ? 1 : 0;

                if (selectedPosition.x < 0)
                {
                    selectedPosition.x = slotPerRow - 1;
                }
                if (selectedPosition.x > slotPerRow - 1)
                {
                    selectedPosition.x = 0;
                }
                if (selectedPosition.y < 0)
                {
                    selectedPosition.y = slotPerColumn - 1;
                }
                if (selectedPosition.y > slotPerColumn - 1)
                {
                    selectedPosition.y = 0;
                }

                //selectedIndex = GetGridIndex(selectedPosition);
                hoveredIndex = GetGridIndex(selectedPosition);
                UpdateSelection();

                /*for (int i = 0; i < UIManager.instance.inventoryBackGrid.transform.childCount; i++)
                {
                    UIManager.instance.inventoryBackGrid.transform.GetChild(i).GetComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(0.085f, 0.085f, 0.085f, 0.5f);
                }
                UIManager.instance.inventoryBackGrid.transform.GetChild(selectedIndex).GetComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(0.85f, 0.85f, 0.85f, 0.5f);*/
                foreach (InventorySlot slot in inventorySlotList)
                {
                    slot.background.color = new UnityEngine.Color(0.085f, 0.085f, 0.085f, 0.5f);
                }
                inventorySlotList[hoveredIndex].background.color = new UnityEngine.Color(0.85f, 0.85f, 0.85f, 0.5f);

            }

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
        EquipItem(inventorySlotList[equippedSlotIndex].inventoryItem);
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

        UIManager.instance.inventoryUI.SetActive(true);
        UIManager.instance.gameplayUI.SetActive(false);
        UIManager.instance.inventoryItemGrid.SetActive(true);
        UIManager.instance.inventoryTypeGrid.SetActive(false);
        UIManager.instance.inventoryUI.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().text = "Inventory";

        LockCursor(false);

        selectedPosition = 0;
        hoveredIndex = GetGridIndex(selectedPosition);
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
            slot.background.color = new UnityEngine.Color(0.085f, 0.085f, 0.085f, 0.5f);
        }

        inventorySlotList[hoveredIndex].background.color = new UnityEngine.Color(0.85f, 0.85f, 0.85f, 0.5f);
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
        UIManager.instance.inventoryItemGrid.SetActive(false);
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

    [ServerRpc(RequireOwnership = false)]
    public void PocketItemServerRpc(NetworkObjectReference inventoryItem, NetworkObjectReference playerController)
    {   
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject))
        {
            PocketItemClientRpc(inventoryItemObject, playerControllerObject);
        }
    }
    [ClientRpc]
    public void PocketItemClientRpc(NetworkObjectReference inventoryItem, NetworkObjectReference playerController)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject))
        {
            inventoryItemObject.GetComponent<I_InventoryItem>().owner = playerControllerObject.GetComponent<PlayerController>();
            inventoryItemObject.GetComponent<I_InventoryItem>().EnableItemMeshes(false);
            inventoryItemObject.GetComponent<I_InventoryItem>().EnableItemPhysics(false);
        }
    }

    public I_InventoryItem AddItemToInventory(I_InventoryItem inventoryItem)
    {
        if (IsHost)
        {
            PocketItemClientRpc(inventoryItem.NetworkObject, playerController.NetworkObject);
        }
        else
        {
            PocketItemServerRpc(inventoryItem.NetworkObject, playerController.NetworkObject);
        }

        int temp = inventoryItem.itemStatus.amount;

        foreach (I_InventoryItem item in inventoryItemList)
        {
            if (item.itemData == inventoryItem.itemData && item.itemData.isStackable)
            {
                while (item.itemStatus.amount < item.itemData.maxStackAmount && temp > 0)
                {
                    item.itemStatus.amount++;
                    temp--;
                    item.inventorySlot.amount.text = "" + item.itemStatus.amount;
                }

                if (temp <= 0)
                {
                    return item;
                }
            }
        }

        if (temp > 0)
        {
            inventoryItem.itemStatus.amount = temp;

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
                inventoryItemList.Add(inventoryItem);
                inventoryItem.inventorySlot = slot;

                slot.inventoryItem = inventoryItem;
                slot.UpdateInventorySlotDisplay();

                UpdateIcons();

                if (slot.GetIndex() == equippedSlotIndex)
                {
                    UpdateEquippedItem();
                }

                return inventoryItem;
            }
            else
            {
                if (IsHost)
                {
                    UnpocketItemClientRpc(inventoryItem.NetworkObject, playerController.NetworkObject);
                }
                else
                {
                    UnpocketItemServerRpc(inventoryItem.NetworkObject, playerController.NetworkObject);
                }
                //DropItemFromInventory(inventoryItem, temp);
                inventoryItem.itemStatus.amount = temp;
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

    public void RemoveItem(I_InventoryItem inventoryItem)
    {
        if (inventoryItem.itemData.isStackable)
        {
            if (inventoryItem.itemStatus.amount > 1)
            {
                inventoryItem.itemStatus.amount--;
                inventoryItem.inventorySlot.amount.text = "" + inventoryItem.itemStatus.amount;

            }
            else
            {
                inventoryItemList.Remove(inventoryItem);
                Destroy(inventoryItem.inventorySlot.gameObject);
                if (equippedItem == inventoryItem)
                {
                    UnequipItem();
                }
            }
        }
        else
        {
            inventoryItemList.Remove(inventoryItem);
            Destroy(inventoryItem.inventorySlot.gameObject);
            if (equippedItem == inventoryItem)
            {
                UnequipItem();
            }
        }

        UpdateSelection();
        UpdateIcons();

        //loop through items and organize inventory.
        //perhaps a separate functions for this?
    }

    [ServerRpc(RequireOwnership = false)]
    public void InstantiateItemServerRpc(NetworkObjectReference inventoryItem, NetworkObjectReference playerController, int amount)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject))
        {
            var gameObject = Instantiate(inventoryItemObject.gameObject);
            gameObject.GetComponent<I_InventoryItem>().itemStatus.amount = amount;
            gameObject.GetComponent<NetworkObject>().Spawn();
            UnpocketItemServerRpc(gameObject.GetComponent<NetworkObject>(), playerControllerObject);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UnpocketItemServerRpc(NetworkObjectReference inventoryItem, NetworkObjectReference playerController)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject))
        {
            UnpocketItemClientRpc(inventoryItemObject, playerControllerObject);
        }
    }

    [ClientRpc]
    public void UnpocketItemClientRpc(NetworkObjectReference inventoryItem, NetworkObjectReference playerController)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject) && playerController.TryGet(out NetworkObject playerControllerObject))
        {
            inventoryItemObject.GetComponent<I_InventoryItem>().owner = null;
            inventoryItemObject.GetComponent<I_InventoryItem>().EnableItemMeshes(true);
            inventoryItemObject.GetComponent<I_InventoryItem>().EnableItemPhysics(true);
            inventoryItemObject.transform.position = playerControllerObject.GetComponent<PlayerController>().tHead.transform.position + playerControllerObject.GetComponent<PlayerController>().tHead.transform.forward * 0.5f;
            inventoryItemObject.transform.rotation = playerControllerObject.GetComponent<PlayerController>().tHead.transform.rotation;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void HideItemServerRpc(NetworkObjectReference inventoryItem, bool hide)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject))
        {
            HideItemClientRpc(inventoryItemObject, hide);
        }
    }

    [ClientRpc]
    public void HideItemClientRpc(NetworkObjectReference inventoryItem, bool hide)
    {
        if (inventoryItem.TryGet(out NetworkObject inventoryItemObject))
        {
            inventoryItemObject.GetComponent<I_InventoryItem>().EnableItemMeshes(!hide);
        }
    }

    public void ClearInventorySlot(InventorySlot slot)
    {
        slot.inventoryItem.inventorySlot = null;
        slot.inventoryItem = null;
        slot.image.sprite = null;
        slot.image.color = new UnityEngine.Color(1, 1, 1, 0);
        slot.name.text = "";
        slot.amount.text = "";
    }

    public void DropItemFromInventory(I_InventoryItem inventoryItem)
    {
        if (equippedItem == inventoryItem)
        {
            UnequipItem();
        }

        inventoryItemList.Remove(inventoryItem);
        ClearInventorySlot(inventoryItem.inventorySlot);

        UnpocketItemServerRpc(inventoryItem.NetworkObject, playerController.NetworkObject);
    }

    public void DropItemFromInventory(I_InventoryItem inventoryItem, int amount = 1)
    {
        //playerController.inventoryAudio.PlayItemDrop();

        if (inventoryItem.itemData.isStackable)
        {
            if (inventoryItem.itemStatus.amount > amount)
            {
                inventoryItem.itemStatus.amount -= amount;
                inventoryItem.inventorySlot.amount.text = "" + inventoryItem.itemStatus.amount;
                InstantiateItemServerRpc(inventoryItem.NetworkObject, playerController.NetworkObject, amount);

            }
            else
            {
                if (equippedItem == inventoryItem)
                {
                    UnequipItem();
                }

                inventoryItemList.Remove(inventoryItem);
                ClearInventorySlot(inventoryItem.inventorySlot);
                UnpocketItemServerRpc(inventoryItem.NetworkObject, playerController.NetworkObject);
            }
        }
        else
        {
            if (equippedItem == inventoryItem)
            {
                UnequipItem();
            }

            inventoryItemList.Remove(inventoryItem);
            ClearInventorySlot(inventoryItem.inventorySlot);
            UnpocketItemServerRpc(inventoryItem.NetworkObject, playerController.NetworkObject);
            //droppedObject.GetComponentInChildren<I_InventoryItem>().itemStatus.durability = inventoryItem.status.durability;
        }

        UpdateSelection();
        UpdateIcons();

        //loop through items and organize inventory.
        //perhaps a separate functions for this?
    }

    public void DropItemFromInventory(ItemData itemData, int amount)
    {
        //materialCount -= x;

        int temp = amount;

        foreach (I_InventoryItem inventoryItem in inventoryItemList)
        {
            if (inventoryItem.itemData == itemData && inventoryItem.itemData.isStackable)
            {
                while (inventoryItem.itemStatus.amount > 0 && temp > 0)
                {
                    inventoryItem.itemStatus.amount--;
                    temp--;
                    inventoryItem.inventorySlot.amount.text = "" + inventoryItem.itemStatus.amount;

                    //remove from inventory if <= 0
                }

                if (temp <= 0)
                {
                    return;
                }

                if (inventoryItem.itemStatus.amount <= 0)
                {
                    if (equippedItem == inventoryItem)
                    {
                        UnequipItem();
                    }

                    inventoryItemList.Remove(inventoryItem);
                    ClearInventorySlot(inventoryItem.inventorySlot);
                    UnpocketItemServerRpc(inventoryItem.NetworkObject, playerController.NetworkObject);

                }
                break;
            }
        }
    }

    public void EquipItem(I_InventoryItem inventoryItem, bool autoUnequip = false)
    {
        if (inventoryItem == null)
        {
            UnequipItem();
            return;
        }
        if (!inventoryItem.itemData.isEquippable)
        {
            return;
        }

        if (equippedItem == inventoryItem)
        {
            if (autoUnequip)
            {
                UnequipItem();
            }
        }
        else
        {
            UnequipItem();

            equippedItem = inventoryItem;
            inventoryItem.inventorySlot.rightHandIcon.enabled = true;

            //playerController.inventoryAudio.PlayItemEquip();


            if (IsHost)
            {
                HideItemClientRpc(inventoryItem.NetworkObject, false);
            }
            else
            {
                HideItemServerRpc(inventoryItem.NetworkObject, false);
            }

        }

        if (playerController.targetInteractable != null)
        {
            playerController.targetInteractable.Target();
        }
    }

    public void UnequipItem()//(ItemData.EquipType type)
    {
        //TODO: get unequip sound
        //playerController.inventoryAudio.PlayItemUnequip();
        
        if (equippedItem != null && equippedItem.inventorySlot != null)
        {
            equippedItem.inventorySlot.rightHandIcon.enabled = false;
            /*if (playerController.equippedTransformRight.childCount > 0)
            {
                Destroy(playerController.equippedTransformRight.GetChild(0).gameObject);
            }*/
            if (IsHost)
            {
                HideItemClientRpc(equippedItem.NetworkObject, true);
            }
            else
            {
                HideItemServerRpc(equippedItem.NetworkObject, true);
            }
        }
        equippedItem = null;
    }

    public I_InventoryItem FindInventoryItem(string name)
    {
        foreach (I_InventoryItem item in inventoryItemList)
        {
            if (item.itemData.title == name)
            {
                return item;
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
        inventorySlotList[GetGridIndex(selectedPosition)].GetComponent<InventorySlot>().background.color = new UnityEngine.Color(0.85f, 0.85f, 0.85f, 0.5f);

        if (!requireItemType)
        {
            if (inventorySlotList[hoveredIndex].inventoryItem != null)
            {
                selectedItem = inventorySlotList[hoveredIndex].inventoryItem;
            }
            else
            {
                selectedItem = null;
            }
        }
        else if (requireItemType)
        {

            if (inventorySlotList[hoveredIndex].inventoryItem != null)
            {
                selectedItem = inventorySlotList[hoveredIndex].inventoryItem;
            }
            else
            {
                selectedItem = null;
            }
        }

        if (selectedItem != null && selectedItem.itemData != null)
        {
            CreateObjectDetail();
        }
        else if(deleteOnUnselect)
        {
            DeleteObjectDetail();
        }
    }

    public void CreateObjectDetail()
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

    public void DeleteObjectDetail()
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
            UIManager.instance.detailObjectPivot.GetChild(0).transform.Rotate(playerController.tHead.GetChild(0).transform.up, rotateValue.x, Space.World);
            UIManager.instance.detailObjectPivot.GetChild(0).transform.Rotate(playerController.tHead.GetChild(0).transform.right, rotateValue.y, Space.World);
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
                UIManager.instance.detailObjectPivot.GetChild(0).transform.Rotate(playerController.tHead.GetChild(0).transform.up, 0.2f, Space.World);
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

/*    public void EquipMap()
    {
        I_InventoryItem map = FindInventoryItem("Map");
        I_InventoryItem compass = FindInventoryItem("Compass");
        if (map != null)
        {
            if (equippedItemLeft != null && equippedItemLeft == map)
            {
                UnequipItem(map);
                if (compass != null)
                {
                    UnequipItem(compass);
                }
            }
            else
            {
                EquipItem(map, false);
                if (compass != null)
                {
                    EquipItem(compass, false);
                }
            }
        }
    }*/

    public void UpdateIcons()
    {
        // InventoryItem map = FindInventoryItem("Map");
        // if (map != null)
        // {
        //     UIManager.instance.mapIcon.SetActive(true);
        // }
        // else
        // {
        //     UIManager.instance.mapIcon.SetActive(false);
        // }

        // InventoryItem flashlight = FindInventoryItem("Flashlight");
        // if (flashlight != null)
        // {
        //     UIManager.instance.flashlightIcon.SetActive(true);
        // }
        // else
        // {
        //     UIManager.instance.flashlightIcon.SetActive(false);
        // }
    }
    public void LockCursor(bool lockCursor)
    {
        if (lockCursor)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
        else
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Confined;
            UnityEngine.Cursor.visible = true;
        }
    }
}
