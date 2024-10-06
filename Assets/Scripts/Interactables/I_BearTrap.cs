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
    
    public float damageOnHit = 25f;
    public float damageOnTick = 25f;
    public float tickInterval = 1f;
    public float tickCooldown = 0;
    public bool lockMovement = false;
    
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
                    targetPosition.y = trappedPlayer.transform.position.y;
                    trappedPlayer.rb.position = Vector3.Lerp(trappedPlayer.rb.position, targetPosition, Time.deltaTime * trappedPlayerPositionInterpolationSpeed);
                }

                if (tickCooldown > 0)
                {
                    tickCooldown -= Time.deltaTime;
                }
                else
                {
                    tickCooldown = tickInterval;
                    trappedPlayer.TakeDamage(damageOnTick, Vector3.zero);
                }
            }
            else
            {
                DeactivateTrapRpc();
            }
        }

        if (trappedPlayer)
        {
            
        }
        else
        {
            
        }

        if (isHeld)
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
        if (trappedPlayer && trappedPlayer != GameSessionManager.Instance.localPlayerController)
        {
            return true;
        }
        
        return false;
    }
    
    [Rpc(SendTo.Server)]
    public void ActivateTrapRpc(int playerId)
    {
        if (activated.Value)
        {
            return;
        }
        
        activated.Value = true;
        trappedPlayerId.Value = playerId;
        StartInteractIkAnimationRpc(playerId);
        
        PlayerController playerController = GameSessionManager.Instance.playerControllerList[playerId];
        playerController.TakeDamageRpc(damageOnHit, Vector3.zero);
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
        activated.Value = false;
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
    }
}
