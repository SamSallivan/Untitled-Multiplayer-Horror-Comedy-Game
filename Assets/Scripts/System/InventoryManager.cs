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

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;
    public bool activated;

    public InventorySlot slotPrefab;

    public List<InventoryItem> inventoryItemList;
    public int selectedIndex;
    public int2 selectedPosition;
    public InventoryItem selectedItem;
    public InventoryItem equippedItemLeft = null;
    public InventoryItem equippedItemRight = null;
    public InventoryItem equippedItemCenter = null;

    public int slotPerRow = 8;
    public int slotPerColumn = 4;

    private InventoryItem detailObject;
    private bool detailRotationFix;
    public bool detailObjectDrag;
    public float inputDelay;

    public static event Action<ItemData> OnPickUp = delegate{};
    public static event Action<InventoryItem> OnReturnRequiredType = delegate { };

    public bool requireItemType;
    public List<InventoryItem> requireItemList;

    public PlayerController playerController;


    void Awake()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (!playerController)
        {
            playerController = GameSessionManager.Instance.localPlayerController;
        }
        if (inputDelay < 1)
        {
            inputDelay += Time.fixedDeltaTime;
        }
        
        if (!activated && GameSessionManager.Instance.localPlayerController.enableMovement)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                OpenInventory();
            }
