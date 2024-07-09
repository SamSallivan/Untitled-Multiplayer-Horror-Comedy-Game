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
using Unity.VisualScripting;

public class GameSessionManager : NetworkBehaviour
{
	public static GameSessionManager Instance { get; private set; }

    [Foldout("Switches", true)]
	public bool hasHostSpawned;
	
    [Foldout("Values", true)]
	public int connectedClientCount;
	//public int alivePlayerNumber;
    public PlayerController localPlayerController;
    public Dictionary<ulong, int> ClientPlayerList = new Dictionary<ulong, int>();
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

		StartCoroutine(StartSpatialVoiceChat());

	}
	private IEnumerator StartSpatialVoiceChat()
	{
		yield return new WaitUntil(() =>  localPlayerController != null && localPlayerController.isPlayerControlled);

		foreach (PlayerController playerController in playerControllerList)
		{
			if ((bool)playerController.GetComponent<NfgoPlayer>() && !playerController.GetComponent<NfgoPlayer>().IsTracking)
			{
				playerController.GetComponent<NfgoPlayer>().VoiceChatTrackingStart();
			}
		}
		
		UpdatePlayerVoiceEffects();

	}

	public void UpdatePlayerVoiceEffects()
	{
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		updatePlayerVoiceInterval = 2f;
		//PlayerController playerControllerB = ((!GameNetworkManager.Instance.localPlayerController.isPlayerDead || !(GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)) ? GameNetworkManager.Instance.localPlayerController : GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript);
		for (int i = 0; i < playerControllerList.Count; i++)
		{
			PlayerController playerControllerB2 = playerControllerList[i];
			//if ((!playerControllerB2.isPlayerControlled && !playerControllerB2.isPlayerDead) || playerControllerB2 == GameNetworkManager.Instance.localPlayerController)
			if (playerControllerB2 == GameNetworkManager.Instance.localPlayerController)
			{
				continue;
			}
			if (playerControllerB2.voicePlayerState == null || playerControllerB2.currentVoiceChatIngameSettings._playerState == null || playerControllerB2.currentVoiceChatAudioSource == null)
			{
				RefreshPlayerVoicePlaybackObjects();
				if (playerControllerB2.voicePlayerState == null || playerControllerB2.currentVoiceChatAudioSource == null)
				{
					Debug.Log($"Was not able to access voice chat object for player #{i}; {playerControllerB2.voicePlayerState == null}; {playerControllerB2.currentVoiceChatAudioSource == null}");
					continue;
				}
			}
			AudioSource currentVoiceChatAudioSource = playerControllerList[i].currentVoiceChatAudioSource;
			bool flag = false;//playerControllerB2.speakingToWalkieTalkie && playerControllerB.holdingWalkieTalkie && playerControllerB2 != playerControllerB;
			// if (playerControllerB2.isPlayerDead)
			// {
			// 	currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().enabled = false;
			// 	currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>().enabled = false;
			// 	currentVoiceChatAudioSource.panStereo = 0f;
			// 	SoundManager.Instance.playerVoicePitchTargets[playerControllerB2.playerClientId] = 1f;
			// 	SoundManager.Instance.SetPlayerPitch(1f, (int)playerControllerB2.playerClientId);
			// 	if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			// 	{
			// 		currentVoiceChatAudioSource.spatialBlend = 0f;
			// 		playerControllerB2.currentVoiceChatIngameSettings.set2D = true;
			// 		playerControllerB2.voicePlayerState.Volume = 1f;
			// 	}
			// 	else
			// 	{
			// 		currentVoiceChatAudioSource.spatialBlend = 1f;
			// 		playerControllerB2.currentVoiceChatIngameSettings.set2D = false;
			// 		playerControllerB2.voicePlayerState.Volume = 0f;
			// 	}
			// 	continue;
			// }
			AudioLowPassFilter component = currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>();
			OccludeAudio component2 = currentVoiceChatAudioSource.GetComponent<OccludeAudio>();
			component.enabled = true;
			//component2.overridingLowPass = flag || playerControllerList[i].voiceMuffledByEnemy;
			currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>().enabled = flag;
			if (!flag)
			{
				currentVoiceChatAudioSource.spatialBlend = 1f;
				playerControllerB2.currentVoiceChatIngameSettings.set2D = false;
				currentVoiceChatAudioSource.bypassListenerEffects = false;
				currentVoiceChatAudioSource.bypassEffects = false;
				//currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerControllerB2.playerClientId];
				component.lowpassResonanceQ = 1f;
			}
			// else
			// {
			// 	currentVoiceChatAudioSource.spatialBlend = 0f;
			// 	playerControllerB2.currentVoiceChatIngameSettings.set2D = true;
			// 	if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			// 	{
			// 		currentVoiceChatAudioSource.panStereo = 0f;
			// 		currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerControllerB2.playerClientId];
			// 		currentVoiceChatAudioSource.bypassListenerEffects = false;
			// 		currentVoiceChatAudioSource.bypassEffects = false;
			// 	}
			// 	else
			// 	{
			// 		currentVoiceChatAudioSource.panStereo = 0.4f;
			// 		currentVoiceChatAudioSource.bypassListenerEffects = false;
			// 		currentVoiceChatAudioSource.bypassEffects = false;
			// 		currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerControllerB2.playerClientId];
			// 	}
			// 	component2.lowPassOverride = 4000f;
			// 	component.lowpassResonanceQ = 3f;
			// }
			// if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			// {
			// 	playerControllerB2.voicePlayerState.Volume = 0.8f;
			// }
			// else
			// {
			// 	playerControllerB2.voicePlayerState.Volume = 1f;
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
		for (int i = 0; i < playerControllerList.Count; i++)
		{
			PlayerController playerController = playerControllerList[i];
			if (!playerController.isPlayerControlled)// && !playerController.isPlayerDead)
			{
				Debug.Log($"Skipping player #{i} as they are not controlled or dead");
				continue;
			}
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j]._playerState == null)
				{
					array[j].FindPlayerIfNull();
					if (array[j]._playerState == null)
					{
						Debug.LogError($"Unable to connect player to voice B #{i}; {array[j].isActiveAndEnabled}; {array[j]._playerState == null}");
					}
				}
				else if (!array[j].isActiveAndEnabled)
				{
					Debug.LogError($"Unable to connect player to voice A #{i}; {array[j].isActiveAndEnabled}; {array[j]._playerState == null}");
				}
				else if (array[j]._playerState.Name == playerController.gameObject.GetComponentInChildren<NfgoPlayer>().PlayerId)
				{
					Debug.Log($"Found a match for voice object #{j} and player object #{i}");
					playerController.voicePlayerState = array[j]._playerState;
					playerController.currentVoiceChatAudioSource = array[j].voiceAudio;
					playerController.currentVoiceChatIngameSettings = array[j];
					//playerController.currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerController.playerClientId];
					//Debug.Log($"player voice chat audiosource: {playerController.currentVoiceChatAudioSource}; set audiomixer to {SoundManager.Instance.playerVoiceMixers[playerController.playerClientId]} ; {playerController.currentVoiceChatAudioSource.outputAudioMixerGroup} ; {playerController.playerClientId}");
				}
			}
		}
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
			UpdatePlayerVoiceEffects();
		}
		else
		{
			updatePlayerVoiceInterval += Time.deltaTime;
		}

    }

	public void OnHostConnectedGameSession()
	{
		ClientPlayerList.Add(NetworkManager.Singleton.LocalClientId, connectedClientCount);
		playerControllerList[0].GetComponent<NetworkObject>().ChangeOwnership(NetworkManager.Singleton.LocalClientId);
		playerControllerList[0].GetComponent<PlayerController>().isPlayerControlled = true;
		//alivePlayerNumber = connectedClientCount + 1;

		//Teleport player controller to its spawn position.
		//playerControllerList[0].TeleportPlayer(playerControllerList[0].gameObject.transform.position + new Vector3(0, 10, 0));
		playerControllerList[0].TeleportPlayer(spawnTransform.position);

		if (!GameNetworkManager.Instance.disableSteam)
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
		
		Debug.Log("player connected");
		Debug.Log($"connected players #: {connectedClientCount}");
		try
		{
			Debug.Log($"Connecting new player on host; clientId: {clientId}");

			List<int> occupiedPlayerSlotList = ClientPlayerList.Values.ToList();
			int targetlocalPlayerIndex = 0;
			for (int i = 1; i < GameNetworkManager.Instance.maxPlayerNumber; i++)
			{
				if (!occupiedPlayerSlotList.Contains(i))
				{
					targetlocalPlayerIndex = i;
					break;
				}
			}

            PlayerController newPlayerController = playerControllerList[targetlocalPlayerIndex];
			newPlayerController.localClientId = clientId;
			newPlayerController.GetComponent<NetworkObject>().ChangeOwnership(clientId);
			Debug.Log($"New player assigned object id: {newPlayerController}");

			List<ulong> connectedPlayerIdList = new List<ulong>();
			for (int j = 0; j < playerControllerList.Count; j++)
			{
				NetworkObject component = playerControllerList[j].GetComponent<NetworkObject>();
				if (!component.IsOwnedByServer)
				{
					connectedPlayerIdList.Add(component.OwnerClientId);
				}
				else if (j == 0)
				{
					connectedPlayerIdList.Add(NetworkManager.Singleton.LocalClientId);
				}
				else
				{
					connectedPlayerIdList.Add(999uL);
				}
			}

			OnClientConnectedGameSessionClientRpc(clientId, connectedClientCount, connectedPlayerIdList.ToArray(), targetlocalPlayerIndex);
			ClientPlayerList.Add(clientId, targetlocalPlayerIndex);
			Debug.Log($"client id connecting: {clientId} ; their corresponding player object id: {targetlocalPlayerIndex}");
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error occured in OnClientConnected! Shutting server down. clientId: {clientId}. Error: {arg}");
			GameNetworkManager.Instance.Disconnect();
		}

    }

	[ClientRpc]
	private void OnClientConnectedGameSessionClientRpc(ulong clientId, int connectedPlayerNumber, ulong[] connectedPlayerIds, int connectedLocalPlayerIndex)
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
			if (!base.IsServer)
			{
				ClientPlayerList.Clear();
				for (int i = 0; i < connectedPlayerIds.Length; i++)
				{
					if (connectedPlayerIds[i] == 999)
					{
						Debug.Log($"Skipping at index {i}");
						continue;
					}
					ClientPlayerList.Add(connectedPlayerIds[i], i);
					Debug.Log($"adding value to ClientPlayerList at value of index {i}: {connectedPlayerIds[i]}");
				}
				if (!ClientPlayerList.ContainsKey(clientId))
				{
					Debug.Log($"Successfully added new client id {clientId} and connected to object {connectedLocalPlayerIndex}");
					ClientPlayerList.Add(clientId, connectedLocalPlayerIndex);
				}
				else
				{
					Debug.Log("ClientId already in ClientPlayerList!");
				}
				Debug.Log($"clientplayerlist count for client: {ClientPlayerList.Count}");
				
			}
			this.connectedClientCount = connectedPlayerNumber + 1;
			Debug.Log("New player: " + playerControllerList[connectedLocalPlayerIndex].name);

			PlayerController playerController = playerControllerList[connectedLocalPlayerIndex];
			playerController.localClientId = clientId;

            //Teleport player controller to its spawn position.
            //playerControllerList[connectedLocalPlayerIndex].TeleportPlayer(playerControllerList[connectedLocalPlayerIndex].gameObject.transform.position + new Vector3(0, 15, 0));
			playerControllerList[connectedLocalPlayerIndex].TeleportPlayer(spawnTransform.position);

            for (int j = 0; j < this.connectedClientCount + 1; j++)
			{
				if (j == 0 || !playerControllerList[j].IsOwnedByServer)
				{
                    playerControllerList[j].isPlayerControlled = true;
				}
			}
			playerController.isPlayerControlled = true;
            //alivePlayerNumber = this.connectedClientCount + 1;
            Debug.Log($"Connected players after connection: {this.connectedClientCount}");


            // if (NetworkManager.Singleton.LocalClientId == clientId)
            // {
            //     //Detected same client as previous, sync back inventory, progress and stuff.
            // }
        }
        catch (Exception arg)
		{
			Debug.LogError($"Failed to assign new player with client id #{clientId}: {arg}");
			GameNetworkManager.Instance.Disconnect();
		}
	}

	public void OnClientDisconnectedGameSession(ulong clientId)
	{
		if (ClientPlayerList == null || !ClientPlayerList.ContainsKey(clientId))
		{
			return;
		}
		if (NetworkManager.Singleton == null || GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			GameNetworkManager.Instance.Disconnect();
			return;
        }
        Debug.Log(clientId + "   " + NetworkManager.Singleton.LocalClientId + "   " + GameNetworkManager.Instance.localPlayerController.localClientId);

        /*if (clientId == NetworkManager.Singleton.LocalClientId || clientId == GameNetworkManager.Instance.localPlayerController.localClientId)
		{
			Debug.Log("Disconnect callback called for local client; ignoring.");
            return;
		}*/

        /*if (!ClientPlayerList.TryGetValue(clientId, out var value))
		{
			Debug.LogError("Could not get player object number from client id on disconnect!");
			return;
		}*/

        /*if (!base.IsServer)
		{
			Debug.Log($"player disconnected c; {clientId}");
			Debug.Log(ClientPlayerList.Count);
			for (int i = 0; i < ClientPlayerList.Count; i++)
			{
				ClientPlayerList.TryGetValue((ulong)i, out var value2);
				Debug.Log($"client id: {i} ; player object id: {value2}");
			}
			Debug.Log($"disconnecting client id: {clientId}");
			if (ClientPlayerList.TryGetValue(clientId, out var value3) && value3 == 0)
			{
				Debug.Log("Host disconnected!");
				Debug.Log(GameNetworkManager.Instance.isDisconnecting);
				if (!GameNetworkManager.Instance.isDisconnecting)
				{
					Debug.Log("Host quit! Ending game for client.");
					GameNetworkManager.Instance.Disconnect();
					return;
				}
			}
			OnClientDisconnect(value, clientId);
		}

		else
		{
			List<ulong> list = new List<ulong>();
			foreach (KeyValuePair<ulong, int> clientPlayer in ClientPlayerList)
			{
				if (clientPlayer.Key != clientId)
				{
					list.Add(clientPlayer.Key);
				}
			}
			ClientRpcParams clientRpcParams = default(ClientRpcParams);
			clientRpcParams.Send = new ClientRpcSendParams
			{
				TargetClientIds = list.ToArray()
			};
			ClientRpcParams clientRpcParams2 = clientRpcParams;
			OnClientDisconnect(value, clientId);
			OnClientDisconnectClientRpc(value, clientId, clientRpcParams2);
		}*/

        for (int i = 0; i < ClientPlayerList.Count; i++)
        {
            ClientPlayerList.TryGetValue((ulong)i, out var value2);
            Debug.Log($"client id: {i} ; player object id: {value2}");
        }

        ClientPlayerList.TryGetValue(clientId, out var value);

		if (base.IsServer)
        {
            OnClientDisconnectClientRpc(value, clientId);
		}
		else
		{
            /*if (value == 0 && !GameNetworkManager.Instance.isDisconnecting)
            {
                Debug.Log("Host disconnected!");
                Debug.Log(clientId + " " + value);
                Debug.Log(GameNetworkManager.Instance.isDisconnecting);
                Debug.Log("Host quit! Ending game for client.");
                GameNetworkManager.Instance.Disconnect();
                return;
            }*/

            OnClientDisconnect(value, clientId);
        }
    }

    [ClientRpc]
	public void OnClientDisconnectClientRpc(int playerObjectNumber, ulong clientId, ClientRpcParams clientRpcParams = default(ClientRpcParams))
	{
		OnClientDisconnect(playerObjectNumber, clientId);
    }

    public void OnClientDisconnect(int playerObjectNumber, ulong clientId)
    {
        if (!ClientPlayerList.ContainsKey(clientId))
        {
            Debug.Log("disconnect: clientId key already removed!");
            return;
        }
        if (GameNetworkManager.Instance.localPlayerController != null && clientId == GameNetworkManager.Instance.localPlayerController.localClientId)
        {
            Debug.Log("OnLocalClientDisconnect: Local client is disconnecting so return.");
            return;
        }
        if (base.NetworkManager.ShutdownInProgress || NetworkManager.Singleton == null)
        {
            Debug.Log("Shutdown is in progress, returning");
            return;
        }
        Debug.Log("Player DC'ing 2");
        //Update alivePlayerNumber
        ClientPlayerList.Remove(clientId);
        connectedClientCount--;

		//Reset PlayerController values
        PlayerController playerController = playerControllerList[playerObjectNumber].GetComponent<PlayerController>();
        try
        {
            playerController.isPlayerControlled = false;
			//Drop all inventory items
			//playerController.TeleportPlayer(despawnTransform.position);
            StartCoroutine(DelayedDespawnTeleport(playerController));
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

    private IEnumerator DelayedDespawnTeleport(PlayerController playerController)
    {
        yield return null;
        yield return null;
        playerController.TeleportPlayer(despawnTransform.position);
    }
}
