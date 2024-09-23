using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "ItemData", menuName = "ScriptableObjects/ItemData")]
public class ItemData : ScriptableObject
{
    // public enum ItemType
    // {
    //     Standard,
    //     Tape,
    //     Fish,
    //     Key
    // }

    // public enum EquipType
    // {
    //     Left,
    //     Right,
    //     Both
    // }

    // [FoldoutGroup("Base Info")]
    // public ItemType type;

    [FoldoutGroup("Base Info")]
    public string title;

    [FoldoutGroup("Base Info")]
    public Sprite sprite;

    [FoldoutGroup("Base Info")]
    [TextArea(5, 5)]
    public string description;

    [FoldoutGroup("Settings")]
    public bool isStackable;
    
    [FoldoutGroup("Settings")]
    [ShowIf(nameof(isStackable))]
    public int maxStackAmount = 1;
    
    [FoldoutGroup("Settings")]
    public int discoverScore= 5;

    //public bool isEquippable;

    //[ConditionalField(nameof(isEquippable))]
    //public EquipType equipType;
    
    [FoldoutGroup("Settings")]
    public Vector3 equipPosition;
    
    [FoldoutGroup("Settings")]
    public Vector3 equipRotation;
    
    [FoldoutGroup("Settings")]
    public GameObject dropObject;
    
    [FoldoutGroup("Settings")]
    public float examineScale = 1;
    
    [FoldoutGroup("Settings")]
    public Quaternion examineRotation;
    
    [FoldoutGroup("Settings")]
    public string equipAnimatorParameter;
    
    [FoldoutGroup("Settings")]
    public bool twoHandAnimation = false;

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
    public float durability;

    public ItemStatus(int amount, float durability){
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