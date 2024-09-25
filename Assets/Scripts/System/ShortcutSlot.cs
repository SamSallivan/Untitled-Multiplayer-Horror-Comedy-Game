using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class ShortcutSlot : MonoBehaviour
{
    public int shortcutIndex;

    public InventorySlot sourceInventorySlot;
    public TMP_Text name;
    public TMP_Text amount;
    public Image image;
    public Image durabilityFill;

    public GameObject outline;

    public void Start()
    {
        shortcutIndex = transform.GetSiblingIndex();
        sourceInventorySlot = InventoryManager.instance.inventorySlotList[shortcutIndex];
    }

    public void Update()
    {
        UpdateShortcutSlotDisplay();
    }

    public void UpdateShortcutSlotDisplay()
    {
        if (InventoryManager.instance.equippedSlotIndex == shortcutIndex)
        {
            outline.SetActive(true);
        }
        else
        {
            outline.SetActive(false);
        }

        if (sourceInventorySlot != null && sourceInventorySlot.inventoryItem != null)
        {
            I_InventoryItem inventoryItem = sourceInventorySlot.inventoryItem;
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
}
