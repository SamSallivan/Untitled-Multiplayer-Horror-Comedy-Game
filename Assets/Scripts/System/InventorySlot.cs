using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class InventorySlot : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler
{
    public event Action<I_InventoryItem> OnReturnRequiredType = delegate { };

    public TMP_Text name;
    public TMP_Text amount;
    public Image image;
    public Image leftHandIcon;
    public Image rightHandIcon;

    public I_InventoryItem inventoryItem;

    public TMP_Text durability;
    public Image background;
    public GameObject outline;

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
            name.text = inventoryItem.itemData.name;
            amount.text = $"{inventoryItem.itemStatus.amount}";

            if (inventoryItem == InventoryManager.instance.equippedItem)
            {
                rightHandIcon.enabled = true;
            }
            else
            {
                rightHandIcon.enabled = false;
            }
        }
        else
        {
            image.sprite = null;
            image.color = new UnityEngine.Color(1, 1, 1, 0);
            name.text = "";
            amount.text = "";
            rightHandIcon.enabled = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        if (mouseInput != Vector2.zero && InventoryManager.instance.hoveredIndex != GetIndex())
        {
            foreach (Transform child in transform.parent)
            {
                child.GetComponent<InventorySlot>().outline.SetActive(false);
            }

            outline.SetActive(true);

            InventoryManager.instance.hoveredIndex = GetIndex();
            //InventoryManager.instance.selectedPosition = InventoryManager.instance.GetGridPosition(GetIndex());

            //InventoryManager.instance.UpdateSelection(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (InventoryManager.instance.detailObjectDrag)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 1)
        {
            //InventoryManager.instance.selectedIndex = GetIndex();
            InventoryManager.instance.selectedPosition = InventoryManager.instance.GetGridPosition(GetIndex());

            InventoryManager.instance.UpdateSelection();

            /*for (int i = 0; i < UIManager.instance.inventoryItemGrid.transform.childCount; i++)
            {
                UIManager.instance.inventoryItemGrid.transform.GetChild(i).GetComponent<InventorySlot>().background.color = new Color(0.085f, 0.085f, 0.085f, 0.5f);
            }
            UIManager.instance.inventoryItemGrid.transform.GetChild(GetIndex()).GetComponent<InventorySlot>().background.color = new Color(0.85f, 0.85f, 0.85f, 0.5f);*/


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
                InventorySlot targetSlot = InventoryManager.instance.inventorySlotList[InventoryManager.instance.hoveredIndex];
                if (targetSlot != this)
                {
                    I_InventoryItem temp = targetSlot.inventoryItem;
                    targetSlot.inventoryItem = InventoryManager.instance.draggedItem;
                    InventoryManager.instance.draggedItem.inventorySlot = targetSlot;
                    inventoryItem = temp;
                    if (temp != null)
                    {
                        temp.inventorySlot = this;
                    }

                    UpdateInventorySlotDisplay();
                    targetSlot.UpdateInventorySlotDisplay();
                    InventoryManager.instance.selectedPosition = InventoryManager.instance.GetGridPosition(targetSlot.GetIndex());

                    InventoryManager.instance.UpdateSelection();

                    InventoryManager.instance.UpdateEquippedItem();
                }
            }
        }
        InventoryManager.instance.draggedItem = null;
        UIManager.instance.draggedImage.enabled = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {

    }

    public void ClearDelegate()
    {
        OnReturnRequiredType = null;
    }
}
