using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DetailPanelRaycast : MonoBehaviour, IPointerEnterHandler, IPointerUpHandler, IPointerDownHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        UIManager.instance.detailObjectInBound = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // if (eventData.button == PointerEventData.InputButton.Left && InventoryManager.instance.draggedItem != null)
        // {
        //     InventoryManager.instance.DropItemFromInventory(InventoryManager.instance.draggedItem);
        // }
        // InventoryManager.instance.draggedItem = null;
    }
}
