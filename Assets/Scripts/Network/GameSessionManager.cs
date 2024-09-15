using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using Dissonance.Integrations.Unity_NFGO;
using Sirenix.OdinInspector;

public class GameSessionManager : NetworkBehaviour
{
	public static GameSessionManager Instance { get; private set; }
	
    [FoldoutGroup("References")]
    public List<PlayerController> playerControllerList = new List<PlayerController>();
	
    [FoldoutGroup("References")]
	[ReadOnly]
    public PlayerController localPlayerController;

    [FoldoutGroup("References")]
	public ItemList itemList;
	
    [FoldoutGroup("References")]
    public Transform spawnTransform;
	
    [FoldoutGroup("References")]
    public Transform despawnTransform;
	
    [FoldoutGroup("References")]
	[ReadOnly]
	public AudioListener audioListener;

    [FoldoutGroup("Values")]
	[ReadOnly]
	public bool hasHostSpawned;
	
    [FoldoutGroup("Values")]
	[ReadOnly]
	public int connectedPlayerCount;

    // [FoldoutGroup("Values")]
	// [ReadOnly]
	//public int alivePlayerNumber;
	
    [FoldoutGroup("Values")]
	private float updatePlayerVoiceInterval;
	
    [FoldoutGroup("Values")]
	[ReadOnly]
    public Dictionary<ulong, int> ClientIdToPlayerIdDictionary = new Dictionary<ulong, int>();


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
		NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>(includeInactive: true);
		foreach(NetworkObject networkObject in networkObjects)
		{
			networkObject.DontDestroyWithOwner = true;
		}

