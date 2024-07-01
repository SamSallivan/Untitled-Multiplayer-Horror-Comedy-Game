using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryBackSlot : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public event Action<I_InventoryItem> OnReturnRequiredType = delegate { };

    public int GetIndex()
    {
        return transform.GetSiblingIndex(); ;
    }

    public void ClearDelegate()
    {
        OnReturnRequiredType = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("???");
        Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        if (mouseInput != Vector2.zero && InventoryManager.instance.hoveredIndex != GetIndex())
        {
            foreach (Transform child in transform.parent)
            {
                child.GetChild(0).gameObject.SetActive(false);
            }

            transform.GetChild(0).gameObject.SetActive(true);

            //InventoryManager.instance.selectedPosition = InventoryManager.instance.GetGridPosition(GetIndex());
            InventoryManager.instance.hoveredIndex = GetIndex();

            InventoryManager.instance.UpdateSelection(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("???");
        if (InventoryManager.instance.detailObjectDrag)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 1)
        {
            InventoryManager.instance.selectedPosition = InventoryManager.instance.GetGridPosition(GetIndex());
            InventoryManager.instance.hoveredIndex = GetIndex();
            InventoryManager.instance.UpdateSelection();

            for (int i = 0; i < UIManager.instance.inventoryBackGrid.transform.childCount; i++)
            {
                UIManager.instance.inventoryBackGrid.transform.GetChild(i).GetComponent<Image>().color = new Color(0.085f, 0.085f, 0.085f, 0.5f);
            }
            UIManager.instance.inventoryBackGrid.transform.GetChild(GetIndex()).GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.85f, 0.5f);


            if (InventoryManager.instance.requireItemType)
            {
                if (InventoryManager.instance.requireItemList.Count > GetIndex())
                {
                    OnReturnRequiredType?.Invoke(InventoryManager.instance.requireItemList[GetIndex()]);
                    print(InventoryManager.instance.requireItemList[GetIndex()].itemData.title);
                    InventoryManager.instance.CloseInventory();
                }
                return;
            }

            else
            {
                if (InventoryManager.instance.inventoryItemList.Count > GetIndex())
                {
                    I_InventoryItem item = InventoryManager.instance.inventoryItemList[GetIndex()];
                    InventoryManager.instance.EquipItem(item);
                }
            }
        }


        /*if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 2)
        {
        }*/

        /*else
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (UIManager.instance.inventoryItemGrid.transform.childCount > GetIndex())
                {
                    InventoryItem item = UIManager.instance.inventoryItemGrid.transform.GetChild(GetIndex()).GetComponent<InventorySlot>().inventoryItem;
                    if (item.data.isEquippable)
                    {
                        InventoryManager.instance.EquipItem(item);
                    }
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {

            }
        }*/

        /*if (eventData.button == PointerEventData.InputButton.Right)
        {
            InventoryManager.instance.selectedPosition = InventoryManager.instance.GetGridPosition(GetIndex());
            InventoryManager.instance.selectedIndex = GetIndex();

            if (UIManager.instance.inventoryItemGrid.transform.childCount > GetIndex())
            {
                InventoryItem item = UIManager.instance.inventoryItemGrid.transform.GetChild(GetIndex()).GetComponent<InventorySlot>().inventoryItem;
                InventoryManager.instance.DropItem(item);
            }
        }*/
    }
}
