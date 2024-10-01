using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dissonance.Integrations.Unity_NFGO;
using UnityEngine.Serialization;

public class VoiceChatManager : MonoBehaviour
{
    public static VoiceChatManager Instance { get; private set; }
	
    [Header("Values")]
    [SerializeField]
    private float updatePlayerVoiceCooldown;
    
    [Header("Settings")]
    public float updatePlayerVoiceCooldownSetting = 5f;
    
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
    
    private void Start()
    {
        StartCoroutine(InitializeVoiceChat());
    }

    private IEnumerator InitializeVoiceChat()
    {
        yield return new WaitUntil(() =>  GameSessionManager.Instance.localPlayerController != null && GameSessionManager.Instance.localPlayerController.controlledByClient.Value);

        foreach (PlayerController playerController in GameSessionManager.Instance.playerControllerList)
        {
            if (playerController.GetComponent<NfgoPlayer>())// && !playerController.GetComponent<NfgoPlayer>().IsTracking)
            {
                playerController.GetComponent<NfgoPlayer>().InitializeVoiceChatTracking();
            }
        }
		
        UpdateVoiceChat();
    }

    public void LateUpdate()
    {
        if (updatePlayerVoiceCooldown > 0f)
        {
	        updatePlayerVoiceCooldown -= Time.deltaTime;
        }
        else
        {
	        UpdateVoiceChat();
	        updatePlayerVoiceCooldown = updatePlayerVoiceCooldownSetting;
        }
    }
    
	public void UpdateVoiceChat()
	{
		if (GameNetworkManager.Instance == null || GameSessionManager.Instance.localPlayerController == null)
		{
			return;
		}
		
		//PlayerController playerControllerListener = !localPlayerController.isPlayerDead ? localPlayerController : localPlayerController.spectatedPlayerController;
		
		foreach (PlayerController playerController in GameSessionManager.Instance.playerControllerList)
		{
			if (!playerController.controlledByClient.Value || playerController == GameSessionManager.Instance.localPlayerController)
			{
				continue;
			}

			if (playerController.voicePlayerState == null || playerController.playerVoiceChatPlaybackObject._playerState == null || playerController.playerVoiceChatAudioSource == null)
			{
				RefreshPlayerVoicePlaybackObjects();
				if (playerController.voicePlayerState == null || playerController.playerVoiceChatAudioSource == null)
				{
					Debug.Log($"Unable to access voice chat object for {playerController.name}");
					continue;
				}
			}
			
			AudioSource currentVoiceChatAudioSource = playerController.playerVoiceChatAudioSource;
			AudioLowPassFilter lowPassFilter = currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>();
			AudioHighPassFilter highPassFilter = currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>();
			AudioOccluder audioOccluder = currentVoiceChatAudioSource.GetComponent<AudioOccluder>();
			
			if (playerController.isPlayerDead.Value)
			{ 
				lowPassFilter.enabled = false; 
				highPassFilter.enabled = false;
				currentVoiceChatAudioSource.panStereo = 0f;
				if (GameSessionManager.Instance.localPlayerController.isPlayerDead.Value)
				{
			 		currentVoiceChatAudioSource.spatialBlend = 0f;
			 		playerController.playerVoiceChatPlaybackObject.set2D = true;
			 		playerController.voicePlayerState.Volume = 1f;
				}
				else
				{
			 		currentVoiceChatAudioSource.spatialBlend = 1f;
			 		playerController.playerVoiceChatPlaybackObject.set2D = false;
			 		playerController.voicePlayerState.Volume = 0f;
				}
				continue;
			}

			bool flag = false;//playerController.speakingToWalkieTalkie && playerControllerB.holdingWalkieTalkie && playerController != playerControllerB;
			lowPassFilter.enabled = true;
			if (!flag)
			{
				highPassFilter.enabled = false;
				audioOccluder.overridingLowPass = false;// || playerController.voiceMuffledByEnemy;
				currentVoiceChatAudioSource.spatialBlend = 1f;
				playerController.playerVoiceChatPlaybackObject.set2D = false;
				currentVoiceChatAudioSource.bypassListenerEffects = false;
				currentVoiceChatAudioSource.bypassEffects = false;
				//currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerController.playerClientId];
				lowPassFilter.lowpassResonanceQ = 1f;
			}
			// else
			// {
			//	highPassFilter.enabled = true;
			//	occludeAudio.overridingLowPass = true;// || playerController.voiceMuffledByEnemy;
			// 	currentVoiceChatAudioSource.spatialBlend = 0f;
			// 	playerController.currentVoiceChatIngameSettings.set2D = true;
			// 	if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			// 	{
			// 		currentVoiceChatAudioSource.panStereo = 0f;
			// 		currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerController.playerClientId];
			// 		currentVoiceChatAudioSource.bypassListenerEffects = false;
			// 		currentVoiceChatAudioSource.bypassEffects = false;
			// 	}
			// 	else
			// 	{
			// 		currentVoiceChatAudioSource.panStereo = 0.4f;
			// 		currentVoiceChatAudioSource.bypassListenerEffects = false;
			// 		currentVoiceChatAudioSource.bypassEffects = false;
			// 		currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerController.playerClientId];
			// 	}
			// 	component2.lowPassOverride = 4000f;
			// 	lowPassFilter.lowpassResonanceQ = 3f;
			// }

			 if (GameSessionManager.Instance.localPlayerController.isPlayerDead.Value)
			 {
			 	playerController.voicePlayerState.Volume = 0.8f;
			 }
			 else
			 {
			 	playerController.voicePlayerState.Volume = 1f;
			 }
		}
	}
	
	public void RefreshPlayerVoicePlaybackObjects()
	{
		if (GameNetworkManager.Instance == null || GameSessionManager.Instance.localPlayerController == null)
		{
			return;
		}

		PlayerVoicePlaybackObject[] array = UnityEngine.Object.FindObjectsOfType<PlayerVoicePlaybackObject>(includeInactive: true);
		Debug.Log($"Refreshing voice playback objects. Number of voice objects found: {array.Length}");

		foreach (PlayerController playerController in GameSessionManager.Instance.playerControllerList)
		{
			if (!playerController.controlledByClient.Value && !playerController.isPlayerDead.Value)
			{
				continue;
			}

			for (int j = 0; j < array.Length; j++)
			{
				if (array[j]._playerState == null)
				{
					array[j].FindPlayerIfNull();
					if (array[j]._playerState == null)
					{
						Debug.LogError($"Unable to connect {playerController.name} to voice");
						return;
					}
				}

				if (!array[j].isActiveAndEnabled)
				{
					Debug.LogError($"Unable to connect {playerController.name} to voice");
					return;
				}
				
				Debug.Log($"Comparing Voice object #{j}: {array[j]._playerState.Name} to {playerController.name}: {playerController.gameObject.GetComponentInChildren<NfgoPlayer>().PlayerId}");
				if (array[j]._playerState.Name == playerController.gameObject.GetComponentInChildren<NfgoPlayer>().PlayerId)
				{
					Debug.Log($"Found a match for voice object #{j} and player object {playerController.name}");
					playerController.voicePlayerState = array[j]._playerState;
					playerController.playerVoiceChatAudioSource = array[j].voiceAudio;
					playerController.playerVoiceChatPlaybackObject = array[j];
					//playerController.currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerController.playerClientId];
				}
			}
		}
	}

}
