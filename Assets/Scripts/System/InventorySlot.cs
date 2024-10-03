using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class 
InventorySlot : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler
{
    public event Action<I_InventoryItem> OnReturnRequiredType = delegate { };

    public TMP_Text amount;
    public Image image;
    public Image durabilityFill;

    public I_InventoryItem inventoryItem;

    public Image background;
    public GameObject outline;
    
    public void Update()
    {
        UpdateInventorySlotDisplay();
    }
    
    public int GetIndex()
    {
        return transform.GetSiblingIndex(); ;
    }

    public void UpdateInventorySlotDisplay()
    {
        if (inventoryItem != null)
        {
            image.sprite = inventoryItem.itemData.sprite;
            image.color = new UnityEngine.Color(1, 1, 1, 1);
            amount.text = inventoryItem.itemData.isStackable ? $"{inventoryItem.itemStatus.amount}" : "";

            if (inventoryItem.itemData.hasDurability)
            {
                durabilityFill.fillAmount = inventoryItem.itemStatus.durability;
            }
            else
            {
                durabilityFill.fillAmount = 0;
            }
        }
        else
        {
            image.sprite = null;
            image.color = new UnityEngine.Color(1, 1, 1, 0);
            amount.text = "";
            durabilityFill.fillAmount = 0;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        if (mouseInput != Vector2.zero && InventoryManager.instance.hoveredSlot != this)
        {
            foreach (Transform child in transform.parent)
            {
                child.GetComponent<InventorySlot>().outline.SetActive(false);
            }

            outline.SetActive(true);

            InventoryManager.instance.hoveredSlot = this;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        // if (mouseInput != Vector2.zero && InventoryManager.instance.hoveredIndex != GetIndex())
        // {
        //     outline.SetActive(false);

        //     InventoryManager.instance.hoveredIndex = -1;
        // }

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (InventoryManager.instance.detailObjectDrag)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 1)
        {
            InventoryManager.instance.selectedSlot = this;

            InventoryManager.instance.UpdateSelection();

            if (InventoryManager.instance.requireItemType)
            {
                if (inventoryItem != null)
                {
                    OnReturnRequiredType?.Invoke(InventoryManager.instance.requireItemList[GetIndex()]);
                    print(InventoryManager.instance.requireItemList[GetIndex()].itemData.title);
                    InventoryManager.instance.CloseInventory();
                }
                return;
            }

            else
            {
                if (inventoryItem != null)
                {
                    //I_InventoryItem item = InventoryManager.instance.inventoryItemList[GetIndex()];
                    //InventoryManager.instance.EquipItem(inventoryItem);
                }
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && inventoryItem != null)
        {
            InventoryManager.instance.draggedItem = inventoryItem;
            UIManager.instance.draggedImage.enabled = true;
            UIManager.instance.draggedImage.sprite = inventoryItem.itemData.sprite;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && InventoryManager.instance.draggedItem != null)
        {
            if (UIManager.instance.detailObjectInBound)
            {
                InventoryManager.instance.DropItemFromInventory(InventoryManager.instance.draggedItem);
            }
            else
            {
                InventorySlot targetSlot = InventoryManager.instance.hoveredSlot;
                if (targetSlot != this)
                {
                    if(targetSlot.inventoryItem && targetSlot.inventoryItem.itemData == inventoryItem.itemData && inventoryItem.itemData.isStackable)
                    {
                        
                        while (targetSlot.inventoryItem.itemStatus.amount < inventoryItem.itemData.maxStackAmount && inventoryItem.itemStatus.amount > 0)
                        {
                            targetSlot.inventoryItem.itemStatus.amount++;
                            targetSlot.inventoryItem.SetItemAmountRpc(targetSlot.inventoryItem.itemStatus.amount);
                            inventoryItem.itemStatus.amount--;
                            inventoryItem.SetItemAmountRpc(inventoryItem.itemStatus.amount);
                        }

                        if (inventoryItem.itemStatus.amount <= 0)
                        {
                            InventoryManager.instance.DestoryItemServerRpc(inventoryItem.NetworkObject);
                            inventoryItem = null;
                        }
                        // else
                        // {
                        //     InventoryManager.instance.AddItemToInventory(inventoryItem);
                        // }

                    }
                    else
                    {
                        I_InventoryItem temp = targetSlot.inventoryItem;
                        targetSlot.inventoryItem = InventoryManager.instance.draggedItem;
                        InventoryManager.instance.draggedItem.inventorySlot = targetSlot;
                        inventoryItem = temp;
                        if (temp != null)
                        {
                            temp.inventorySlot = this;
                        }
                    }

                    UpdateInventorySlotDisplay();
                    targetSlot.UpdateInventorySlotDisplay();

                    InventoryManager.instance.selectedSlot = targetSlot;

                    InventoryManager.instance.UpdateSelection();

                    InventoryManager.instance.UpdateEquippedItem();
                }
            }
        }
        InventoryManager.instance.draggedItem = null;
        UIManager.instance.draggedImage.enabled = false;
    }

    public void ClearDelegate()
    {
        OnReturnRequiredType = null;
    }
}
