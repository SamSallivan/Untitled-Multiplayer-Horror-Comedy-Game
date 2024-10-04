using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class I_CampFire : Interactable
{
    public override IEnumerator InteractionEvent()
    {
        if (GameSessionManager.Instance.localPlayerController.currentEquippedItem.itemData == requiredItemData)
        {
            ObjectiveManager.instance.AddProgressToObjective("Camp Fire", 1);
            I_InventoryItem inventoryItem = GameSessionManager.Instance.localPlayerController.currentEquippedItem;
            InventoryManager.instance.DiscardEquippedItem();
            inventoryItem.DestoryItemServerRpc(); 
        }
        yield break;
    }
}
