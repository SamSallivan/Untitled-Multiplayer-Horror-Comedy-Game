using Steamworks;
using Steamworks.Data;
using Unity.VisualScripting;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Netcode.Transports.Facepunch;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Sirenix.OdinInspector;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance { get; private set; } = null;

    [Header("Settings")]
    public int gameVersionNumber = 1;
    public int maxPlayerNumber = 4;

    [Header("Switches")]
	[Sirenix.OdinInspector.ReadOnly]
	public bool steamDisabled;
	[Sirenix.OdinInspector.ReadOnly]
    public bool gameStarted;
	[Sirenix.OdinInspector.ReadOnly]
    public bool disconnecting;
	[Sirenix.OdinInspector.ReadOnly]
    public bool localClientJoinRequestPending;
	[Sirenix.OdinInspector.ReadOnly]
    public bool waitingForLobbyDataRefresh;
    private bool networkManagerCallbacksSubscribed;

    [Header("Values")]
	[Sirenix.OdinInspector.ReadOnly]
	public int connectedClientCount;
	[Sirenix.OdinInspector.ReadOnly]
	public string disconnectionReasonText;
	[Sirenix.OdinInspector.ReadOnly]
	public string localSteamClientUsername;
	[Sirenix.OdinInspector.ReadOnly]
    public LobbySettings lobbySettings;
	
    [Header("References")]
	[Sirenix.OdinInspector.ReadOnly]
    public PlayerController localPlayerController;
	private Coroutine lobbyRefreshTimeOutCoroutine;
	[Sirenix.OdinInspector.ReadOnly]
	public string currentSteamLobbyName;
	public Lobby? currentSteamLobby { get; private set; }
	[Sirenix.OdinInspector.ReadOnly]
	public List<SteamId> steamIdsInCurrentSteamLobby = new List<SteamId>();

	private void Awake()
	{
		if (Instance == null){
			Instance = this;
        }
		else
		{
			Destroy(gameObject);
			return;
        }

        DontDestroyOnLoad(gameObject);

		//Add Transport component to Network Manager
		if(!steamDisabled)
		{
			if (!NetworkManager.Singleton.GetComponent<FacepunchTransport>())
			{
				NetworkManager.Singleton.NetworkConfig.NetworkTransport = NetworkManager.Singleton.AddComponent<FacepunchTransport>();
			}
		}
		else
		{
			if (!NetworkManager.Singleton.GetComponent<UnityTransport>())
			{
				NetworkManager.Singleton.NetworkConfig.NetworkTransport = NetworkManager.Singleton.AddComponent<UnityTransport>();
			}
		}
	}
    
	private void GetLocalSteamClientUsername()
	{
		if (!steamDisabled)
		{
			string text = SteamClient.Name.ToString();
			if (text.Length > 18)
			{
				text.Remove(15, text.Length - 15);
				text += "...";
			}
			localSteamClientUsername = text;
		}
	}

	private void Start()
    {	
        GetComponent<NetworkManager>().NetworkConfig.ProtocolVersion = (ushort)gameVersionNumber;
	}

    private void OnEnable()
    {
        SubscribeToSteamMatchmakingCallbacks();
    }

    private void OnDisable()
	{
        UnsubscribeToSteamMatchmakingCallbacks();
	}

	public async void StartHost()
	{
		
		if(steamDisabled)
		{
			SwitchToUnityTransport();
		}
		else
		{
			SwitchToFacepunchTransport();
		}
		
		MainMenuManager.Instance.SetLoadingScreen(isLoading: true);

		if (currentSteamLobby.HasValue)
		{
			LeaveCurrentSteamLobby();
		}

		if (!steamDisabled)
		{
			GameNetworkManager instance = Instance;
			instance.currentSteamLobby = await SteamMatchmaking.CreateLobbyAsync(maxPlayerNumber);
		}

        SubscribeToNetworkManagerCallbacks();
        NetworkManager.Singleton.ConnectionApprovalCallback = NetworkManager_ConnectionApprovalCallback;

		try
		{
            if (NetworkManager.Singleton.StartHost())
			{
				Debug.Log("StartHost: Started host successfully");
				NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Single);
				StartCoroutine(DelayLoadLobbyScene());
			}
			else
			{
				Debug.Log("StartHost: Starting host failed");
				MainMenuManager.Instance.SetLoadingScreen(isLoading: false);
				//MainMenuManager.logText.text = "Failed to start server; 20";
				return;
			}
		}
		catch (Exception arg)
		{
			Debug.Log($"StartHost: Starting host failed: {arg}");
			MainMenuManager.Instance.SetLoadingScreen(isLoading: false);
			//MainMenuManager.logText.text = "Failed to start server; 30";
		}

		
		if (!steamDisabled)
		{
			steamIdsInCurrentSteamLobby.Add(SteamClient.SteamId);
		}

		connectedClientCount = 1;
    }
	
	private IEnumerator DelayLoadLobbyScene()
	{
		yield return new WaitForSeconds(0.1f);
		NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
	}

	public void StartClient(SteamId ownerSteamId)
	{
		steamDisabled = false;
		SwitchToFacepunchTransport();

		MainMenuManager.Instance.SetLoadingScreen(isLoading: true);
        localClientJoinRequestPending = true;
		GetComponent<FacepunchTransport>().targetSteamId = ownerSteamId;
		NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(Instance.gameVersionNumber + "," + (ulong)SteamClient.SteamId);

		try
		{
			if (NetworkManager.Singleton.StartClient()){
				Debug.Log($"StartClient: Client successfully joined room hosted by {ownerSteamId}");
				SubscribeToNetworkManagerCallbacks();
			}
			else
			{
				Debug.Log("StartClient: Joined steam lobby successfully, but connection failed");
				MainMenuManager.Instance.SetLoadingScreen(isLoading: false);
				//MainMenuManager.logText.text = "Failed to start client";
				LeaveCurrentSteamLobby();
				ResetNetworkManagerValues();
				return;
			}
		}
		catch (Exception arg)
		{
			Debug.Log($"StartClient: Client connection failed: {arg}");
			MainMenuManager.Instance.SetLoadingScreen(isLoading: false);
			//MainMenuManager.logText.text = "Failed to start client";
		}
	}

	public void StartClientLAN()
    {
		steamDisabled = true;
		SwitchToUnityTransport();

		MainMenuManager.Instance.SetLoadingScreen(isLoading: true);
        localClientJoinRequestPending = true;
		NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(gameVersionNumber.ToString());

		try
		{
			if (NetworkManager.Singleton.StartClient())
			{
				Debug.Log("StartLocalClient: Local client has started!");
				SubscribeToNetworkManagerCallbacks();
			}
			else
			{
				Debug.Log("StartLocalClient: Local client could not start.");
				MainMenuManager.Instance.SetLoadingScreen(isLoading: false);
				//MainMenuManager.logText.text = "Failed to start client";
			}
		}
		catch (Exception arg)
		{
			Debug.Log($"StartLocalClient: Client connection failed: {arg}");
			MainMenuManager.Instance.SetLoadingScreen(isLoading: false);
			//MainMenuManager.logText.text = "Failed to start client";
		}
		
	}

	public void LeaveCurrentSteamLobby()
	{
		Debug.Log("LeaveCurrentSteamLobby");
		try
		{
			if (currentSteamLobby.HasValue)
			{
				Instance.currentSteamLobby.Value.Leave();
				Instance.currentSteamLobby = null;
				steamIdsInCurrentSteamLobby.Clear();
				currentSteamLobbyName = SteamClient.Name;
			}
		}
		catch (Exception arg)
		{
			Debug.Log($"LeaveCurrentSteamLobby: Error: {arg}");
		}
	}

	// private void OnApplicationQuit()
	// {
	// 	try
	// 	{
	// 		//Disconnect();
	// 	}
	// 	catch (Exception arg)
	// 	{
	// 		Debug.LogError($"Error while disconnecting: {arg}");
	// 	}
	// }

	[Button("Disconnect")]
	public void Disconnect()
	{
		if (disconnecting || !GameSessionManager.Instance)
		{
			return;
		}
		Debug.Log("Disconnect: Disconnecting.");
		
		disconnecting = true;

		GameSessionManager.Instance.Save();

		if (!steamDisabled)
		{
			LeaveCurrentSteamLobby();
		}

		NetworkObject[] array = UnityEngine.Object.FindObjectsOfType<NetworkObject>(includeInactive: true);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].DontDestroyWithOwner = false;
		}
		
		StartCoroutine(ReturnToMainMenuCoroutine());

		// if (NetworkManager.Singleton == null)
		// {
		// 	Debug.Log("Server is not active; quitting to main menu");
		// 	ResetGameValues();
		// 	SceneManager.LoadScene("MainMenu");
		// }
		// else
		// {
		// 	StartCoroutine(ReturnToMainMenuCoroutine());
		// }
	}

	private IEnumerator ReturnToMainMenuCoroutine()
	{
		Debug.Log($"Disconnect: Shutting down and disconnecting from server.");
		NetworkManager.Singleton.Shutdown();

		yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);

		ResetNetworkManagerValues();
		SceneManager.LoadScene("MainMenu");
	}


	#region Steam Callbacks
	public void SubscribeToSteamMatchmakingCallbacks()
	{
		if (!steamDisabled)
		{
            SteamMatchmaking.OnLobbyCreated += SteamMatchmaking_OnLobbyCreated;
            //SteamMatchmaking.OnLobbyEntered += SteamMatchmaking_OnLobbyEntered;
            SteamMatchmaking.OnLobbyMemberJoined += SteamMatchmaking_OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave += SteamMatchmaking_OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyInvite += SteamMatchmaking_OnLobbyInvite;
			//SteamMatchmaking.OnLobbyGameCreated += SteamMatchmaking_OnLobbyGameCreated;
            SteamFriends.OnGameLobbyJoinRequested += SteamFriends_OnGameLobbyJoinRequested;
        }
	}	

	public void UnsubscribeToSteamMatchmakingCallbacks()
	{
		if (!steamDisabled)
		{
            SteamMatchmaking.OnLobbyCreated -= SteamMatchmaking_OnLobbyCreated;
            //SteamMatchmaking.OnLobbyEntered -= SteamMatchmaking_OnLobbyEntered;
            SteamMatchmaking.OnLobbyMemberJoined -= SteamMatchmaking_OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave -= SteamMatchmaking_OnLobbyMemberLeave;
            SteamMatchmaking.OnLobbyInvite -= SteamMatchmaking_OnLobbyInvite;
			//SteamMatchmaking.OnLobbyGameCreated -= SteamMatchmaking_OnLobbyGameCreated;
            SteamFriends.OnGameLobbyJoinRequested -= SteamFriends_OnGameLobbyJoinRequested;
        }
	}	

	private void SteamFriends_OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
	{
        Debug.Log($"SteamFriends_OnGameLobbyJoinRequested: Join requested through steam invite");
        Debug.Log($"SteamFriends_OnGameLobbyJoinRequested: lobby id: {lobby.Id}");
        VerifyLobbyJoinRequest(lobby, lobby.Id);
	}

	public void VerifyLobbyJoinRequest(Lobby lobby, SteamId lobbyId)
	{
		if (waitingForLobbyDataRefresh)
		{
			return;
		}

		if (currentSteamLobby.HasValue)
		{
            Debug.Log("SteamFriends_OnGameLobbyJoinRequested: Already in a lobby.");
			if (MainMenuManager.Instance)
			{
				MainMenuManager.Instance.DisplayNotification("You are already in a lobby!", "Back");
			}
			LeaveCurrentSteamLobby();
            return;
		}

		MainMenuManager.Instance.SetLoadingScreen(isLoading: true);

        Destroy(NetworkManager.Singleton.GetComponent<UnityTransport>());
        if (!NetworkManager.Singleton.GetComponent<FacepunchTransport>())
        {
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = NetworkManager.Singleton.AddComponent<FacepunchTransport>();
        }

        SubscribeToSteamMatchmakingCallbacks();

		SteamMatchmaking.OnLobbyDataChanged += SteamMatchmaking_OnLobbyDataChanged;
		waitingForLobbyDataRefresh = true;

		if (lobby.Refresh())
		{
			lobbyRefreshTimeOutCoroutine = StartCoroutine(LobbyRefreshTimeOutCoroutine());
			Debug.Log("VerifyLobbyJoinRequest: Waiting for lobby data refresh");
		}
		else
		{
			Debug.Log("VerifyLobbyJoinRequest: Could not refresh lobby");
			SteamMatchmaking.OnLobbyDataChanged -= SteamMatchmaking_OnLobbyDataChanged;
			MainMenuManager.Instance.SetLoadingScreen(isLoading: false, RoomEnter.Error, "Error! Could not get the lobby data. Are you offline?");
		}
	}	

	public IEnumerator LobbyRefreshTimeOutCoroutine()
	{
		yield return new WaitForSeconds(7f);
		waitingForLobbyDataRefresh = false;
		SteamMatchmaking.OnLobbyDataChanged -= SteamMatchmaking_OnLobbyDataChanged;
		GetComponent<SteamLobbyManager>().serverListBlankText.text = "Could not get the lobby data.";
        MainMenuManager.Instance.SetLoadingScreen(isLoading: false, RoomEnter.Error, "Error! Could not get the lobby data. Are you offline?");
    }

    public void SteamMatchmaking_OnLobbyDataChanged(Lobby lobby)
	{
		if (lobbyRefreshTimeOutCoroutine != null)
		{
			StopCoroutine(lobbyRefreshTimeOutCoroutine);
			lobbyRefreshTimeOutCoroutine = null;
		}
		if (!waitingForLobbyDataRefresh)
		{
			return;
		}

		waitingForLobbyDataRefresh = false;
		SteamMatchmaking.OnLobbyDataChanged -= SteamMatchmaking_OnLobbyDataChanged;
		Debug.Log($"SteamMatchmaking_OnLobbyDataChanged: Lobby data refreshed, lobby id {lobby.Id}");
		Debug.Log($"SteamMatchmaking_OnLobbyDataChanged: Members in lobby: {lobby.MemberCount}");
		
        if(IsLobbyJoinable(lobby)){
            JoinLobby(lobby, lobby.Id);
        }
	}

	public bool IsLobbyJoinable(Lobby lobby)
	{
		string version = lobby.GetData("vers");
		if (version != Instance.gameVersionNumber.ToString())
		{
			Debug.Log($"IsLobbyJoinable: Lobby join denied! Attempted to join vers.{version}");
			MainMenuManager.Instance.SetLoadingScreen(isLoading: false, RoomEnter.DoesntExist, $"The server host is playing on version {version} while you are on version {gameVersionNumber}.");
			return false;
		}

		Friend[] blockedFriends = SteamFriends.GetBlocked().ToArray();
		if (blockedFriends != null)
		{
			foreach(Friend friend in blockedFriends)
			{
				Debug.Log($"IsLobbyJoinable: blocked user: {friend.Name}; id: {friend.Id}");
				if (lobby.IsOwnedBy(friend.Id))
				{
                    MainMenuManager.Instance.SetLoadingScreen(isLoading: false, RoomEnter.DoesntExist, "Lobby owned by a blocked user.");
                    return false;
				}
			}
		}

		if (lobby.GetData("joinable") == "false")
		{
			Debug.Log("IsLobbyJoinable: Lobby join denied! Host lobby is not joinable");
            MainMenuManager.Instance.SetLoadingScreen(isLoading: false, RoomEnter.DoesntExist, "Lobby is not joinable. Game already started.");
            return false;
		}

		if (lobby.MemberCount >= 4 || lobby.MemberCount < 1)
		{
			Debug.Log($"IsLobbyJoinable: Lobby join denied! Too many members in lobby! {lobby.Id}");
            MainMenuManager.Instance.SetLoadingScreen(isLoading: false, RoomEnter.Full, "The server is full!");
            return false;
		}

		Debug.Log($"IsLobbyJoinable: Lobby join successful!");
		return true;
	}

	public async void JoinLobby(Lobby lobby, SteamId id)
	{
		Debug.Log($"JoinLobby: Lobby id: {lobby.Id}");
		Debug.Log($"JoinLobby: Steam id: {id}");

		if (!currentSteamLobby.HasValue)
		{
			currentSteamLobby = lobby;
			currentSteamLobbyName = lobby.GetData("name");
			if (await lobby.Join() == RoomEnter.Success)
			{
				Debug.Log("JoinLobby: Successfully joined steam lobby.");
				StartClient(lobby.Owner.Id);
			}
			else
			{
				Debug.Log("JoinLobby: Failed to join steam lobby.");
				LeaveCurrentSteamLobby();
                MainMenuManager.Instance.SetLoadingScreen(isLoading: false, RoomEnter.Error, "Failed to join steam lobby.");
            }
        }
		else
		{
            Debug.Log("JoinLobby: Already in a lobby.");
			if (MainMenuManager.Instance)
			{
				MainMenuManager.Instance.DisplayNotification("You are already in a lobby!", "Back");
			}
			LeaveCurrentSteamLobby();
            return;
		}
	}

	private void SteamMatchmaking_OnLobbyInvite(Friend friend, Lobby lobby)
	{
		Debug.Log($"SteamMatchmaking_OnLobbyInvite: You got invited by {friend.Name} to join lobby: {lobby.Id}");
	}

	private void SteamMatchmaking_OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
		if (steamIdsInCurrentSteamLobby.Contains(friend.Id))
		{
			steamIdsInCurrentSteamLobby.Remove(friend.Id);
		}
    }

	private void SteamMatchmaking_OnLobbyMemberJoined(Lobby lobby, Friend friend)	
    {
		if (Instance.currentSteamLobby.HasValue)
		{
			Friend[] array = Instance.currentSteamLobby.Value.Members.ToArray();
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (!steamIdsInCurrentSteamLobby.Contains(array[i].Id))
					{
						steamIdsInCurrentSteamLobby.Add(array[i].Id);
					}
				}
			}
		}
		Debug.Log($"SteamMatchmaking_OnLobbyMemberJoined: Player joined w steamId: {friend.Id}");
	}

    private void SteamMatchmaking_OnLobbyCreated(Result result, Lobby lobby)
	{
		if (result != Result.OK)
        {
			Debug.LogError($"SteamMatchmaking_OnLobbyCreated: Lobby couldn't be created!, {result}");
			return;
		}

		lobby.SetData("name", lobbySettings.lobbyName.ToString());
		lobby.SetData("vers", gameVersionNumber.ToString());

		if (!string.IsNullOrEmpty(lobbySettings.serverTag))
		{
			lobby.SetData("tag", lobbySettings.serverTag.ToLower());
		}
		else
		{
			lobby.SetData("tag", "none");
		}

		if (lobbySettings.isLobbyPublic)
		{
			lobby.SetPublic();
		}
		else
		{
			lobby.SetPrivate();
			lobby.SetFriendsOnly();
		}

		lobby.SetJoinable(b: false);
		currentSteamLobby = lobby;
		currentSteamLobbyName = lobby.GetData("name");

		Debug.Log("SteamMatchmaking_OnLobbyCreated: Lobby " + currentSteamLobbyName + " has been created.");
	}

	#endregion

	#region Network Callbacks

	public void 
	SubscribeToNetworkManagerCallbacks()
	{
		if (NetworkManager.Singleton == null){
			return;
		}

		if (!networkManagerCallbacksSubscribed)
		{
			NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
			NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
            //NetworkManager.Singleton.OnServerStarted += NetworkManager_OnServerStarted;
            //NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
			networkManagerCallbacksSubscribed = true;
		}
	}	
    
    public void UnsubscribeToNetworkManagerCallbacks()
	{
        if (NetworkManager.Singleton == null)
			return;

		if (networkManagerCallbacksSubscribed)
		{
			NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
			NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
            //NetworkManager.Singleton.OnServerStarted -= NetworkManager_OnServerStarted;
            //NetworkManager.Singleton.ConnectionApprovalCallback -= NetworkManager_ConnectionApprovalCallback;
            networkManagerCallbacksSubscribed = false;
		}
	}

	private void NetworkManager_OnServerStarted()
    {
	}

    private void NetworkManager_OnClientConnectedCallback(ulong clientId)
	{
		if (NetworkManager.Singleton != null && GameSessionManager.Instance != null)
		{
			if (NetworkManager.Singleton.IsServer)
			{
				connectedClientCount++;
				GameSessionManager.Instance.OnClientConnectedGameSession(clientId);
			}
		}
	}
	
    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        Debug.Log($"NetworkManager_ConnectionApprovalCallback: Joining client id: {request.ClientNetworkId}; Local/host client id: {NetworkManager.Singleton.LocalClientId}");
        if (request.ClientNetworkId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("NetworkManager_ConnectionApprovalCallback: Stopped as the client in question was the host.");
            return;
        }

        Debug.Log($"NetworkManager_ConnectionApprovalCallback: Game version of client request: " + Encoding.ASCII.GetString(request.Payload).ToString());
        bool flag = true;
		string payload = Encoding.ASCII.GetString(request.Payload);
		string clientGameVersion = payload.Split(",")[0];

        if (disconnecting)
        {
            response.Reason = "The host was not accepting connections.";
			flag = false;
		}
		else if (string.IsNullOrEmpty(payload))
		{
			response.Reason = "Unknown; please verify your game files.";
			flag = false;
		}
		else if (connectedClientCount >= maxPlayerNumber)
		{
			response.Reason = "Lobby is full!";
			flag = false;
		}
		else if (gameStarted)
		{
			response.Reason = "Game has already started!";
			flag = false;
		}
		else if (gameVersionNumber.ToString() != clientGameVersion)
		{
			response.Reason = $"Game version mismatch! Their version: {gameVersionNumber}. Your version: {clientGameVersion[0]}";
			flag = false;
		}
		// else if (!steamDisabled && *hasBeenKickedBefore*)
		// {
		// 	response.Reason = "You cannot rejoin after being kicked.";
		// 	flag = false;
		// }
        Debug.Log($"NetworkManager_ConnectionApprovalCallback: Approved connection?: {flag}.");
		if(!flag)
		{
        	Debug.Log("NetworkManager_ConnectionApprovalCallback: Disapproval reason: " + response.Reason);
		}
        response.CreatePlayerObject = false;
        response.Approved = flag;
        response.Pending = false;
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
		Debug.Log("NetworkManager_OnClientDisconnectCallback: ");

		if (NetworkManager.Singleton == null)
		{
			return;
		}
		
		if (clientId == NetworkManager.Singleton.LocalClientId && localClientJoinRequestPending)
		{
			Debug.Log("NetworkManager_OnClientDisconnectCallback: Join request disapproved.");
			OnLocalClientJoinRequestDisapproved(clientId);
			return;
		}

		//
		if (clientId == NetworkManager.Singleton.LocalClientId)
		{
			Debug.Log("NetworkManager_OnClientDisconnectCallback: Local client disconnected, returning to main menu.");
			disconnectionReasonText = "Disconnected.";
			Disconnect();
		}

        if (NetworkManager.Singleton.IsServer)
		{
			connectedClientCount--;

			// //
			// if (clientId == NetworkManager.Singleton.LocalClientId)
			// {
			// 	Debug.Log("NetworkManager_OnClientDisconnectCallback: Local server disconnected, ignoring.");
			// 	return;
			// }
        }

		GameSessionManager.Instance.OnClientDisconnectedGameSession(clientId);
	}

	private void OnLocalClientJoinRequestDisapproved(ulong clientId)
	{
		localClientJoinRequestPending = false;
		if (!string.IsNullOrEmpty(NetworkManager.Singleton.DisconnectReason))
		{
			disconnectionReasonText = NetworkManager.Singleton.DisconnectReason;
		}
		Debug.Log($"OnLocalClientJoinRequestDisapproved: Connection denied; reason: {NetworkManager.Singleton.DisconnectReason}");
		MainMenuManager.Instance.SetLoadingScreen(isLoading: false);
		LeaveCurrentSteamLobby();
		ResetNetworkManagerValues();
		if (NetworkManager.Singleton.IsConnectedClient)
		{
			Debug.Log("OnLocalClientJoinRequestDisapproved: Calling shutdown on NetworkManager.");
			NetworkManager.Singleton.Shutdown(discardMessageQueue: true);
		}
	}

	private void ResetNetworkManagerValues()
	{
		disconnecting = false;
		connectedClientCount = 0;
		localPlayerController = null;
		gameStarted = false;
        UnsubscribeToNetworkManagerCallbacks();
	}

    #endregion

	public void SwitchToUnityTransport()
	{
		Destroy(NetworkManager.Singleton.GetComponent<FacepunchTransport>());
		if (!NetworkManager.Singleton.GetComponent<UnityTransport>())
		{
			NetworkManager.Singleton.NetworkConfig.NetworkTransport = NetworkManager.Singleton.AddComponent<UnityTransport>();
			NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.ServerListenAddress = "0.0.0.0";
		}
		UnsubscribeToSteamMatchmakingCallbacks();
	}

	public void SwitchToFacepunchTransport()
	{
		Destroy(NetworkManager.Singleton.GetComponent<UnityTransport>());
		if (!NetworkManager.Singleton.GetComponent<FacepunchTransport>())
		{
			NetworkManager.Singleton.NetworkConfig.NetworkTransport = NetworkManager.Singleton.AddComponent<FacepunchTransport>();
		}
		SubscribeToSteamMatchmakingCallbacks();
	}

}

public class LobbySettings
{
	public string lobbyName = "";

	public string serverTag = "";

	public bool isLobbyPublic;

	public LobbySettings(string name, bool isPublic, string setTag = "")
	{
		lobbyName = name;
		isLobbyPublic = isPublic;
		serverTag = setTag;
	}
}
