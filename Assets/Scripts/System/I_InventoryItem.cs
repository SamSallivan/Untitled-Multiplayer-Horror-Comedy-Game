using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class I_InventoryItem : Interactable
{
    public event Action OnPickUp = delegate { };

    public override IEnumerator InteractionEvent()
    {
        
        if(itemData != null)
        {
            InventoryItem newItem = InventoryManager.instance.AddItem(itemData, itemStatus);
            Destroy(transform.gameObject);
            OnPickUp?.Invoke();
            if (newItem != null)
            {

                if (equipOnPickUp && itemData.isEquippable)
                {
                    InventoryManager.instance.EquipItem(newItem);
                }

                if (openInventoryOnPickUp)
                {
                    InventoryManager.instance.OpenInventory();
                    InventoryManager.instance.selectedPosition =
                        InventoryManager.instance.GetGridPosition(newItem.slot.GetIndex());
                }

                UnTarget();
            }
        }
        yield return null; 
    }

    // public IEnumerator PickUp()
    // {
    //     yield return null;
    // }
    // public IEnumerator Examine()
    // {
    //     yield return null;
    // }

    public override void Target()
    {
        if (highlightTarget != null)
        {
            if (!highlightTarget.GetComponent<OutlineRenderer>())
            {
                OutlineRenderer outline = highlightTarget.AddComponent<OutlineRenderer>();
                outline.OutlineMode = OutlineRenderer.Mode.OutlineVisible;
                outline.OutlineWidth = 10;
            }
        }
        UIManager.instance.interactionName.text = textName;
        UIManager.instance.interactionName.text += !itemData.isStackable ? "" : " x " + itemStatus.amount;
        //UI.instance.interactionPrompt.text = textPrompt;

        if (textPrompt != "" && interactionType != InteractionType.None)
        {
            UIManager.instance.interactionPrompt.text = "[E] ";
            UIManager.instance.interactionPrompt.text += activated ? textPromptActivated : textPrompt;
            //enable button prompt image instead
            //UIManager.instance.interactionPromptAnimation.Play("PromptButtonAppear");
        }
    }
}
