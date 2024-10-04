using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using TMPro;
using Unity.Netcode;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

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
    public float requiredHoldDuration = 1f;

    [FoldoutGroup("Settings")]
    public bool requireItem;

    [FoldoutGroup("Settings")]
    [ShowIf("requireItem")]
    public ItemData requiredItemData;

    [FoldoutGroup("Settings")]
    public EmoteData specialAnimation;

    [FoldoutGroup("Settings")]
    public Transform playerPositionTargetTransform;

    [FoldoutGroup("Settings")]
    public Transform playerLookAtTargetTransform;
    
    [FoldoutGroup("Settings")]

    [ShowIf(nameof(interactionType), InteractionType.InventoryItem)]
    public ItemData itemData;
    
    [FoldoutGroup("Settings")]
    [ShowIf(nameof(interactionType), InteractionType.InventoryItem)]
    public ItemStatus itemStatus;
    
    [FoldoutGroup("Settings")]
    [ShowIf(nameof(interactionType), InteractionType.InventoryItem)]
    public bool openInventoryOnPickUp;
    
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
            if (!CustomRequirement())
            {
                ResetInteract();
                UIManager.instance.Notify(requiredItemData.name + " required");
                return;
            }
            
            heldDuration += Time.deltaTime;
            UIManager.instance.interactionHoldBar.fillAmount = heldDuration / requiredHoldDuration;
            
            if (heldDuration >= requiredHoldDuration)
            {
                Interact();
            }
        }

        InteractableUpdate();
    }

    public void LateUpdate()
    {
        if (isHeld)
        {
            PlayerController playerController = GameSessionManager.Instance.localPlayerController;

            if (playerPositionTargetTransform)
            {
                Vector3 targetPosition = playerPositionTargetTransform.position;
                targetPosition.y = playerController.transform.position.y;
                playerController.rb.position = Vector3.Lerp(playerController.rb.position, targetPosition, Time.deltaTime * 5f);
            }

            if (playerLookAtTargetTransform)
            {
                float angleY = Quaternion.LookRotation(playerLookAtTargetTransform.position - playerController.mouseLookY.transform.position).eulerAngles.x;
                playerController.mouseLookY.SetRotation(Mathf.Lerp(playerController.mouseLookY.rotationY, -angleY, Time.deltaTime * 5));
                
                Quaternion angleX = Quaternion.LookRotation(playerLookAtTargetTransform.position - playerController.mouseLookX.transform.position);
                angleX.eulerAngles = new Vector3(0, angleX.eulerAngles.y, 0);
                playerController.mouseLookX.transform.rotation = Quaternion.Lerp(playerController.mouseLookX.transform.rotation, angleX, Time.deltaTime * 5);                 
                
            }
        }
    }

    public void PerformInteract()
    {
        if (!CustomRequirement())
        {
            UIManager.instance.Notify(requiredItemData.name + " required");
            return;
        }
        
        if (!requireHold)
        {
            Interact();
        }
        else
        {
            isHeld = true;
        }

        if (specialAnimation)
        {
            GameSessionManager.Instance.localPlayerController.inSpecialAnimation .Value = true;
            StartSpecialAnimationRpc(GameSessionManager.Instance.localPlayerController.localPlayerId);
        }
    }

    public void ResetInteract()
    {
        isHeld = false;
        heldDuration = 0;
        UIManager.instance.interactionHoldBar.fillAmount = 0;
        

        if (specialAnimation)
        {
            GameSessionManager.Instance.localPlayerController.inSpecialAnimation .Value = false;
            StopSpecialAnimationRpc(GameSessionManager.Instance.localPlayerController.localPlayerId);
        }
    }


    [Rpc(SendTo.Everyone)]
    public void StartSpecialAnimationRpc(int playerId)
    {
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        playerController.playerAnimationController.StartEmoteAnimation(specialAnimation);
    }
    
    [Rpc(SendTo.Everyone)]
    public void StopSpecialAnimationRpc(int playerId)
    {
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        playerController.playerAnimationController.StopEmoteAnimation();
    }
    
    public void Interact()
    {
        if (!interactedOnce)
        {
            
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
        
        ResetInteract();
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

        if (!CustomRequirement())
        {
            UIManager.instance.Notify(requiredItemData.name + " required");
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

        //ResetInteract();
        
        if (!specialAnimation)
        {
            ResetInteract();
        }
    }

    public virtual bool CustomRequirement()
    {
        if (requireItem && requiredItemData != null)
        {
            if (GameSessionManager.Instance.localPlayerController.currentEquippedItem == null || GameSessionManager.Instance.localPlayerController.currentEquippedItem.itemData != requiredItemData)
            {
                return false;
            }
        }
        
        return true;
    }
    

    public virtual void InteractableUpdate()
    {
    }
    
}