/*
            if (Input.GetKeyDown(KeyCode.M))
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

            if (Input.GetKeyDown(KeyCode.G))
            {
                if (equippedItemCenter != null && equippedItemCenter.data != null)
                {
                    DropItem(equippedItemCenter);
                }
                if (equippedItemLeft != null && equippedItemLeft.data != null)
                {
                    DropItem(equippedItemLeft);
                }
                if (equippedItemRight != null && equippedItemRight.data != null)
                {
                    DropItem(equippedItemRight);
                }
            }
        }
        else if (activated)
        {
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
            {
                CloseInventory();
            }

            if (selectedItem != null && selectedItem.data != null)
            {
                if (Input.GetKeyDown(KeyCode.E) && inputDelay >= 0.1f)
                {
                    if (!requireItemType)
                    {
                        EquipItem(selectedItem);
                    }
                    else
                    {
                        OnReturnRequiredType?.Invoke(requireItemList[selectedIndex]); 
                        CloseInventory();
                    }
                }
                if (Input.GetKeyDown(KeyCode.G) && inputDelay >= 0.1f)
                {
                    if (!requireItemType)
                    {
                        DropItem(selectedItem);
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

                selectedIndex = GetGridIndex(selectedPosition);
                UpdateSelection();

                for (int i = 0; i < UIManager.instance.inventoryBackGrid.transform.childCount; i++)
                {
                    UIManager.instance.inventoryBackGrid.transform.GetChild(i).GetComponent<Image>().color = new Color(0.085f, 0.085f, 0.085f, 0.5f);
                }
                UIManager.instance.inventoryBackGrid.transform.GetChild(selectedIndex).GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.85f, 0.5f);

            }

            if (detailObject != null)
            {
                RotateDetailObject();
            }
        }
    }

    public void RequireItemType(ItemData.ItemType type, Action<InventoryItem> action)
    {
        requireItemType = true;
        requireItemList.Clear();
        foreach (InventoryItem item in inventoryItemList)
        {
            if (item.data.type == type)
            {
                requireItemList.Add(item);
                Instantiate(item.slot.gameObject, UIManager.instance.inventoryTypeGrid.transform);
                UIManager.instance.inventoryBackGrid.transform.GetChild(requireItemList.Count-1).GetComponent<InventoryBackSlot>().OnReturnRequiredType += action;
            }
        }
        OnReturnRequiredType += action;

        OpenInventory();
        UIManager.instance.inventoryItemGrid.SetActive(false);
        UIManager.instance.inventoryTypeGrid.SetActive(true);
        UIManager.instance.inventoryUI.transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().text = type.ToString();

    }

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
        selectedIndex = GetGridIndex(selectedPosition);
        UpdateSelection();

        //play the fade in effect
        //UIManager.instance.inventoryAnimation.Play("Basic Fade-in");

        for (int i = 0; i < UIManager.instance.inventoryBackGrid.transform.childCount; i++)
        {
            UIManager.instance.inventoryBackGrid.transform.GetChild(i).GetChild(0).gameObject.SetActive(false);
            UIManager.instance.inventoryBackGrid.transform.GetChild(i).GetComponent<Image>().color = new Color(0.085f, 0.085f, 0.085f, 0.5f);
        }

        UIManager.instance.inventoryBackGrid.transform.GetChild(selectedIndex).GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.85f, 0.5f);
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

        foreach (InventoryItem item in inventoryItemList)
        {
            UIManager.instance.inventoryBackGrid.transform.GetChild(0).GetComponent<InventoryBackSlot>().ClearDelegate();
        }
        foreach (Transform child in UIManager.instance.inventoryTypeGrid.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void SelectItem()
    {
        //TODO: check if new item is hovered, play sound

    }

    public InventoryItem AddItem(ItemData itemData, ItemStatus itemStatus)
    {
        
        OnPickUp?.Invoke(itemData);

        //playerController.inventoryAudio.PlayItemCollect();

        int temp = itemStatus.amount;

        foreach (InventoryItem item in inventoryItemList)
        {
            if (item.data == itemData && item.data.isStackable)
            {
                while (item.status.amount < item.data.maxStackAmount && temp > 0)
                {
                    item.status.amount++;
                    temp--;
                    item.slot.amount.text = "" + item.status.amount;
                }

                if (temp <= 0)
                {
                    return item;
                }
            }
        }

        if (temp > 0)
        {
            InventorySlot newSlot1 = Instantiate(slotPrefab, UIManager.instance.inventoryItemGrid.transform);
            InventoryItem newItem1 = new InventoryItem(itemData, new ItemStatus(temp, 1), newSlot1);
            inventoryItemList.Add(newItem1);
            newSlot1.inventoryItem = newItem1;
            newSlot1.image.sprite = itemData.sprite;
            newSlot1.name.text = itemData.name;
            newSlot1.amount.text = $"{temp}";

            if (inventoryItemList.Count > slotPerRow * slotPerColumn)
            {
                DropItem(newItem1, itemStatus.amount);
                return null;
            }

            UpdateIcons();
            return newItem1;
        }
        return null;
        

        /*InventorySlot newSlot = Instantiate(slotPrefab, UIManager.instance.inventoryItemGrid.transform);
        InventoryItem newItem = new InventoryItem(itemData, itemStatus, newSlot);
        inventoryItemList.Add(newItem);
        newSlot.inventoryItem = newItem;
        newSlot.image.sprite = itemData.sprite;
        newSlot.name.text = itemData.name;
        newSlot.amount.text = $"{itemStatus.amount}";

        if (inventoryItemList.Count > slotPerRow * slotPerColumn)
        {
            DropItem(newItem);
            return null;
        }
        return newItem;*/

    }

    public void RemoveItem(InventoryItem inventoryItem)
    {
        if (inventoryItem.data.isStackable)
        {
            if (inventoryItem.status.amount > 1)
            {
                inventoryItem.status.amount--;
                inventoryItem.slot.amount.text = "" + inventoryItem.status.amount;

            }
            else
            {
                inventoryItemList.Remove(inventoryItem);
                Destroy(inventoryItem.slot.gameObject);
                if (equippedItemLeft == inventoryItem || equippedItemRight == inventoryItem || equippedItemCenter == inventoryItem)
                {
                    UnequipItem(inventoryItem.data.equipType);
                }
            }
        }
        else
        {
            inventoryItemList.Remove(inventoryItem);
            Destroy(inventoryItem.slot.gameObject);
            if (equippedItemLeft == inventoryItem || equippedItemRight == inventoryItem || equippedItemCenter == inventoryItem)
            {
                UnequipItem(inventoryItem.data.equipType);
            }
        }

        UpdateSelection();
        UpdateIcons();

        //loop through items and organize inventory.
        //perhaps a separate functions for this?
    }

    public void DropItem(InventoryItem inventoryItem, int amount = 1)
    {
        //playerController.inventoryAudio.PlayItemDrop();

        if (inventoryItem.data.isStackable)
        {
            if (inventoryItem.status.amount > amount)
            {
                inventoryItem.status.amount -= amount;
                inventoryItem.slot.amount.text = "" + inventoryItem.status.amount;
                GameObject droppdeObject = Instantiate(inventoryItem.data.dropObject, playerController.tHead.transform.position + playerController.tHead.transform.forward * 0.5f, playerController.tHead.transform.rotation);
                droppdeObject.GetComponent<I_InventoryItem>().itemStatus.amount = amount;
                droppdeObject.GetComponent<NetworkObject>().Spawn();

            }
            else
            {
                inventoryItemList.Remove(inventoryItem);
                Destroy(inventoryItem.slot.gameObject);
                GameObject droppdeObject = Instantiate(inventoryItem.data.dropObject, playerController.tHead.transform.position + playerController.tHead.transform.forward * 0.5f, playerController.tHead.transform.rotation);
                droppdeObject.GetComponent<I_InventoryItem>().itemStatus.amount = amount;
                droppdeObject.GetComponent<NetworkObject>().Spawn();
                if (equippedItemLeft == inventoryItem || equippedItemRight == inventoryItem || equippedItemCenter == inventoryItem)
                {
                    UnequipItem(inventoryItem.data.equipType);
                }
            }
        }
        else
        {
            inventoryItemList.Remove(inventoryItem);
            Destroy(inventoryItem.slot.gameObject);
            GameObject droppdeObject = Instantiate(inventoryItem.data.dropObject, playerController.tHead.transform.position + playerController.tHead.transform.forward * 0.5f, playerController.tHead.transform.rotation);
            droppdeObject.GetComponentInChildren<I_InventoryItem>().itemStatus.durability = inventoryItem.status.durability;
            droppdeObject.GetComponent<I_InventoryItem>().itemStatus.amount = amount;
            droppdeObject.GetComponent<NetworkObject>().Spawn();
            if (equippedItemLeft == inventoryItem || equippedItemRight == inventoryItem || equippedItemCenter == inventoryItem)
            {
                UnequipItem(inventoryItem.data.equipType);
            }
        }

        UpdateSelection();
        UpdateIcons();

        //loop through items and organize inventory.
        //perhaps a separate functions for this?
    }

    public void DropItem(ItemData itemData, int amount)
    {
        //materialCount -= x;

        int temp = amount;

        foreach (InventoryItem item in inventoryItemList)
        {
            if (item.data == itemData && item.data.isStackable)
            {
                while (item.status.amount > 0 && temp > 0)
                {
                    item.status.amount--;
                    temp--;
                    item.slot.amount.text = "" + item.status.amount;

                    //remove from inventory if <= 0
                }

                if (temp <= 0)
                {
                    return;
                }

                if (item.status.amount <= 0)
                {
                    inventoryItemList.Remove(item);
                    Destroy(item.slot.gameObject);
                    if (equippedItemLeft == item || equippedItemRight == item || equippedItemCenter == item)
                    {
                        UnequipItem(item.data.equipType);
                    }
                }
                break;
            }
        }
    }

    public void EquipItem(InventoryItem item, bool autoUnequip = true)
    {
        if (!item.data.isEquippable)
        {
            return;
        }

        if (equippedItemLeft == item || equippedItemRight == item || equippedItemCenter == item)
        {
            if (autoUnequip)
            {
                UnequipItem(item);
            }
        }
        else
        {
            UnequipItem(item.data.equipType);
            Transform equipPivot;

            switch (item.data.equipType)
            {
                case ItemData.EquipType.Left:
                    equippedItemLeft = item;
                    equippedItemLeft.slot.leftHandIcon.enabled = true;
                    equipPivot = playerController.equippedTransformLeft;
                    break;

                case ItemData.EquipType.Right:
                    equippedItemRight = item;
                    equippedItemRight.slot.rightHandIcon.enabled = true;
                    equipPivot = playerController.equippedTransformRight;
                    break;

                case ItemData.EquipType.Both:
                    equippedItemCenter = item;
                    equippedItemCenter.slot.leftHandIcon.enabled = true;
                    equippedItemCenter.slot.rightHandIcon.enabled = true;
                    equipPivot = playerController.equippedTransformCenter;
                    break;

                default:
                    equipPivot = playerController.equippedTransformRight;
                    break;
            }
            //playerController.inventoryAudio.PlayItemEquip();

            GameObject newObject = Instantiate(item.data.dropObject, equipPivot);
            newObject.name = item.data.dropObject.name + " Equipped";
            //newObject.transform.localPosition = new Vector3(0, 0, 0);
            newObject.transform.localPosition = item.data.equipPosition;
            //newObject.transform.localRotation = item.data.equipRotation;

            //Destroy(newObject.transform.GetChild(0).gameObject);
            //newObject.transform.GetComponentInChildren<Interactable>().enabled = false;
            foreach (Collider collider in newObject.GetComponents<Collider>())
            {
                Destroy(collider);
            }
            //Destroy(newObject.GetComponent<Rigidbody>());
            newObject.GetComponent<Rigidbody>().isKinematic = true;
        }

        if (playerController.targetInteractable != null)
        {
            playerController.targetInteractable.Target();
        }
    }

    public void UnequipItem(InventoryItem item)
    {
        if (equippedItemLeft == item)
        {
            UnequipItem(ItemData.EquipType.Left);
        }
        else if (equippedItemRight == item)
        {
            UnequipItem(ItemData.EquipType.Right);
        }
        else if (equippedItemCenter == item)
        {
            UnequipItem(ItemData.EquipType.Both);
        }
    }

    public void UnequipItem(ItemData.EquipType type)
    {
        //TODO: get unequip sound
        //playerController.inventoryAudio.PlayItemUnequip();

        switch (type)
        {
            case ItemData.EquipType.Left:

                if (equippedItemLeft != null && equippedItemLeft.slot != null)
                {
                    equippedItemLeft.slot.leftHandIcon.enabled = false;
                    if (playerController.equippedTransformLeft.childCount > 0)
                    {
                        Destroy(playerController.equippedTransformLeft.GetChild(0).gameObject);
                    }
                }
                equippedItemLeft = null;

                if (equippedItemCenter != null && equippedItemCenter.slot != null)
                {
                    equippedItemCenter.slot.leftHandIcon.enabled = false;
                    equippedItemCenter.slot.rightHandIcon.enabled = false;
                    Debug.Log("1");
                    if (playerController.equippedTransformCenter.childCount > 0)
                    {
                        Debug.Log("2");
                        Destroy(playerController.equippedTransformCenter.GetChild(0).gameObject);
                    }
                }
                equippedItemCenter = null;
                break;

            case ItemData.EquipType.Right:

                if (equippedItemRight != null && equippedItemRight.slot != null)
                {
                    equippedItemRight.slot.rightHandIcon.enabled = false;
                    if (playerController.equippedTransformRight.childCount > 0)
                    {
                        Destroy(playerController.equippedTransformRight.GetChild(0).gameObject);
                    }
                }
                equippedItemRight = null;

                if (equippedItemCenter != null && equippedItemCenter.slot != null)
                {
                    equippedItemCenter.slot.leftHandIcon.enabled = false;
                    equippedItemCenter.slot.rightHandIcon.enabled = false;
                    if (playerController.equippedTransformCenter.childCount > 0)
                    {
                        Destroy(playerController.equippedTransformCenter.GetChild(0).gameObject);
                    }
                }
                equippedItemCenter = null;
                break;

            case ItemData.EquipType.Both:

                if (equippedItemLeft != null && equippedItemLeft.slot != null)
                {
                    equippedItemLeft.slot.leftHandIcon.enabled = false;
                    if (playerController.equippedTransformLeft.childCount > 0)
                    {
                        Destroy(playerController.equippedTransformLeft.GetChild(0).gameObject);
                    }
                }
                equippedItemLeft = null;

                if (equippedItemRight != null && equippedItemRight.slot != null)
                {
                    equippedItemRight.slot.rightHandIcon.enabled = false;
                    if (playerController.equippedTransformRight.childCount > 0)
                    {
                        Destroy(playerController.equippedTransformRight.GetChild(0).gameObject);
                    }
                }
                equippedItemRight = null;

                if (equippedItemCenter != null && equippedItemCenter.slot != null)
                {
                    equippedItemCenter.slot.leftHandIcon.enabled = false;
                    equippedItemCenter.slot.rightHandIcon.enabled = false;
                    if (playerController.equippedTransformCenter.childCount > 0)
                    {
                        Destroy(playerController.equippedTransformCenter.GetChild(0).gameObject);
                    }
                }
                equippedItemCenter = null;
                break;
        }
    }

    public InventoryItem FindInventoryItem(string name)
    {
        foreach (InventoryItem item in inventoryItemList)
        {
            if (item.data.title == name)
            {
                return item;
            }
        }

        return null;
    }

    public void UpdateSelection(bool deleteOnUnselect = true)
    {
        if (!requireItemType)
        {
            if (inventoryItemList.Count - 1 >= selectedIndex)
            {
                selectedItem = inventoryItemList[selectedIndex];
            }
            else
            {
                selectedItem = null;
            }
        }
        else if (requireItemType)
        {

            if (requireItemList.Count - 1 >= selectedIndex)
            {
                selectedItem = requireItemList[selectedIndex];
            }
            else
            {
                selectedItem = null;
            }
        }

        if (selectedItem != null && selectedItem.data != null)
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
        if (UIManager.instance.detailName.text != selectedItem.data.name)
        {
            detailObject = selectedItem;
            UIManager.instance.detailName.text = selectedItem.data.title;
            UIManager.instance.detailDescription.text = selectedItem.data.description;

            /*while (UIManager.instance.detailObjectPivot.childCount > 0)
            {
                Destroy(UIManager.instance.detailObjectPivot.GetChild(0).gameObject);
            }*/

            foreach (Transform child in UIManager.instance.detailObjectPivot)
            {
                Destroy(child.gameObject);
            }

            GameObject detailGameObject = Instantiate(selectedItem.data.dropObject, UIManager.instance.detailObjectPivot);
            detailGameObject.transform.localScale *= 1200;
            detailGameObject.transform.localScale *= selectedItem.data.examineScale;
            detailGameObject.transform.localRotation = selectedItem.data.examineRotation;
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

            if (Quaternion.Angle(currentRotation, detailObject.data.examineRotation) > 2f && detailRotationFix)
            {
                UIManager.instance.detailObjectPivot.GetChild(0).transform.localRotation = Quaternion.Slerp(currentRotation, detailObject.data.examineRotation, Time.deltaTime * 5f);
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

    public void EquipMap()
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
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
    }
}
