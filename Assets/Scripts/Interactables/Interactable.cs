using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using RootMotion.FinalIK;
using TMPro;
using Unity.Netcode;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

public enum InteractionType
{
    None = 0,
    InventoryItem = 2,
    Custom = 3,
    CustomToggle = 4
}

[Serializable]
public struct IkAnimation
{
    public FullBodyBipedEffector effector;
    public InteractionObject interactionObject;
}

[Serializable]
public class MyEvent : UnityEvent<string, GameObject> { }

public class Interactable : NetworkBehaviour
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
    [ShowIf(nameof(interactionType), InteractionType.Custom)]
    public bool onceOnly;
    
    [FoldoutGroup("Settings")]
    [ShowIf(nameof(interactionType), InteractionType.CustomToggle)]
    public bool excludeOtherInteraction;

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
    public bool forcePlayerTransform;
    
    [FoldoutGroup("Settings")]
    [ShowIf("forcePlayerTransform")]
    public Transform playerPositionTargetTransform;
    
    [FoldoutGroup("Settings")]
    [ShowIf("forcePlayerTransform")]
    public float playerPositionInterpolationSpeed = 10f;

    [FoldoutGroup("Settings")]
    [ShowIf("forcePlayerTransform")]
    public Transform playerLookAtTargetTransform;
    
    [FoldoutGroup("Settings")]
    [ShowIf("forcePlayerTransform")]
    public float playerRotationInterpolationSpeed = 10f;

    [FoldoutGroup("Settings")]
    [ShowIf("forcePlayerTransform")]
    public float forcePlayerTransformExtendDuration;
    
    private float forcePlayerTransformExtendDurationTimer;
    
    //[FoldoutGroup("Settings")]
    //public Trigger triggerZone;
    

    [FoldoutGroup("Animation")]
    public bool animationOnInteract;

    [FoldoutGroup("Animation")]
    [ShowIf("animationOnInteract")]
    public float animationOnInteractExtendedDuration;

    [FoldoutGroup("Animation")]
    [ShowIf("animationOnInteract")]
    public EmoteData interactAnimation;
    
    [FoldoutGroup("Animation")]
    [ShowIf("animationOnInteract")]
    public List<IkAnimation> ikAnimationsOnInteract = new List<IkAnimation>();
    

    [FoldoutGroup("Animation")]
    public bool animationOnInteractComplete;

    [FoldoutGroup("Animation")]
    [ShowIf("animationOnInteractComplete")]
    public float animationOnInteractCompleteDuration;

    [FoldoutGroup("Animation")]
    [ShowIf("animationOnInteractComplete")]
    public EmoteData interactCompleteAnimation;
    
    [FoldoutGroup("Animation")]
    [ShowIf("animationOnInteractComplete")]
    public List<IkAnimation> ikAnimationsOnInteractComplete = new List<IkAnimation>();

    [FoldoutGroup("Values")]
    [ShowIf(nameof(interactionType), InteractionType.CustomToggle)]
    public bool activated;
    
    [FoldoutGroup("Values")]
    [ShowIf(nameof(onceOnly))]
    public bool interactedOnce;

    [FoldoutGroup("Values")]
    [ShowIf("requireHold")]
    public bool isHeld;

    [FoldoutGroup("Values")]
    [ShowIf("requireHold")]
    public float heldDuration;
    
    
    public void PerformInteract()
    {
        if (!CustomRequirement())
        {
            UIManager.instance.Notify(requiredItemData.name + " required");
            return;
        }

        if (animationOnInteract)
        {
            if (interactAnimation != null)
            {
                GameSessionManager.Instance.localPlayerController.inSpecialAnimation.Value = true;
                StartInteractAnimationRpc(GameSessionManager.Instance.localPlayerController.localPlayerId);
            }

            StartInteractIkAnimationRpc(GameSessionManager.Instance.localPlayerController.localPlayerId);
        }
        
        if (!requireHold)
        {
            Interact();
        }
        else
        {
            isHeld = true;
        }
    }
    
    
    public void Update()
    {
        if (isHeld)
        {
            if (!CustomRequirement())
            {
                StartCoroutine(ResetInteract());
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
        if(forcePlayerTransformExtendDurationTimer > 0)
        {
            forcePlayerTransformExtendDurationTimer -= Time.deltaTime;
        }
        
        if (isHeld || forcePlayerTransformExtendDurationTimer > 0)
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
                if (angleY > 180f)
                {
                    angleY -= 360f;
                }
                playerController.mouseLookY.SetRotation(Mathf.Lerp(playerController.mouseLookY.rotationY, -angleY, Time.deltaTime * playerPositionInterpolationSpeed));
                Debug.Log(angleY);
                
                Quaternion angleX = Quaternion.LookRotation(playerLookAtTargetTransform.position - playerController.mouseLookX.transform.position);
                angleX.eulerAngles = new Vector3(0, angleX.eulerAngles.y, 0);
                playerController.mouseLookX.transform.rotation = Quaternion.Lerp(playerController.mouseLookX.transform.rotation, angleX, Time.deltaTime * playerRotationInterpolationSpeed);                 
                
            }
        }
    }

    public IEnumerator ResetInteract(bool completeInteract = false)
    {
        isHeld = false;
        heldDuration = 0;
        UIManager.instance.interactionHoldBar.fillAmount = 0;

        
        if (animationOnInteract)
        {
            if (completeInteract)
            {
                yield return new WaitForSeconds(animationOnInteractExtendedDuration);
            }
            
            GameSessionManager.Instance.localPlayerController.inSpecialAnimation .Value = false;
            StopSpecialAnimationRpc(GameSessionManager.Instance.localPlayerController.localPlayerId);

            StopInteractIkAnimationRpc(GameSessionManager.Instance.localPlayerController.localPlayerId);
        }

        if (!completeInteract)
        {
            yield break;
        }
        
        if (animationOnInteractComplete)
        {
            if (interactCompleteAnimation != null)
            {
                GameSessionManager.Instance.localPlayerController.inSpecialAnimation.Value = true;
                StartInteractCompleteAnimationRpc(GameSessionManager.Instance.localPlayerController.localPlayerId);
            }

            StartInteractCompleteIkAnimationRpc(GameSessionManager.Instance.localPlayerController.localPlayerId);
            
            yield return new WaitForSeconds(animationOnInteractCompleteDuration);

            if (interactCompleteAnimation != null)
            {
                GameSessionManager.Instance.localPlayerController.inSpecialAnimation.Value = false;
                StopSpecialAnimationRpc(GameSessionManager.Instance.localPlayerController.localPlayerId);
            }
            
            StopInteractCompleteIkAnimationRpc(GameSessionManager.Instance.localPlayerController.localPlayerId);
        }

    }


    [Rpc(SendTo.Everyone)]
    public void StartInteractAnimationRpc(int playerId)
    {
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        playerController.playerAnimationController.StartEmoteAnimation(interactAnimation);
    }


    [Rpc(SendTo.Everyone)]
    public void StartInteractIkAnimationRpc(int playerId)
    {
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        foreach (IkAnimation ikAnimation in ikAnimationsOnInteract)
        {
            playerController.playerAnimationController.GetComponent<InteractionSystem>().StartInteraction(ikAnimation.effector, ikAnimation.interactionObject, true);
        }
    }


    [Rpc(SendTo.Everyone)]
    public void StartInteractCompleteAnimationRpc(int playerId)
    {
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        playerController.playerAnimationController.StartEmoteAnimation(interactCompleteAnimation);
    }


    [Rpc(SendTo.Everyone)]
    public void StartInteractCompleteIkAnimationRpc(int playerId)
    {
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        foreach (IkAnimation ikAnimation in ikAnimationsOnInteractComplete)
        {
            playerController.playerAnimationController.GetComponent<InteractionSystem>().StartInteraction(ikAnimation.effector, ikAnimation.interactionObject, true);
        }
    }
    
    
    [Rpc(SendTo.Everyone)]
    public void StopSpecialAnimationRpc(int playerId)
    {
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        playerController.playerAnimationController.StopEmoteAnimation();
    }


    [Rpc(SendTo.Everyone)]
    public void StopInteractIkAnimationRpc(int playerId)
    {
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        foreach (IkAnimation ikAnimation in ikAnimationsOnInteract)
        {
            playerController.playerAnimationController.GetComponent<InteractionSystem>().StopInteraction(ikAnimation.effector);
        }
    }


    [Rpc(SendTo.Everyone)]
    public void StopInteractCompleteIkAnimationRpc(int playerId)
    {
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        foreach (IkAnimation ikAnimation in ikAnimationsOnInteractComplete)
        {
            playerController.playerAnimationController.GetComponent<InteractionSystem>().StopInteraction(ikAnimation.effector);
        }
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
        
        StartCoroutine(ResetInteract(completeInteract: true));
        forcePlayerTransformExtendDurationTimer = forcePlayerTransformExtendDuration;
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

        StartCoroutine(ResetInteract());
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
