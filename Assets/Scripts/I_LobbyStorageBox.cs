using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class I_LobbyStorageBox : Interactable
{
    public override IEnumerator InteractionEvent()
    {
        InventoryManager.instance.OpenInventory();
        UIManager.instance.detailPanel.SetActive(false);
        UIManager.instance.StoragePanel.SetActive(true);
        yield return null; 
    }
}
