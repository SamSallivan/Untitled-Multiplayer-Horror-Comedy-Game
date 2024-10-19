using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class I_Deposit : Interactable
{
    public string ObjectiveProgressTrigger = "";
    
    public override IEnumerator InteractionEvent()
    {
        if (GameSessionManager.Instance.localPlayerController.currentEquippedItem != null && GameSessionManager.Instance.localPlayerController.currentEquippedItem.itemData.depositable)
        {
            ObjectiveManager.instance.AddProgressToObjective(ObjectiveProgressTrigger, GameSessionManager.Instance.localPlayerController.currentEquippedItem.itemData.depositProgress);
            I_InventoryItem inventoryItem = GameSessionManager.Instance.localPlayerController.currentEquippedItem;
            InventoryManager.instance.DiscardEquippedItem();
            inventoryItem.DestoryItemServerRpc(); 
        }
        yield break;
    }

    public override bool CustomRequirement()
    {
        if (GameSessionManager.Instance.localPlayerController.currentEquippedItem != null && GameSessionManager.Instance.localPlayerController.currentEquippedItem.itemData.depositable)
        {
            return true;
        }
        
        UIManager.instance.Notify("Depositable item required");
        return false;
    }
}