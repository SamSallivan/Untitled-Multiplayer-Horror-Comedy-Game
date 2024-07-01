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
    public int shortcutId;

    public InventorySlot targetSlot;
    public TMP_Text name;
    public TMP_Text amount;
    public Image image;
    public TMP_Text durability;

    public GameObject outline;

    public void Start()
    {
        shortcutId = transform.GetSiblingIndex();
        targetSlot = InventoryManager.instance.inventorySlotList[shortcutId];
    }

    public void Update()
    {
        UpdateShortcutSlotDisplay();
    }

    public void UpdateShortcutSlotDisplay()
    {
        if (InventoryManager.instance.equippedSlotIndex == shortcutId)
        {
            outline.SetActive(true);
        }
        else
        {
            outline.SetActive(false);
        }

        if (targetSlot != null)
        {
            image.sprite = targetSlot.image.sprite;
            image.color = targetSlot.image.color;
            name.text = targetSlot.name.text;
            amount.text = targetSlot.amount.text;

        }
        else
        {
            image.sprite = null;
            image.color = new UnityEngine.Color(1, 1, 1, 0);
            name.text = "";
            amount.text = "";
        }
    }
}
