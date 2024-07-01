using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using MyBox;

public class GameSessionManager : NetworkBehaviour
{
	public static GameSessionManager Instance { get; private set; }
	public int connectedPlayerNumber;
	public int alivePlayerNumber;
	public bool hasHostSpawned;
    public PlayerController localPlayerController;
    public Dictionary<ulong, int> ClientPlayerList = new Dictionary<ulong, int>();
    //public List<GameObject> playerGameObjectList = new List<GameObject>();
    public List<PlayerController> playerControllerList = new List<PlayerController>();


    [Foldout("References", true)]
    public Transform despawnTransform;


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
		if (base.IsServer)
		{
            //OnHostJoinedGameSession();
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
			hasHostSpawned = true;
			ClientPlayerList.Add(NetworkManager.Singleton.LocalClientId, connectedPlayerNumber);
			playerControllerList[0].GetComponent<NetworkObject>().ChangeOwnership(NetworkManager.Singleton.LocalClientId);
			playerControllerList[0].GetComponent<PlayerController>().isPlayerControlled = true;
			alivePlayerNumber = connectedPlayerNumber + 1;

            //Teleport player controller to its spawn position.
            playerControllerList[0].TeleportPlayer(playerControllerList[0].gameObject.transform.position + new Vector3(0, 10, 0));

			if (!GameNetworkManager.Instance.disableSteam)
			{
				GameNetworkManager.Instance.currentSteamLobby.Value.SetJoinable(true);
			}
		}
        
    }
	public void LateUpdate()
    {

    }

	public void OnClientConnectedGameSession(ulong clientId)
	{
		if (!base.IsServer)
		{
			return;
		}
		Debug.Log("player connected");
		Debug.Log($"connected players #: {connectedPlayerNumber}");
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

			OnClientConnectedGameSessionClientRpc(clientId, connectedPlayerNumber, connectedPlayerIdList.ToArray(), targetlocalPlayerIndex);
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
			this.connectedPlayerNumber = connectedPlayerNumber + 1;
			Debug.Log("New player: " + playerControllerList[connectedLocalPlayerIndex].name);

			PlayerController playerController = playerControllerList[connectedLocalPlayerIndex];
			playerController.localClientId = clientId;

            //Teleport player controller to its spawn position.
            playerControllerList[connectedLocalPlayerIndex].TeleportPlayer(playerControllerList[connectedLocalPlayerIndex].gameObject.transform.position + new Vector3(0, 15, 0));

            for (int j = 0; j < this.connectedPlayerNumber + 1; j++)
			{
				if (j == 0 || !playerControllerList[j].IsOwnedByServer)
				{
                    playerControllerList[j].isPlayerControlled = true;
				}
			}
			playerController.isPlayerControlled = true;
            alivePlayerNumber = this.connectedPlayerNumber + 1;
            Debug.Log($"Connected players after connection: {this.connectedPlayerNumber}");


            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                //Detected same client as previous, sync back inventory, progress and stuff.
            }
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
		if (clientId == NetworkManager.Singleton.LocalClientId || clientId == GameNetworkManager.Instance.localPlayerController.localClientId)
		{
			Debug.Log("Disconnect callback called for local client; ignoring.");
			return;
		}

		Debug.Log("Client disconnected from server");
		if (!ClientPlayerList.TryGetValue(clientId, out var value))
		{
			Debug.LogError("Could not get player object number from client id on disconnect!");
		}
		if (!base.IsServer)
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
			OnLocalClientDisconnect(value, clientId);
			return;
		}

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
		OnLocalClientDisconnect(value, clientId);
		OnClientDisconnectClientRpc(value, clientId, clientRpcParams2);
    }

    public void OnLocalClientDisconnect(int playerObjectNumber, ulong clientId)
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
        connectedPlayerNumber--;

		//Reset PlayerController values
        PlayerController playerController = playerControllerList[playerObjectNumber].GetComponent<PlayerController>();
        try
        {
            playerController.isPlayerControlled = false;
			//Drop all inventory items
			playerController.TeleportPlayer(despawnTransform.position);
            if (!NetworkManager.Singleton.ShutdownInProgress && base.IsServer)
            {
                playerController.gameObject.GetComponent<NetworkObject>().RemoveOwnership();
            }
            Debug.Log($"Current players after dc: {connectedPlayerNumber}");
        }
        catch (Exception arg)
        {
            Debug.LogError($"Error while handling player disconnect!: {arg}");
        }
    }

    [ClientRpc]
	public void OnClientDisconnectClientRpc(int playerObjectNumber, ulong clientId, ClientRpcParams clientRpcParams = default(ClientRpcParams))
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager != null && networkManager.IsListening)
		{
			if (networkManager.IsClient || networkManager.IsHost)
			{
				OnLocalClientDisconnect(playerObjectNumber, clientId);
			}
		}
	}
}
