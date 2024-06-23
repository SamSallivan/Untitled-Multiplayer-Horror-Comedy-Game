using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class InventorySlot : MonoBehaviour, IPointerExitHandler
{
    public TMP_Text name;
    public TMP_Text amount;
    public Image image;
    public Image leftHandIcon;
    public Image rightHandIcon;

    public InventoryItem inventoryItem;

    public TMP_Text durability;

    public int GetIndex()
    {
        return transform.GetSiblingIndex(); ;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //InventoryManager.instance.selectedPosition = InventoryManager.instance.GetGridPosition(GetIndex());
    }

    public void OnPointerExit(PointerEventData eventData)
    {

    }
/*
    public void OnPointerClick(PointerEventData eventData)
    {
        InventoryManager.instance.EquipItem(inventoryItem);
    }*/
}
