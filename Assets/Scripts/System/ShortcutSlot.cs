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
    public TMP_Text durability;

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

        if (sourceInventorySlot != null)
        {
            image.sprite = sourceInventorySlot.image.sprite;
            image.color = sourceInventorySlot.image.color;
            name.text = sourceInventorySlot.name.text;
            amount.text = sourceInventorySlot.amount.text;

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
