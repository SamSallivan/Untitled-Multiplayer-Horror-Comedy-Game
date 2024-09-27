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

    [FoldoutGroup("Settings")]
    public bool requireHold;

    [FoldoutGroup("Settings")]
    [ShowIf("requireHold")]
    public float requireHoldDuration = 1f;
    
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
    
    [FoldoutGroup("Values")]
    [ShowIf(nameof(onceOnly))]
    [ReadOnly]
    public bool interactedOnce;

    [FoldoutGroup("Values")]
    [ShowIf("requireHold")]
    [ReadOnly]
    public bool isHeld;

    [FoldoutGroup("Values")]
    [ShowIf("requireHold")]
    [ReadOnly]
    public float heldDuration;

    public void Update()
    {
        if (isHeld)
        {
            heldDuration += Time.deltaTime;
            UIManager.instance.interactionHoldBar.fillAmount = heldDuration / requireHoldDuration;
            
            if (heldDuration >= requireHoldDuration)
            {
                Interact();
                CancelInteract();
            }
        }
    }

    public void PerformInteract()
    {
        if (!requireHold)
        {
            Interact();
        }
        else
        {
            isHeld = true;
        }
    }

    public void CancelInteract()
    {
        if (!requireHold)
        {
            return;
        }
        else
        {
            isHeld = false;
            heldDuration = 0;
            UIManager.instance.interactionHoldBar.fillAmount = 0;
        }
    }
    
    public void Interact()
    {

        if (!interactedOnce)
        {
            /*switch (interactionType)
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
                    StartCoroutine(InteractionEvent());
                    break;
            }*/
            
            StartCoroutine(InteractionEvent());
            
            if(activated && excludeOtherInteraction)
            {
                GameSessionManager.Instance.localPlayerController.exclusiveInteractable = this;
            }
            else if(!activated && excludeOtherInteraction)
            {
                GameSessionManager.Instance.localPlayerController.exclusiveInteractable = null;
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

    public virtual IEnumerator InteractionEvent()
    {
        yield break;
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

        if (textPrompt != "" && interactionType != InteractionType.None)
        {
            UIManager.instance.interactionPrompt.text = "[E] ";
            UIManager.instance.interactionPrompt.text += activated ? textPromptActivated : textPrompt;
        }

        if (requireHold)
        {
            UIManager.instance.interactionHoldBarBackground.enabled = true;
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

        if (requireHold)
        {
            UIManager.instance.interactionHoldBarBackground.enabled = false;
        }
        
        CancelInteract();
    }

}
