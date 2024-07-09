using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;
using static Interactable;

[CreateAssetMenu(fileName = "ItemData", menuName = "ScriptableObjects/ItemData")]
public class ItemData : ScriptableObject
{
    public enum ItemType
    {
        Standard,
        //Tape,
        //Fish,
        //Key
    }
/*    public enum EquipType
    {
        Left,
        Right,
        Both
    }*/

    [Foldout("Base Info", true)]
    public ItemType type;
    public string title;
    public Sprite sprite;
    [TextArea(5, 5)]
    public string description;

    [Foldout("Settings", true)]
    public bool isStackable;
    [ConditionalField(nameof(isStackable))]
    public int maxStackAmount = 1;
    //public bool isEquippable;
    //[ConditionalField(nameof(isEquippable))]
    //public EquipType equipType;
    public Vector3 equipPosition;
    public Vector3 equipRotation;

    //public bool isDroppable;
    //[ConditionalField(nameof(isDroppable))]
    public GameObject dropObject;

    //public bool isExaminable;
    public float examineScale = 1;
    public Quaternion examineRotation;

    /*[ConditionalField(nameof(isExaminable))]
    public bool isReadable;
    [ConditionalField(nameof(isReadable))]
    [TextArea(10, 10)]
    public string readText;*/

    //[ConditionalField(nameof(type), false, ItemType.Tape)]
    //public string recordingName;

    //[ConditionalField(nameof(type), false, ItemType.Tape)]
    //public DialogueData recording;

    //[ConditionalField(nameof(type), false, ItemType.Fish)]
    //public float reelDecreaseCoefficient;
    //[ConditionalField(nameof(type), false, ItemType.Fish)]
    //public Vector2 nimbbleInterval;
}


[System.Serializable]
public struct ItemStatus
{
    public int amount;
    public int durability;

    public ItemStatus(int amount, int durability){
        this.amount = amount;
        this.durability = durability;
    }
}

/*[System.Serializable]
public class InventoryItem
{
    public ItemData itemData;
    public ItemStatus itemStatus;
    public InventorySlot inventorySlot;

    public InventoryItem()
    {
    }

    public InventoryItem(ItemData data, ItemStatus status, InventorySlot slot)
    {
        this.itemData = data;
        this.itemStatus = status;
        this.inventorySlot = slot;
    }
}*/