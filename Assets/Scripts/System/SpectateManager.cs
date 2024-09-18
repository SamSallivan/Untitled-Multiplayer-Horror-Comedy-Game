using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectateManager : MonoBehaviour
{
    public static SpectateManager Instance { get; private set; }
    
    [Header("References")]
    public Transform SpectateTargetTransform;
    public Transform SpectateCameraTransform;
    public PlayerController spectateTargetPlayerController;
    public MouseLook mouseLookX;
    public MouseLook mouseLookY;
    
    [Header("Values")]
    public bool isSpectating = false;
    
    [Header("Settings")]
    public float CameraPositionInterpolationSpeed = 10f;
    public LayerMask cameraOcclusionMask;
    public float minCameraDistance = 0.5f;
    public float maxCameraDistance = 3f;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            UnityEngine.Object.Destroy(Instance.gameObject);
            Instance = this;
        }
    }

    private void Update()
    {
        if (!isSpectating || !spectateTargetPlayerController)
        {
            return;
        }
        
        if (spectateTargetPlayerController.isPlayerDead)
        {
            SpectateNextPlayer();
        }
        
        SpectateTargetTransform.position = Vector3.Lerp(SpectateTargetTransform.position, spectateTargetPlayerController.transform.position, Time.deltaTime * CameraPositionInterpolationSpeed);
        UpdateCameraRotation();
        UpdateCameraPosition();
    }

    public void StartSpectating()
    {
        SpectateTargetTransform.position = GameSessionManager.Instance.localPlayerController.transform.position;
        
        isSpectating = true;
        SpectateNextPlayer();
        GameSessionManager.Instance.localPlayerController.headPosition.targetPositionTransform = SpectateCameraTransform;
    }

    public void StopSpectating()
    {
        isSpectating = false;
        spectateTargetPlayerController = null;
        GameSessionManager.Instance.localPlayerController.headPosition.targetPositionTransform = GameSessionManager.Instance.localPlayerController.headPosition.targetPositionTransformDefault;
        GameSessionManager.Instance.localPlayerController.headPosition.transform.localRotation = Quaternion.identity;
    }
    
    public void SpectateNextPlayer()
    {
        int targetPlayerId = spectateTargetPlayerController ? spectateTargetPlayerController.localPlayerId : 0;
        
        for (int i = 0; i < GameNetworkManager.Instance.maxPlayerNumber; i++)
        {
            targetPlayerId = (targetPlayerId + 1) % 4;
            PlayerController targetPlayerController = GameSessionManager.Instance.playerControllerList[targetPlayerId];
            
            if (targetPlayerController.controlledByClient && !targetPlayerController.isPlayerDead && targetPlayerController != GameSessionManager.Instance.localPlayerController)
            {
                spectateTargetPlayerController = targetPlayerController;
                return;
            }
        }
        
        spectateTargetPlayerController = GameSessionManager.Instance.localPlayerController;
    }

    private void UpdateCameraRotation()
    {
        Vector2 mouseInput = GameSessionManager.Instance.localPlayerController.playerInputActions.FindAction("Look").ReadValue<Vector2>();
        mouseLookX.UpdateCameraRotation(mouseInput.x);
        mouseLookY.UpdateCameraRotation(mouseInput.y);
    }
    
    private void UpdateCameraPosition()
    {
        Ray ray = new Ray(SpectateTargetTransform.position, -SpectateTargetTransform.GetChild(0).forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 1.4f, cameraOcclusionMask, QueryTriggerInteraction.Ignore))
        {
            SpectateCameraTransform.position = ray.GetPoint(Mathf.Clamp(hit.distance - 0.25f, minCameraDistance, maxCameraDistance));
        }
        else
        {
            SpectateCameraTransform.transform.position = ray.GetPoint(maxCameraDistance);
        }
        SpectateCameraTransform.transform.LookAt(SpectateTargetTransform);
        GameSessionManager.Instance.localPlayerController.headPosition.transform.rotation = SpectateCameraTransform.rotation;
    }
}
