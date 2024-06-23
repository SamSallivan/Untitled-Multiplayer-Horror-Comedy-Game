using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyBox;
using UnityEngine.Events;
using System;
using TMPro;
using Unity.VisualScripting;

public enum InteractionType
{
    None = 0,
    Examine = 1,
    InventoryItem = 2,
    Custom = 3,
    CustomToggle = 4
}

[Serializable]
public class MyEvent : UnityEvent<string, GameObject> { }

public class Interactable : MonoBehaviour
{

    //[Separator("Base Info")]
    [Foldout("Base Info", true)]
    public string textName;

    [ConditionalField(nameof(interactionType), true, InteractionType.None)]
    public string textPrompt;

    [ConditionalField(nameof(interactionType), false, InteractionType.CustomToggle)]
    public string textPromptActivated;

    public GameObject highlightTarget;

    //public DialogueData dialogueOnInteraction;


    [Foldout("Settings", true)]
    public InteractionType interactionType;

    [ConditionalField(nameof(interactionType), false, InteractionType.Examine, InteractionType.CustomToggle)]
    public bool onceOnly;

    [ConditionalField(nameof(onceOnly))]
    [ReadOnly]
    public bool interactedOnce;
    
    [ConditionalField(nameof(interactionType), false, InteractionType.Examine)]//, InteractionType.ExamineAndInventory)]
    //public bool hasText;
    //[ConditionalField(nameof(hasText))]
    [TextArea(10, 10)]
    public string examineText;
    public Sprite examineImage;

    [ConditionalField(nameof(interactionType), false, InteractionType.InventoryItem)]//, InteractionType.ExamineAndInventory)]
    public ItemData itemData;
    [ConditionalField(nameof(interactionType), false, InteractionType.InventoryItem)]//, InteractionType.ExamineAndInventory)]
    public ItemStatus itemStatus;
    [ConditionalField(nameof(interactionType), false, InteractionType.InventoryItem)]//, InteractionType.ExamineAndInventory)]
    public bool openInventoryOnPickUp;
    [ConditionalField(nameof(interactionType), false, InteractionType.InventoryItem)]//, InteractionType.ExamineAndInventory)]
    public bool equipOnPickUp;

    [ConditionalField(nameof(interactionType), false, InteractionType.CustomToggle)]
    [ReadOnly]
    public bool activated;
    [ConditionalField(nameof(interactionType), false, InteractionType.CustomToggle)]
    public bool excludeOtherInteraction;
    
    //public Trigger triggerZone;

    public virtual IEnumerator InteractionEvent()
    {
        yield break;
    }

    public virtual void Interact()
    {

        if (!interactedOnce)
        {
            switch (interactionType)
            {
                case InteractionType.None:
                    break;

                case InteractionType.Examine:
                    StartCoroutine(InteractionEvent());
                    break;

                case InteractionType.InventoryItem:
                    StartCoroutine(InteractionEvent());
                    break;

                case InteractionType.Custom:
                    StartCoroutine(InteractionEvent());
                    break;

                case InteractionType.CustomToggle:

                    if(!activated && excludeOtherInteraction)
                    {
                        PlayerController.instance.exclusiveInteractable = this;
                    }
                    else if(activated && excludeOtherInteraction)
                    {
                        PlayerController.instance.exclusiveInteractable = null;
                    }

                    StartCoroutine(InteractionEvent());
                    break;
            }

            if (onceOnly)
            {
                interactedOnce = true;
            }

            // if (dialogueOnInteraction != null)
            // {
            //     DialogueManager.instance.OverrideDialogue(dialogueOnInteraction);
            // }
        }
    }

    public virtual void Target()
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
        //UI.instance.interactionPrompt.text = textPrompt;

        if (textPrompt != "" && interactionType != InteractionType.None)
        {
            UIManager.instance.interactionPrompt.text = "[E] ";
            UIManager.instance.interactionPrompt.text += activated ? textPromptActivated : textPrompt;
            //enable button prompt image instead
            //UIManager.instance.interactionPromptAnimation.Play("PromptButtonAppear");
        }
    }

    public void UnTarget()
    {
        if (highlightTarget != null)
        {
            Destroy(highlightTarget.GetComponent<OutlineRenderer>());
        }
        UIManager.instance.interactionName.text = "";
        UIManager.instance.interactionPrompt.text = "";
        //disable button prompt image here
        //UIManager.instance.interactionPromptAnimation.Play("PromptButtonDisappear");
    }
    
    // [ServerRpc(RequireOwnership = false)]
    // public void DestroyServerRpc()
    // {
    //     //this.NetworkObject.Despawn();
    //     DestroyClientRpc();
    // }

    // [ClientRpc]
    // public void DestroyClientRpc()
    // {
    //     //this.NetworkObject.Despawn();
    //     Destroy(this.gameObject);
    // }
}