		StartCoroutine(InitializeVoiceChat());

	}

    private void Update() 
    {
		if (base.IsServer && !hasHostSpawned)
		{
			OnHostConnectedGameSession();
			hasHostSpawned = true;
		}

    }

	public void LateUpdate()
    {
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
	
	#region Save&Load

	public class InventoryItemSaveData
	{
		public int index = -1;
		public int amount;
	}

	[Button("Save")]
	public void Save()
	{
		List<InventoryItemSaveData> list = new List<InventoryItemSaveData>();
		for(int i = 0; i < InventoryManager.instance.storageSlotList.Count; i++)
		{
			list.Add(new InventoryItemSaveData());
			if(InventoryManager.instance.storageSlotList[i].inventoryItem)
			{
				
				for(int j = 0; j < itemList.itemDataList.Count; j++)
				{
					if(InventoryManager.instance.storageSlotList[i].inventoryItem.itemData == itemList.itemDataList[j])
					{
						list[i].index = j;
						list[i].amount = InventoryManager.instance.storageSlotList[i].inventoryItem.itemStatus.amount;
						break;
					}
				}
			}
		}
		try
		{
			ES3.Save("StorageSlotSaveData", list.ToArray());
		}
		catch (Exception arg)
		{
		}
	}

	[Button("Load")]
	public void Load()
	{
		InventoryItemSaveData[] list = ES3.Load<InventoryItemSaveData[]>("StorageSlotSaveData");

		for(int i = 0; i < list.Length; i++)
		{
			if(list[i].index != -1)
			{
				InventoryManager.instance.InstantiatePocketedItemServerRpc(list[i].index, list[i].amount, i, NetworkManager.Singleton.LocalClientId);
			}
		}
	}

	public IEnumerator LoadCoroutine()
	{
		yield return new WaitUntil(() =>  localPlayerController != null && localPlayerController.controlledByClient);

		Load();
	}

	public int GetItemIndex(ItemData itemData)
	{
		for(int i = 0; i < itemList.itemDataList.Count; i++)
		{
			if(itemData == itemList.itemDataList[i])
			{
				return i;
			}
		}
		return -1;
	}

	#endregion Save&Load

	#region OnConnection

	public void OnHostConnectedGameSession()
	{
		try
		{
			ClientIdToPlayerIdDictionary.Add(NetworkManager.Singleton.LocalClientId, 0);
			playerControllerList[0].GetComponent<NetworkObject>().ChangeOwnership(NetworkManager.Singleton.LocalClientId);
			playerControllerList[0].GetComponent<PlayerController>().controlledByClient = true;
			connectedPlayerCount = 1;
			//alivePlayerNumber = connectedClientCount + 1;
			StartCoroutine(LoadCoroutine());

			//Teleport player controller to its spawn position.
			playerControllerList[0].TeleportPlayer(spawnTransform.position);

			if (!GameNetworkManager.Instance.steamDisabled)
			{
				GameNetworkManager.Instance.currentSteamLobby.Value.SetJoinable(true);
			}
		}
		catch (Exception arg)
		{
			Debug.LogError($"OnHostConnectedGameSession: Error: {arg}. Shutting server down.");
			GameNetworkManager.Instance.Disconnect();
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
			Debug.Log($"OnClientConnectedGameSession: Connecting clientId: {clientId}");

			List<int> connectedPlayerIdList = ClientIdToPlayerIdDictionary.Values.ToList();
			List<ulong> connectedClientIdList = ClientIdToPlayerIdDictionary.Keys.ToList();
			int targetPlayerId = 0;

			for (int i = 1; i < GameNetworkManager.Instance.maxPlayerNumber; i++)
			{
				if (!connectedPlayerIdList.Contains(i))
				{
					targetPlayerId = i;
					break;
				}
			}

			if (targetPlayerId == 0)
			{
				Debug.Log($"OnClientConnectedGameSession: No available Player Controller found, disconnecting");
				GameNetworkManager.Instance.Disconnect();
				return;
			}

            PlayerController playerController = playerControllerList[targetPlayerId];
			playerController.localClientId = clientId;
			playerController.GetComponent<NetworkObject>().ChangeOwnership(clientId);
			playerController.controlledByClient = true;
			ClientIdToPlayerIdDictionary.Add(clientId, targetPlayerId);
			connectedPlayerCount = ClientIdToPlayerIdDictionary.Count;
			OnClientConnectedGameSessionClientRpc(clientId, targetPlayerId, ClientIdToPlayerIdDictionary.Keys.ToArray(), ClientIdToPlayerIdDictionary.Values.ToArray());

			Debug.Log($"OnClientConnectedGameSession: New client {clientId} assigned PlayerController: {playerController}");

		}
		catch (Exception arg)
		{
			Debug.LogError($"OnClientConnectedGameSession: Error: {arg}. Shutting server down. clientId: {clientId}.");
			GameNetworkManager.Instance.Disconnect();
		}

    }

	[ClientRpc]
	private void OnClientConnectedGameSessionClientRpc(ulong clientId, int targetPlayerId, ulong[] connectedClientIds, int[] connectedPlayerIds)
	{
		if (NetworkManager == null || !NetworkManager.IsListening)
		{
			return;
		}

		try
		{
			Debug.Log($"OnClientConnectedGameSessionClientRpc: Connecting clientId: {clientId}");

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
					ClientIdToPlayerIdDictionary.Add(connectedClientIds[i], connectedPlayerIds[i]);
				}
			}
			connectedPlayerCount = ClientIdToPlayerIdDictionary.Count;
            //alivePlayerNumber = this.connectedClientCount + 1;

			PlayerController playerController = playerControllerList[targetPlayerId];
			playerController.localClientId = clientId;

            for (int j = 0; j < this.connectedPlayerCount; j++)
			{
				if (j == 0 || !playerControllerList[j].IsOwnedByServer)
				{
                    playerControllerList[j].controlledByClient = true;
				}
			}
			
            Debug.Log($"OnClientConnectedGameSessionClientRpc: Connected player number: {connectedPlayerCount}");
			
			//Reinitialize NfgoPlayer Component Tracking for every PlayerController on all clients
			foreach (PlayerController playerController1 in playerControllerList)
			{
				if (playerController1.GetComponent<NfgoPlayer>() && !playerController.GetComponent<NfgoPlayer>().IsTracking)
				{
					playerController1.GetComponent<NfgoPlayer>().InitializeVoiceChatTracking();
				}
			}

			//Do stuff if I am the client who just joined
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
				StartCoroutine(LoadCoroutine());
				//Teleport player controller to its spawn position.
				playerController.TeleportPlayer(spawnTransform.position);
            }
            else
            {
	            if (localPlayerController.currentEquippedItem != null)
	            {
		            InventoryManager.instance.EquipItemServerRpc(
			            localPlayerController.currentEquippedItem.NetworkObject, localPlayerController.NetworkObject);
	            }
            }

		}
        catch (Exception arg)
		{
			Debug.LogError($"OnClientConnectedGameSessionClientRpc: Failed to assign new player with client id #{clientId}: {arg}");
			GameNetworkManager.Instance.Disconnect();
		}
	}

	#endregion

	#region OnDisconnection

	public void OnClientDisconnectedGameSession(ulong clientId)
	{
        Debug.Log($"OnClientDisconnectedGameSession: Disconnecting Client #{clientId}");

		if (ClientIdToPlayerIdDictionary == null || !ClientIdToPlayerIdDictionary.ContainsKey(clientId))
		{
			return;
		}

		// if (NetworkManager.Singleton == null || GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		// {
		// 	GameNetworkManager.Instance.Disconnect("Host Disconnected.");
		// 	return;
        // }
		
		if (clientId == NetworkManager.Singleton.LocalClientId)
		{
			Debug.Log("NetworkManager_OnClientDisconnectCallback: Local client disconnected, returning to main menu.");
			return;
		}

        if (!ClientIdToPlayerIdDictionary.TryGetValue(clientId, out var disconnectingPlayerId))
		{
			Debug.LogError("Could not get player object number from client id on disconnect!");
			return;
		}

		if (!IsServer && disconnectingPlayerId == 0)
        {
			if (!GameNetworkManager.Instance.disconnecting)
			{
				Debug.Log("OnClientDisconnectedGameSession: Host disconnected. Returning to Main Menu.");
				GameNetworkManager.Instance.Disconnect("Host Disconnected.");
				return;
			}
		}

		if (IsServer)
        {
            OnClientDisconnectedGameSessionClientRpc(clientId, disconnectingPlayerId);
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
        if (localPlayerController != null && clientId == localPlayerController.localPlayerId)
        {
            Debug.Log("OnClientDisconnectClientRpc: Local client disconnecting, ignoring");
            return;
        }
        if (NetworkManager.ShutdownInProgress || NetworkManager.Singleton == null)
        {
            Debug.Log("OnClientDisconnectClientRpc: Shutdown in progress, returning");
            return;
        }

        ClientIdToPlayerIdDictionary.Remove(clientId);
        connectedPlayerCount--;
        //alivePlayerNumber--;

		//Reset PlayerController values
        try
        {
        	PlayerController playerController = playerControllerList[playerId].GetComponent<PlayerController>();
			//Drop all inventory items

			Destroy(playerController.currentVoiceChatIngameSettings.gameObject);
			playerController.GetComponent<NfgoPlayer>().StopTracking();

            if (!NetworkManager.Singleton.ShutdownInProgress && base.IsServer)
            {
                playerController.gameObject.GetComponent<NetworkObject>().RemoveOwnership();
				playerController.TeleportPlayer(despawnTransform.position);
            	playerController.controlledByClient = false;
            }
            Debug.Log($"OnClientDisconnectedGameSessionClientRpc: Client #{clientId} disconnected.");
        }
        catch (Exception arg)
        {
            Debug.LogError($"OnClientDisconnectedGameSessionClientRpc: Error while handling player disconnect: {arg}");
        }
    }

	#endregion

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
		if (GameNetworkManager.Instance == null || localPlayerController == null)
		{
			return;
		}
		
		//PlayerController playerControllerB = ((!GameNetworkManager.Instance.localPlayerController.isPlayerDead || !(GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)) ? GameNetworkManager.Instance.localPlayerController : GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript);
		foreach (PlayerController playerController in playerControllerList)
		{
			//if ((!playerController.isPlayerControlled && !playerController.isPlayerDead) || playerController == GameNetworkManager.Instance.localPlayerController)
			if (!playerController.controlledByClient || playerController == localPlayerController)
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
		if (GameNetworkManager.Instance == null || localPlayerController == null)
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