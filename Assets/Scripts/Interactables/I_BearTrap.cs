using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using RootMotion.FinalIK;

public class I_BearTrap : Interactable
{
    public NetworkVariable<int> trappedPlayerId = new NetworkVariable<int>(-1);
    public PlayerController trappedPlayer;
    public List<IkAnimation> ikAnimations = new List<IkAnimation>();
    
    public Transform trappedPlayerPositionTargetTransform;
    public float trappedPlayerPositionInterpolationSpeed = 10f;
    public bool lockMovement = false;
    
    public Transform trappedPlayerLookTargetTransform;
    public bool clampHorizontalLookRotation = true;
    public float maxHorizontalLookRotation = 90f;
    public float benchPlayerRotationInterpolationSpeed = 5f;

    public float lockTurnAnimationDelay = 1f;
    public float lockTurnAnimationDelayTimer = 0f;
    
    public float damageOnHit = 25f;
    public float damageOnTick = 25f;
    public float tickInterval = 1f;
    public float tickCooldown = 0;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        OnTrappedPlayerIdChanged(-1, trappedPlayerId.Value);
        trappedPlayerId.OnValueChanged += OnTrappedPlayerIdChanged;
    }
    
    public override void InteractableUpdate()
    {
        if (trappedPlayer != null && trappedPlayer.IsOwner)
        {
            if (!trappedPlayer.isPlayerDead.Value)
            {
                if (trappedPlayerPositionTargetTransform)
                {
                    Vector3 targetPosition = trappedPlayerPositionTargetTransform.position;
                    //targetPosition.y = trappedPlayer.transform.position.y;
                    trappedPlayer.rb.position = Vector3.Lerp(trappedPlayer.rb.position, targetPosition, Time.deltaTime * trappedPlayerPositionInterpolationSpeed);
                }

                if (clampHorizontalLookRotation)
                {
                    if (Quaternion.Angle(trappedPlayer.mouseLookX.transform.rotation, Quaternion.LookRotation(trappedPlayerLookTargetTransform.forward)) > maxHorizontalLookRotation)
                    {
                        trappedPlayer.mouseLookX.transform.rotation = Quaternion.Lerp(trappedPlayer.mouseLookX.transform.rotation, Quaternion.LookRotation(trappedPlayerLookTargetTransform.forward), Time.deltaTime * benchPlayerRotationInterpolationSpeed);       
                    }
                }

                if (tickCooldown > 0)
                {
                    tickCooldown -= Time.deltaTime;
                }
                else
                {
                    tickCooldown = tickInterval;
                    trappedPlayer.TakeDamage(damageOnTick, new Vector3(0, 0, 0.25f));
                }
            }
            else
            {
                DeactivateTrapRpc();
            }
        }

        if (trappedPlayer)
        {
            if (lockTurnAnimationDelayTimer < lockTurnAnimationDelay)
            {
                lockTurnAnimationDelayTimer += Time.deltaTime;
            }
            else
            {
                trappedPlayer.playerAnimationController.turnAnimation = false;
                
                if (clampHorizontalLookRotation)
                {
                    trappedPlayer.playerAnimationController.bodyRotationInterpolationSpeed = 0;
                }
            }
        }

        if (isHeld && trappedPlayer != GameSessionManager.Instance.localPlayerController)
        {
            GameSessionManager.Instance.localPlayerController.crouching = true;
            GameSessionManager.Instance.localPlayerController.crouchingNetworkVariable.Value = true;
        }
    }
    
    public override IEnumerator InteractionEvent()
    {
        DeactivateTrapRpc();
        yield break;
    }

    public override bool CustomRequirement()
    {
        if (trappedPlayer)
        {
            return true;
        }
        
        return false;
    }
    
    [Rpc(SendTo.Server)]
    public void ActivateTrapRpc(int playerId = -1)
    {
        if (activated.Value)
        {
            return;
        }
        
        activated.Value = true;

        if (playerId == -1)
        {
            return;
        }
        
        trappedPlayerId.Value = playerId;
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        
        Vector3 direction = transform.position - playerController.transform.position;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        //playerController.playerAnimationController.targetBodyRotation = playerController.transform.GetChild(0).rotation;
        playerController.playerAnimationController.transform.rotation  = playerController.transform.GetChild(0).rotation;
        
        StartInteractIkAnimationRpc(playerId);
        
        playerController.TakeDamageRpc(damageOnHit, new Vector3(0, -0.25f, 0.5f));
        
        if (lockMovement)
        {
            playerController.LockMovementRpc(true);
        }
    }
    
    [Rpc(SendTo.Server)]
    public void DeactivateTrapRpc()
    {
        if (!trappedPlayer.isPlayerDead.Value && lockMovement)
        {
            trappedPlayer.LockMovementRpc(false);
        }
        StopInteractIkAnimationRpc(trappedPlayer.localPlayerId);
        //activated.Value = false;
        trappedPlayerId.Value = -1;
    }
    
    public void OnTrappedPlayerIdChanged(int prevValue, int newValue)
    {
        if (trappedPlayerId.Value != -1)
        {
            trappedPlayer = GameSessionManager.Instance.playerControllerList[trappedPlayerId.Value];
        }
        else
        {
            trappedPlayer = null;
        }
    }


    [Rpc(SendTo.Everyone)]
    public void StartInteractIkAnimationRpc(int playerId)
    {
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        foreach (IkAnimation ikAnimation in ikAnimations)
        {
            playerController.playerAnimationController.GetComponent<InteractionSystem>().StartInteraction(ikAnimation.effector, ikAnimation.interactionObject, true);
        }
        
        playerController.zeroGravity = true;
        playerController.grounder.detectionOffset.y = 0f;
        playerController.playerCollider.enabled = false;
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
        playerController.playerAnimationController.turnAnimation = true;
        playerController.playerAnimationController.bodyRotationInterpolationSpeed = 3;
        playerController.zeroGravity = false;
        playerController.grounder.detectionOffset.y = -0.55f;
        playerController.playerCollider.enabled = true;
    }
}
