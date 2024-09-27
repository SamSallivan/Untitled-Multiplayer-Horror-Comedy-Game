using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectateManager : MonoBehaviour
{
    public static SpectateManager Instance { get; private set; }
    
    [Header("References")]
    public Transform SpectateTargetTransform;
    //public Transform SpectateCameraTransform;
    public PlayerController spectateTargetPlayerController;
    
    [Header("Values")]
    public bool isSpectating = false;
    
    [Header("Settings")]
    public float CameraPositionInterpolationSpeed = 10f;
    public LayerMask cameraOcclusionMask;
    public float cameraRadius = 1f;
    public float minCameraDistance = 0.5f;
    public float maxCameraDistance = 3f;
    public float onDeathZoomOutTime;
    public float onDeathZoomOutTimeSetting = 0.75f;
    
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

        if (onDeathZoomOutTime < onDeathZoomOutTimeSetting * 4f)
        {
            onDeathZoomOutTime += Time.deltaTime;
        }
        else
        {
            if (spectateTargetPlayerController.isPlayerDead.Value || spectateTargetPlayerController.isPlayerExtracted.Value || !spectateTargetPlayerController.controlledByClient)
            {
                SpectateNextPlayer();
            }
        }
        
        UpdatePivot();
        UpdateCamera();
    }

    public void StartSpectating()
    {
        isSpectating = true;
        spectateTargetPlayerController = GameSessionManager.Instance.localPlayerController;
        Camera.main.transform.parent = SpectateTargetTransform;
        Camera.main.transform.localRotation = Quaternion.Euler(0,0,0);
        SpectateTargetTransform.position = GameSessionManager.Instance.localPlayerController.transform.position;
        onDeathZoomOutTime = 0;
    }

    public void StopSpectating()
    {
        isSpectating = false;
        spectateTargetPlayerController = null;
        if (SpectateTargetTransform.childCount > 0)
        {
            Camera.main.transform.parent = GameSessionManager.Instance.localPlayerController.cameraBob.transform;
            Camera.main.transform.localPosition = Vector3.zero;
            Camera.main.transform.localRotation = Quaternion.identity;
        }
    }
    
    public void SpectateNextPlayer()
    {
        if (onDeathZoomOutTime < onDeathZoomOutTimeSetting * 4f)
        {
            return;
        }
        
        int targetPlayerId = spectateTargetPlayerController ? spectateTargetPlayerController.localPlayerId : 0;
        
        for (int i = 0; i < GameNetworkManager.Instance.maxPlayerNumber; i++)
        {
            targetPlayerId = (targetPlayerId + 1) % 4;
            PlayerController targetPlayerController = GameSessionManager.Instance.playerControllerList[targetPlayerId];
            
            if (targetPlayerController.controlledByClient && !targetPlayerController.isPlayerDead.Value && !targetPlayerController.isPlayerExtracted.Value && targetPlayerController != GameSessionManager.Instance.localPlayerController)
            {
                spectateTargetPlayerController = targetPlayerController;
                return;
            }
        }
        
        spectateTargetPlayerController = GameSessionManager.Instance.localPlayerController;
    }

    private void UpdatePivot()
    {
        SpectateTargetTransform.position = Vector3.Lerp(SpectateTargetTransform.position, spectateTargetPlayerController.transform.position, Time.deltaTime * CameraPositionInterpolationSpeed);
        SpectateTargetTransform.rotation = GameSessionManager.Instance.localPlayerController.headPosition.transform.rotation;
    }
    
    private void UpdateCamera()
    {
        float maxDistance = Mathf.Lerp(0, maxCameraDistance, Mathf.Clamp(onDeathZoomOutTime / onDeathZoomOutTimeSetting, 0f, 1f));
        
        Ray ray = new Ray(SpectateTargetTransform.position, -SpectateTargetTransform.GetChild(0).forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, cameraOcclusionMask, QueryTriggerInteraction.Ignore))
        {
            SpectateTargetTransform.transform.GetChild(0).position = ray.GetPoint(Mathf.Clamp(hit.distance, minCameraDistance, maxDistance) - cameraRadius);
        }
        else
        {
            SpectateTargetTransform.transform.GetChild(0).position = ray.GetPoint(maxDistance - cameraRadius);
        }
        
        SpectateTargetTransform.transform.GetChild(0).transform.LookAt(SpectateTargetTransform);
        Camera.main.transform.localRotation = Quaternion.Euler(0,0,0);
    }
}
