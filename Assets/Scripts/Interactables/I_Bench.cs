using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using RootMotion.FinalIK;

public class I_Bench : Interactable
{
    public NetworkVariable<int> playerId = new NetworkVariable<int>(-1);
    public PlayerController player;
    
    public List<IkAnimation> ikAnimations = new List<IkAnimation>();
    
    public Transform benchPlayerPositionTargetTransform;
    public float benchPlayerPositionInterpolationSpeed = 10f;
    public bool lockMovement = false;
    
    
    public Transform benchPlayerLookTargetTransform;
    public bool clampHorizontalLookRotation = true;
    public float maxHorizontalLookRotation = 90f;
    public float benchPlayerRotationInterpolationSpeed = 5f;

    public float lockTurnAnimationDelay = 1f;
    public float lockTurnAnimationDelayTimer = 0f;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        OnplayerIdChanged(-1, playerId.Value);
        playerId.OnValueChanged += OnplayerIdChanged;
    }
    
    
    public override IEnumerator InteractionEvent()
    {
        ActivateRpc(GameSessionManager.Instance.localPlayerController.localPlayerId);
        yield break;
    }
    
    public override void InteractableUpdate()
    {
        if (player != null && player.IsOwner)
        {
            if (!player.isPlayerDead.Value && player.inputDir.sqrMagnitude == 0f)
            {
                if (benchPlayerPositionTargetTransform)
                {
                    Vector3 targetPosition = benchPlayerPositionTargetTransform.position;
                    targetPosition.y = player.transform.position.y;
                    player.rb.position = Vector3.Lerp(player.rb.position, targetPosition, Time.deltaTime * benchPlayerPositionInterpolationSpeed);
                }

                if (clampHorizontalLookRotation)
                {
                    if (Quaternion.Angle(player.mouseLookX.transform.rotation, Quaternion.LookRotation(benchPlayerLookTargetTransform.forward)) > maxHorizontalLookRotation)
                    {
                        player.mouseLookX.transform.rotation = Quaternion.Lerp(player.mouseLookX.transform.rotation, Quaternion.LookRotation(benchPlayerLookTargetTransform.forward), Time.deltaTime * benchPlayerRotationInterpolationSpeed);       
                    }
                }
            }
            else
            {
                DeactivateRpc();
            }
        }

        if (player)
        {
            if (lockTurnAnimationDelayTimer < lockTurnAnimationDelay)
            {
                lockTurnAnimationDelayTimer += Time.deltaTime;
            }
            else
            {
                player.playerAnimationController.turnAnimation = false;
                
                if (clampHorizontalLookRotation)
                {
                    player.playerAnimationController.bodyRotationInterpolationSpeed = 0;
                }
            }
        }
    }

    public override bool CustomRequirement()
    {
        if (!player)
        {
            return true;
        }
        
        return false;
    }
    
    [Rpc(SendTo.Server)]
    public void ActivateRpc(int playerId)
    {
        if (activated.Value)
        {
            return;
        }
        
        activated.Value = true;
        this.playerId.Value = playerId;
        StartInteractIkAnimationRpc(playerId);
        
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        if (lockMovement)
        {
            playerController.LockMovementRpc(true);
        }
    }
    
    [Rpc(SendTo.Server)]
    public void DeactivateRpc()
    {
        if (!player.isPlayerDead.Value && lockMovement)
        {
            player.LockMovementRpc(false);
        }
        
        StopInteractIkAnimationRpc(player.localPlayerId);
        activated.Value = false;
        playerId.Value = -1;
    }


    [Rpc(SendTo.Everyone)]
    public void StartInteractIkAnimationRpc(int playerId)
    {
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        foreach (IkAnimation ikAnimation in ikAnimations)
        {
            playerController.playerAnimationController.GetComponent<InteractionSystem>().StartInteraction(ikAnimation.effector, ikAnimation.interactionObject, true);
        }
    }
    
    [Rpc(SendTo.Everyone)]
    public void StopInteractIkAnimationRpc(int playerId)
    {
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        foreach (IkAnimation ikAnimation in ikAnimations)
        {
            playerController.playerAnimationController.GetComponent<InteractionSystem>().StopInteraction(ikAnimation.effector);
            if (ikAnimation.effector == FullBodyBipedEffector.RightHand)
            {
                playerController.playerAnimationController.rightArmTransform.GetComponent<HandPoser>().weight = 0;
            }
            if (ikAnimation.effector == FullBodyBipedEffector.LeftHand)
            {
                playerController.playerAnimationController.leftArmTransform.GetComponent<HandPoser>().weight = 0;
            }
            if (ikAnimation.effector == FullBodyBipedEffector.RightFoot)
            {
                playerController.playerAnimationController.rightFootTransform.GetComponent<HandPoser>().weight = 0;
            }
            if (ikAnimation.effector == FullBodyBipedEffector.LeftFoot)
            {
                playerController.playerAnimationController.leftFootTransform.GetComponent<HandPoser>().weight = 0;
            }
        }

        lockTurnAnimationDelayTimer = 0;
        player.playerAnimationController.turnAnimation = true;
        player.playerAnimationController.bodyRotationInterpolationSpeed = 3;
    }
    
    public void OnplayerIdChanged(int prevValue, int newValue)
    {
        if (playerId.Value != -1)
        {
            player = GameSessionManager.Instance.playerControllerList[playerId.Value];
        }
        else
        {
            player = null;
        }
    }
}
