using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using MyBox;
using Dissonance;
using Dissonance.Integrations.Unity_NFGO;

public class GameSessionManager : NetworkBehaviour
{
	public static GameSessionManager Instance { get; private set; }

    [Foldout("Switches", true)]
	public bool hasHostSpawned;
	
    [Foldout("Values", true)]
	public int connectedClientCount;
	//public int alivePlayerNumber;
    public PlayerController localPlayerController;
    public Dictionary<ulong, int> ClientIdToPlayerIdDictionary = new Dictionary<ulong, int>();
    public List<PlayerController> playerControllerList = new List<PlayerController>();
	private float updatePlayerVoiceInterval;


    [Foldout("References", true)]
	public ItemList itemList;
    public Transform spawnTransform;
    public Transform despawnTransform;
	public AudioListener audioListener;


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
		NetworkObject[] networkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>(includeInactive: true);
		foreach(NetworkObject networkObject in networkObjects)
		{
			networkObject.DontDestroyWithOwner = true;
		}

		StartCoroutine(InitializeVoiceChat());

	}

    private void Update() 
    {
		if (GameNetworkManager.Instance == null)
		{
			return;
		}

		if (base.IsServer && !hasHostSpawned)
		{
			OnHostConnectedGameSession();
			hasHostSpawned = true;
		}
        
    }

	public void LateUpdate()
    {
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}

		if (updatePlayerVoiceInterval > 5f)
		{
			updatePlayerVoiceInterval = 0f;
			UpdatePlayerVoicePlayback();
		}
		else
		{
			updatePlayerVoiceInterval += Time.deltaTime;
		}

    }
	

	#region OnConnection

	public void OnHostConnectedGameSession()
	{
		ClientIdToPlayerIdDictionary.Add(NetworkManager.Singleton.LocalClientId, 0);
		playerControllerList[0].GetComponent<NetworkObject>().ChangeOwnership(NetworkManager.Singleton.LocalClientId);
		playerControllerList[0].GetComponent<PlayerController>().controlledByClient = true;
		connectedClientCount = 1;
		//alivePlayerNumber = connectedClientCount + 1;

		//Teleport player controller to its spawn position.
		playerControllerList[0].TeleportPlayer(spawnTransform.position);

		if (!GameNetworkManager.Instance.steamDisabled)
		{
			GameNetworkManager.Instance.currentSteamLobby.Value.SetJoinable(true);
		}
	}

	public void OnClientConnectedGameSession(ulong clientId)
	{
		if (!base.IsServer)
		{
			return;
		}
		
		try
		{
			Debug.Log($"Connecting new player to host; clientId: {clientId}");

			List<int> existingPlayerIdList = ClientIdToPlayerIdDictionary.Values.ToList();
			int targetPlayerId = 0;

			for (int i = 1; i < GameNetworkManager.Instance.maxPlayerNumber; i++)
			{
				if (!existingPlayerIdList.Contains(i))
				{
					targetPlayerId = i;
					break;
				}
			}

			if (targetPlayerId == 0)
			{
				Debug.Log($"No available player object found, disconnecting");
				GameNetworkManager.Instance.Disconnect();
				return;
			}

            PlayerController playerController = playerControllerList[targetPlayerId];
			playerController.localClientId = clientId;
			playerController.GetComponent<NetworkObject>().ChangeOwnership(clientId);
			Debug.Log($"New player assigned PlayerController: {playerController}");

            //Teleport player controller to its spawn position.
            StartCoroutine(DelayedSpawnTeleport(playerController));

			List<ulong> connectedClientIdList = new List<ulong>();
			for (int j = 0; j < playerControllerList.Count; j++)
			{
				NetworkObject component = playerControllerList[j].GetComponent<NetworkObject>();
				if (!component.IsOwnedByServer)
				{
					connectedClientIdList.Add(component.OwnerClientId);
				}
				else if (j == 0)
				{
					connectedClientIdList.Add(NetworkManager.Singleton.LocalClientId);
				}
			}

			OnClientConnectedGameSessionClientRpc(clientId, connectedClientCount, connectedClientIdList.ToArray(), targetPlayerId);

			ClientIdToPlayerIdDictionary.Add(clientId, targetPlayerId);
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error occured in OnClientConnected! Shutting server down. clientId: {clientId}. Error: {arg}");
			GameNetworkManager.Instance.Disconnect();
		}

    }

	[ClientRpc]
	private void OnClientConnectedGameSessionClientRpc(ulong clientId, int connectedPlayerNumber, ulong[] connectedClientIds, int targetPlayerId)
	{
		NetworkManager networkManager = base.NetworkManager;

		if ((object)networkManager == null || !networkManager.IsListening)
		{
			return;
		}

		try
		{
			Debug.Log($"NEW CLIENT JOINED THE SERVER!!; clientId: {clientId}");

			if (NetworkManager.Singleton == null)
			{
				return;
			}

			if (clientId == NetworkManager.Singleton.LocalClientId && GameNetworkManager.Instance.localClientJoinRequestPending)
			{
				GameNetworkManager.Instance.localClientJoinRequestPending = false;
			}

			//Reconstructing ClientIdToPlayerIdDictionary on all clients
			if (!base.IsServer)
			{
				ClientIdToPlayerIdDictionary.Clear();
				for (int i = 0; i < connectedClientIds.Length; i++)
				{
					if (connectedClientIds[i] == 999)
					{
						Debug.Log($"Skipping at index {i}");
						continue;
					}
					ClientIdToPlayerIdDictionary.Add(connectedClientIds[i], i);
					Debug.Log($"adding value to ClientPlayerList at value of index {i}: {connectedClientIds[i]}");
				}
				if (!ClientIdToPlayerIdDictionary.ContainsKey(clientId))
				{
					Debug.Log($"Successfully added new client id {clientId} and connected to object {targetPlayerId}");
					ClientIdToPlayerIdDictionary.Add(clientId, targetPlayerId);
				}
				else
				{
					Debug.Log("ClientId already in ClientPlayerList!");
				}
				Debug.Log($"clientplayerlist count for client: {ClientIdToPlayerIdDictionary.Count}");
			
			}

			connectedClientCount = connectedPlayerNumber + 1;
			Debug.Log("New player: " + playerControllerList[targetPlayerId].name);

			PlayerController playerController = playerControllerList[targetPlayerId];
			playerController.localClientId = clientId;

            for (int j = 0; j < this.connectedClientCount; j++)
			{
				if (j == 0 || !playerControllerList[j].IsOwnedByServer)
				{
                    playerControllerList[j].controlledByClient = true;
				}
			}
			playerController.controlledByClient = true;
            //alivePlayerNumber = this.connectedClientCount + 1;
            Debug.Log($"Connected players after connection: {this.connectedClientCount}");

			//Do stuff if I am the client who just join
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
            }
			else
			{
			}
			
			//Reinitialize NfgoPlayer Component Tracking for every PlayerController on all clients
			foreach (PlayerController playerController1 in playerControllerList)
			{
				if (playerController1.GetComponent<NfgoPlayer>() && !playerController.GetComponent<NfgoPlayer>().IsTracking)
				{
					playerController1.GetComponent<NfgoPlayer>().InitializeVoiceChatTracking();
				}
			}
        }
        catch (Exception arg)
		{
			Debug.LogError($"Failed to assign new player with client id #{clientId}: {arg}");
			GameNetworkManager.Instance.Disconnect();
		}
	}

	#endregion

	#region OnDisconnection

	public void OnClientDisconnectedGameSession(ulong clientId)
	{
        Debug.Log($"Disconnecting Client #{clientId}");

		if (ClientIdToPlayerIdDictionary == null || !ClientIdToPlayerIdDictionary.ContainsKey(clientId))
		{
			return;
		}

		if (NetworkManager.Singleton == null || GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			GameNetworkManager.Instance.Disconnect();
			return;
        }

		//Print all players
        for (int i = 0; i < ClientIdToPlayerIdDictionary.Count; i++)
        {
            ClientIdToPlayerIdDictionary.TryGetValue((ulong)i, out var value);
            Debug.Log($"client id: {i} ; player object id: {value}");
        }
		
        if (!ClientIdToPlayerIdDictionary.TryGetValue(clientId, out var playerId))
		{
			Debug.LogError("Could not get player object number from client id on disconnect!");
			return;
		}

		if (base.IsServer)
        {
            OnClientDisconnectedGameSessionClientRpc(clientId, playerId);
		}
    }

    [ClientRpc]
	public void OnClientDisconnectedGameSessionClientRpc(ulong clientId, int playerId)
	{
        if (!ClientIdToPlayerIdDictionary.ContainsKey(clientId))
        {
            Debug.Log("OnClientDisconnectClientRpc: Target clientId key already removed, ignoring");
            return;
        }
        if (GameNetworkManager.Instance.localPlayerController != null && clientId == GameNetworkManager.Instance.localPlayerController.localPlayerId)
        {
            Debug.Log("OnClientDisconnectClientRpc: Local client disconnecting, ignoring");
            return;
        }
        if (base.NetworkManager.ShutdownInProgress || NetworkManager.Singleton == null)
        {
            Debug.Log("OnClientDisconnectClientRpc: Shutdown in progress, returning");
            return;
        }

        //Update alivePlayerNumber
        ClientIdToPlayerIdDictionary.Remove(clientId);
        connectedClientCount--;

		//Reset PlayerController values
        PlayerController playerController = playerControllerList[playerId].GetComponent<PlayerController>();
        try
        {
			playerController.GetComponent<NfgoPlayer>().StopTracking();
            playerController.controlledByClient = false;
			//Drop all inventory items
			//playerController.TeleportPlayer(despawnTransform.position);
            StartCoroutine(DelayedDespawnTeleport(playerController));
			Destroy(playerController.currentVoiceChatIngameSettings.gameObject);
            if (!NetworkManager.Singleton.ShutdownInProgress && base.IsServer)
            {
                playerController.gameObject.GetComponent<NetworkObject>().RemoveOwnership();
            }
            Debug.Log($"Current players after dc: {connectedClientCount}");
        }
        catch (Exception arg)
        {
            Debug.LogError($"Error while handling player disconnect!: {arg}");
        }
    }

	#endregion

    private IEnumerator DelayedSpawnTeleport(PlayerController playerController)
    {
        yield return null;
        yield return null;
        playerController.TeleportPlayer(spawnTransform.position);
    }

    private IEnumerator DelayedDespawnTeleport(PlayerController playerController)
    {
        yield return null;
        yield return null;
        playerController.TeleportPlayer(despawnTransform.position);
    }

	#region Voice Chat

	private IEnumerator InitializeVoiceChat()
	{
		yield return new WaitUntil(() =>  localPlayerController != null && localPlayerController.controlledByClient);

		foreach (PlayerController playerController in playerControllerList)
		{
			if (playerController.GetComponent<NfgoPlayer>() && !playerController.GetComponent<NfgoPlayer>().IsTracking)
			{
				playerController.GetComponent<NfgoPlayer>().InitializeVoiceChatTracking();
			}
		}
		
		UpdatePlayerVoicePlayback();

	}

	public void UpdatePlayerVoicePlayback()
	{
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		
		//PlayerController playerControllerB = ((!GameNetworkManager.Instance.localPlayerController.isPlayerDead || !(GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)) ? GameNetworkManager.Instance.localPlayerController : GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript);
		foreach (PlayerController playerController in playerControllerList)
		{
			//if ((!playerController.isPlayerControlled && !playerController.isPlayerDead) || playerController == GameNetworkManager.Instance.localPlayerController)
			if (!playerController.controlledByClient || playerController == GameNetworkManager.Instance.localPlayerController)
			{
				continue;
			}

			if (playerController.voicePlayerState == null || playerController.currentVoiceChatIngameSettings._playerState == null || playerController.currentVoiceChatAudioSource == null)
			{
				RefreshPlayerVoicePlaybackObjects();
				if (playerController.voicePlayerState == null || playerController.currentVoiceChatAudioSource == null)
				{
					Debug.Log($"Unable to access voice chat object for {playerController.name}");
					continue;
				}
			}
			AudioSource currentVoiceChatAudioSource = playerController.currentVoiceChatAudioSource;

			// if (playerController.isPlayerDead)
			// {
			// 	currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().enabled = false;
			// 	currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>().enabled = false;
			// 	currentVoiceChatAudioSource.panStereo = 0f;
			// 	SoundManager.Instance.playerVoicePitchTargets[playerController.playerClientId] = 1f;
			// 	SoundManager.Instance.SetPlayerPitch(1f, (int)playerController.playerClientId);
			// 	if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			// 	{
			// 		currentVoiceChatAudioSource.spatialBlend = 0f;
			// 		playerController.currentVoiceChatIngameSettings.set2D = true;
			// 		playerController.voicePlayerState.Volume = 1f;
			// 	}
			// 	else
			// 	{
			// 		currentVoiceChatAudioSource.spatialBlend = 1f;
			// 		playerController.currentVoiceChatIngameSettings.set2D = false;
			// 		playerController.voicePlayerState.Volume = 0f;
			// 	}
			// 	continue;
			// }

			bool flag = false;//playerController.speakingToWalkieTalkie && playerControllerB.holdingWalkieTalkie && playerController != playerControllerB;
			AudioLowPassFilter component = currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>();
			OccludeAudio component2 = currentVoiceChatAudioSource.GetComponent<OccludeAudio>();
			component.enabled = true;
			component2.overridingLowPass = flag;// || playerController.voiceMuffledByEnemy;
			currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>().enabled = flag;
			if (!flag)
			{
				currentVoiceChatAudioSource.spatialBlend = 1f;
				playerController.currentVoiceChatIngameSettings.set2D = false;
				currentVoiceChatAudioSource.bypassListenerEffects = false;
				currentVoiceChatAudioSource.bypassEffects = false;
				//currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerController.playerClientId];
				component.lowpassResonanceQ = 1f;
			}
			// else
			// {
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
			// 	component.lowpassResonanceQ = 3f;
			// }

			// if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			// {
			// 	playerController.voicePlayerState.Volume = 0.8f;
			// }
			// else
			// {
			// 	playerController.voicePlayerState.Volume = 1f;
			// }
		}
	}

	public void RefreshPlayerVoicePlaybackObjects()
	{
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}

		PlayerVoiceIngameSettings[] array = UnityEngine.Object.FindObjectsOfType<PlayerVoiceIngameSettings>(includeInactive: true);
		Debug.Log($"Refreshing voice playback objects. Number of voice objects found: {array.Length}");

		foreach (PlayerController playerController in playerControllerList)
		{
			if (!playerController.controlledByClient && !playerController.isPlayerDead)
			{
				Debug.Log($"Skipping {playerController.name} as they are not controlled or dead");
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
				
				Debug.Log(array[j]._playerState.Name + ", " + playerController.gameObject.GetComponentInChildren<NfgoPlayer>().PlayerId);
				if (array[j]._playerState.Name == playerController.gameObject.GetComponentInChildren<NfgoPlayer>().PlayerId)
				{
					Debug.Log($"Found a match for voice object #{j} and player object {playerController.name}");
					playerController.voicePlayerState = array[j]._playerState;
					playerController.currentVoiceChatAudioSource = array[j].voiceAudio;
					playerController.currentVoiceChatIngameSettings = array[j];
					//playerController.currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerController.playerClientId];
				}
			}
		}
	}

	#endregion

}