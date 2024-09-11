using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using TMPro;
using Unity.Netcode;
using Sirenix.OdinInspector;

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

public abstract class Interactable : NetworkBehaviour
{

    [FoldoutGroup("Base Info")]
    public string textName;

    [FoldoutGroup("Base Info")]
    [HideIf(nameof(interactionType), InteractionType.None)]
    public string textPrompt;

    [FoldoutGroup("Base Info")]
    [ShowIf(nameof(interactionType), InteractionType.CustomToggle)]
    public string textPromptActivated;

    [FoldoutGroup("Base Info")]
    public GameObject highlightTarget;

    //[FoldoutGroup("Base Info")]
    //public DialogueData dialogueOnInteraction;

    [FoldoutGroup("Settings")]
    public InteractionType interactionType;

    [FoldoutGroup("Settings")]
    public bool onceOnly;

    [ShowIf(nameof(onceOnly))]
    [ReadOnly]
    public bool interactedOnce;
    
    [FoldoutGroup("Settings")]
    [ShowIf(nameof(interactionType), InteractionType.Examine)]
    public Sprite examineImage;

    [FoldoutGroup("Settings")]
    
    [ShowIf(nameof(interactionType), InteractionType.Examine)]
    public bool hasText;
    
    [FoldoutGroup("Settings")]

    [ShowIf(nameof(hasText))]
    [TextArea(10, 10)]
    public string examineText;
    
    [FoldoutGroup("Settings")]

    [ShowIf(nameof(interactionType), InteractionType.InventoryItem)]
    public ItemData itemData;
    
    [FoldoutGroup("Settings")]
    [ShowIf(nameof(interactionType), InteractionType.InventoryItem)]
    public ItemStatus itemStatus;
    
    [FoldoutGroup("Settings")]
    [ShowIf(nameof(interactionType), InteractionType.InventoryItem)]
    public bool openInventoryOnPickUp;

    //[ShowIf(nameof(interactionType), InteractionType.InventoryItem)]
    //public bool equipOnPickUp;
    
    [FoldoutGroup("Settings")]
    [ShowIf(nameof(interactionType), InteractionType.CustomToggle)]
    public bool excludeOtherInteraction;
    
    //[FoldoutGroup("Settings")]
    //public Trigger triggerZone;

    [FoldoutGroup("Values")]
    [ShowIf(nameof(interactionType), InteractionType.CustomToggle)]
    [ReadOnly]
    public bool activated;

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
                        GameSessionManager.Instance.localPlayerController.exclusiveInteractable = this;
                    }
                    else if(activated && excludeOtherInteraction)
                    {
                        GameSessionManager.Instance.localPlayerController.exclusiveInteractable = null;
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

}
