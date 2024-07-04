using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Unity.VisualScripting;

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance { get; private set; } = null;

    public int gameVersionNumber = 1;

	public bool disableSteam;
	public bool isHostingGame;
	public bool disallowConnection;
    public bool gameStarted;
    public bool isDisconnecting;
    public bool localClientJoinRequestPending;
    public bool isWaitingForLobbyDataRefresh;

    private bool networkManagerCallbacksSubscribed;

	public int totalPlayerCount;
    public int maxPlayerNumber = 4;

	public string disconnectionReasonText;
	public string username;

    public HostSettings lobbySettings;

    public PlayerController localPlayerController;
	private Coroutine lobbyRefreshTimeOutCoroutine;


	private FacepunchTransport transport;

	public List<Lobby> Lobbies { get; private set; } = new List<Lobby>(capacity: 100);
	public Lobby? currentSteamLobby { get; private set; }
	public string currentSteamLobbyName;
	public List<SteamId> steamIdsInCurrentSteamLobby = new List<SteamId>();

	private void Awake()
	{
		if (Instance == null){
			Instance = this;
			StartCoroutine(GetLocalSteamClientUsernameCoroutine());
        }
		else
		{
			Destroy(gameObject);
			return;
        }

        if (!NetworkManager.Singleton.GetComponent<FacepunchTransport>())
        {
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = NetworkManager.Singleton.AddComponent<FacepunchTransport>();
        }

        DontDestroyOnLoad(gameObject);
	}
    
	private IEnumerator GetLocalSteamClientUsernameCoroutine()
	{
		yield return null;
		yield return null;
		if (!disableSteam)
		{
			string text = SteamClient.Name.ToString();
			if (text.Length > 18)
			{
				text.Remove(15, text.Length - 15);
				text += "...";
			}
			username = text;
		}
		else
		{
			username = "PlayerName";
		}
	}

	private void Start()
    {	
        GetComponent<NetworkManager>().NetworkConfig.ProtocolVersion = (ushort)gameVersionNumber;

        if (GetComponent<FacepunchTransport>())
		{
			transport = GetComponent<FacepunchTransport>();
		}
		else
		{
			Debug.Log("Facepunch transport is disabled.");
		}
	}

    private void OnEnable()
    {
        SubscribeToSteamMatchmakingCallbacks();
    }

    private void OnDisable()
	{
        UnsubscribeToSteamMatchmakingCallbacks();
	}

	//public void SetSteamFriendGroupStatus(string status)
	//{
		//SteamFriends.SetRichPresence("string key, string value");
    //}

	private void OnApplicationQuit()
	{
		try
		{
			//Save
			Disconnect();
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error while disconnecting: {arg}");
		}
	}

	public void Disconnect()
	{
		if (!isDisconnecting && GameSessionManager.Instance != null)
		{
			isDisconnecting = true;
			if (isHostingGame)
			{
				disallowConnection = true;
			}

            if (!disableSteam)
            {
                Debug.Log("Leaving current lobby");
                LeaveCurrentSteamLobby();
                currentSteamLobbyName = SteamClient.Name;
            }

            Debug.Log("Disconnecting and setting networkobjects to destroy with owner");
            NetworkObject[] array = UnityEngine.Object.FindObjectsOfType<NetworkObject>(includeInactive: true);
            for (int i = 0; i < array.Length; i++)
            {
                array[i].DontDestroyWithOwner = false;
            }
            
			if (NetworkManager.Singleton == null)
			{
				Debug.Log("Server is not active; quitting to main menu");
				ResetGameValues();
				SceneManager.LoadScene("MainMenu");
			}
			else
			{
				StartCoroutine(ReturnToMainMenuCoroutine());
			}
		}
	}

	private IEnumerator ReturnToMainMenuCoroutine()
	{
		Debug.Log($"Shutting down and disconnecting from server. Is host?: {NetworkManager.Singleton.IsServer}");
		NetworkManager.Singleton.Shutdown();
		yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
		ResetGameValues();
		SceneManager.LoadScene("MainMenu");
	}

	public async void StartHost()
	{
		// if (MainMenuManager.Instance == null)
		// {
		// 	Debug.Log("MainMenuManager script is not present in scene; unable to start host");
		// 	return;
		// }
		if (Instance.currentSteamLobby.HasValue)
		{
			Debug.Log("Tried starting host but currentLobby is not null! This should not happen. Leaving currentLobby and setting null.");
			LeaveCurrentSteamLobby();
		}

		if (!disableSteam)
		{
			GameNetworkManager instance = Instance;
			instance.currentSteamLobby = await SteamMatchmaking.CreateLobbyAsync(maxPlayerNumber);
		}

		//MainMenuManager.SetLoadingScreen(isLoading: true);
		try
		{
            if (NetworkManager.Singleton.StartHost())
			{
				Debug.Log("started host!");
				Debug.Log($"are we in a server?: {NetworkManager.Singleton.IsServer}");
				NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Single);
		        //NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
				StartCoroutine(DelayLoadLobbyScene());
			}
			else
			{
				//MainMenuManager.SetLoadingScreen(isLoading: false);
				//MainMenuManager.logText.text = "Failed to start server; 20";
			}
		}
		catch (Exception arg)
		{
			//MainMenuManager.logText.text = "Failed to start server; 30";
			Debug.Log($"Server connection failed: {arg}");
		}

        SubscribeToNetworkManagerCallbacks();
        NetworkManager.Singleton.ConnectionApprovalCallback = NetworkManager_ConnectionApprovalCallback;
			
		if (!disableSteam)
		{
			steamIdsInCurrentSteamLobby.Add(SteamClient.SteamId);
		}
		isHostingGame = true;
		totalPlayerCount = 1;
    }
	
	private IEnumerator DelayLoadLobbyScene()
	{
		yield return new WaitForSeconds(1f);
		yield return new WaitForSeconds(0.1f);
		NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
	}

	public void StartClient(SteamId id)
	{

		Debug.Log($"CC {id}");
		transport.targetSteamId = id;

        localClientJoinRequestPending = true;
		if (disableSteam)
		{
			NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(Instance.gameVersionNumber.ToString());
		}
		else
		{
			NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(Instance.gameVersionNumber + "," + (ulong)SteamClient.SteamId);
		}

		Debug.Log($"Joining room hosted by {transport.targetSteamId}", this);

		if (NetworkManager.Singleton.StartClient()){
			Debug.Log("Client has joined!", this);
            SubscribeToNetworkManagerCallbacks();
            //UnityEngine.Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: true);
            return;
        }
		Debug.Log("Joined steam lobby successfully, but connection failed");
		//UnityEngine.Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: false);
        LeaveCurrentSteamLobby();
        currentSteamLobbyName = SteamClient.Name;
		ResetNetworkManagerValues();
	}

	public void StartLocalClient()
    {
        localClientJoinRequestPending = true;
		NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(Instance.gameVersionNumber.ToString());

		SubscribeToNetworkManagerCallbacks();
		if (NetworkManager.Singleton.StartClient())
		{
			Debug.Log("Started a client");		
		}
		else
		{
			Debug.Log("Could not start client");
		}
		
	}
	public void LeaveCurrentSteamLobby()
	{
		try
		{
			if (Instance.currentSteamLobby.HasValue)
			{
				Instance.currentSteamLobby.Value.Leave();
				Instance.currentSteamLobby = null;
				steamIdsInCurrentSteamLobby.Clear();
			}
		}
		catch (Exception arg)
		{
			Debug.Log($"Error caught while attempting to leave current lobby!: {arg}");
		}
	}

	#region Steam Callbacks
	public void SubscribeToSteamMatchmakingCallbacks()
	{
		if (!disableSteam)
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
		if (!disableSteam)
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
		// if (MainMenuManager.Instance == null)
		// {
		// 	return;
		// }
		if (Instance.currentSteamLobby.HasValue)
		{
            Debug.Log("Attempted to join by Steam invite request, but already in a lobby.");
            // MenuManager menuManager = UnityEngine.Object.FindObjectOfType<MainMenuManager>();
            // if (menuManager != null)
            // {
            // 	menuManager.DisplayMenuNotification("You are already in a lobby!", "Back");
            // }
            // Instance.currentSteamLobby.Value.Leave();
            // Instance.currentSteamLobby = null;
            return;
		}
        Debug.Log("JOIN REQUESTED through steam invite");
        Debug.Log($"lobby id: {lobby.Id}");
        VerifyLobbyJoinRequest(lobby, lobby.Id);
	}

	public void VerifyLobbyJoinRequest(Lobby lobby, SteamId lobbyId)
	{
		if (isWaitingForLobbyDataRefresh)
		{
			return;
		}

        Destroy(NetworkManager.Singleton.GetComponent<UnityTransport>());
        if (!NetworkManager.Singleton.GetComponent<FacepunchTransport>())
        {
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = NetworkManager.Singleton.AddComponent<FacepunchTransport>();
        }
        GameNetworkManager.Instance.SubscribeToSteamMatchmakingCallbacks();

        // MainMenuManager menuManager = FindObjectOfType<MainMenuManager>();
        // if (menuManager == null)
        // {
        // 	return;
        // }

        Debug.Log($"Lobby id joining: {lobbyId}");
		SteamMatchmaking.OnLobbyDataChanged += SteamMatchmaking_OnLobbyDataChanged;
		isWaitingForLobbyDataRefresh = true;
		Debug.Log("refreshing lobby...");

		if (lobby.Refresh())
		{
			lobbyRefreshTimeOutCoroutine = StartCoroutine(LobbyRefreshTimeOutCoroutine());
			Debug.Log("Waiting for lobby data refresh");
			//menuManager.SetLoadingScreen(isLoading: true);
		}
		else
		{
			Debug.Log("Could not refresh lobby");
			SteamMatchmaking.OnLobbyDataChanged -= SteamMatchmaking_OnLobbyDataChanged;
			//menuManager.SetLoadingScreen(isLoading: false, RoomEnter.Error, "Error! Could not get the lobby data. Are you offline?");
		}
	}	

	public IEnumerator LobbyRefreshTimeOutCoroutine()
	{
		yield return new WaitForSeconds(7f);
		isWaitingForLobbyDataRefresh = false;
		SteamMatchmaking.OnLobbyDataChanged -= SteamMatchmaking_OnLobbyDataChanged;
		GetComponent<SteamLobbyManager>().serverListBlankText.text = "Error! Could not get the lobby data.";
        //UnityEngine.Object.FindObjectOfType<MainMenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.Error, "Error! Could not get the lobby data. Are you offline?");
    }

    public void SteamMatchmaking_OnLobbyDataChanged(Lobby lobby)
	{
		if (lobbyRefreshTimeOutCoroutine != null)
		{
			StopCoroutine(lobbyRefreshTimeOutCoroutine);
			lobbyRefreshTimeOutCoroutine = null;
		}
		if (!isWaitingForLobbyDataRefresh)
		{
			Debug.Log("Not waiting for lobby data refresh; returned");
			return;
		}
		isWaitingForLobbyDataRefresh = false;
		SteamMatchmaking.OnLobbyDataChanged -= SteamMatchmaking_OnLobbyDataChanged;
		Debug.Log($"Got lobby data refresh!; {lobby.Id}");
		Debug.Log($"Members in lobby: {lobby.MemberCount}");
		
        if(IsLobbyJoinable(lobby)){
            JoinLobby(lobby, lobby.Id);
        }
	}

	public bool IsLobbyJoinable(Lobby lobby)
	{
		string data = lobby.GetData("vers");
		if (data != Instance.gameVersionNumber.ToString())
		{
			Debug.Log($"Lobby join denied! Attempted to join vers.{data} lobby id: {lobby.Id}");
			//UnityEngine.Object.FindObjectOfType<MainMenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.DoesntExist, $"The server host is playing on version {data} while you are on version {Instance.gameVersionNum}.");
			return false;
		}

		Friend[] blockedFriends = SteamFriends.GetBlocked().ToArray();
		if (blockedFriends != null)
		{
			foreach(Friend friend in blockedFriends)
			{
				Debug.Log($"blocked users: {friend.Name}; id: {friend.Id}");
				if (lobby.IsOwnedBy(friend.Id))
				{
                    //UnityEngine.Object.FindObjectOfType<MainMenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.DoesntExist, "An error occured!");
                    return false;
				}
			}
		}

		if (lobby.GetData("joinable") == "false")
		{
			Debug.Log("Lobby join denied! Host lobby is not joinable");
            //UnityEngine.Object.FindObjectOfType<MainMenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.DoesntExist, "The server host has already landed their ship, or they are still loading in.");
            return false;
		}

		if (lobby.MemberCount >= 4 || lobby.MemberCount < 1)
		{
			Debug.Log($"Lobby join denied! Too many members in lobby! {lobby.Id}");
            //UnityEngine.Object.FindObjectOfType<MainMenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.Full, "The server is full!");
            return false;
		}

		Debug.Log($"Lobby join accepted! Lobby id {lobby.Id} is OK");
		return true;
	}

	public async void JoinLobby(Lobby lobby, SteamId id)
	{
		Debug.Log($"lobby.id: {lobby.Id}");
		Debug.Log($"id: {id}");
		// if (MainMenuManager.Instance == null)
		// {
		// 	return;
		// }
		if (!Instance.currentSteamLobby.HasValue)
		{
			Instance.currentSteamLobby = lobby;
			currentSteamLobbyName = lobby.GetData("name");
			if (await lobby.Join() == RoomEnter.Success)
			{
				Debug.Log("Successfully joined steam lobby.");
				Debug.Log($"AA {Instance.currentSteamLobby.Value.Id}");
				Debug.Log($"BB {id}");
				Instance.StartClient(lobby.Owner.Id);
			}
			else
			{
				Debug.Log("Failed to join steam lobby.");
				LeaveCurrentSteamLobby();
				currentSteamLobbyName = SteamClient.Name;
                //UnityEngine.Object.FindObjectOfType<MainMenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.Error, "The host has not loaded or has already landed their ship.");
            }
        }
		else
		{
			Debug.Log("Lobby error!: Attempted to join, but we are already in a Steam lobby. We should not be in a lobby while in the menu!");
			LeaveCurrentSteamLobby();
		}
	}

	private void SteamMatchmaking_OnLobbyInvite(Friend friend, Lobby lobby)
	{
		Debug.Log($"You got invited by {friend.Name} to join {lobby.Id}");
	}

	private void SteamMatchmaking_OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
		if (!steamIdsInCurrentSteamLobby.Contains(friend.Id))
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
		Debug.Log($"Player joined w steamId: {friend.Id}");
	}

	// private void SteamMatchmaking_OnLobbyEntered(Lobby lobby)
    // {
	// 	Debug.Log($"You have entered in lobby, clientId={NetworkManager.Singleton.LocalClientId}", this);

	// 	if (NetworkManager.Singleton.IsHost)
	// 		return;

	// 	StartClient(lobby.Owner.Id);
	// }

    private void SteamMatchmaking_OnLobbyCreated(Result result, Lobby lobby)
	{
		if (result != Result.OK)
        {
			Debug.LogError($"Lobby couldn't be created!, {result}", this);
			return;
		}

		lobby.SetData("name", lobbySettings.lobbyName.ToString());
		lobby.SetData("vers", Instance.gameVersionNumber.ToString());
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
		Instance.currentSteamLobby = lobby;
		currentSteamLobbyName = lobby.GetData("name");
		Debug.Log("Lobby " + currentSteamLobbyName + " has been created");
	}

	#endregion

	#region Network Callbacks

	public void SubscribeToNetworkManagerCallbacks()
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
		if (!(NetworkManager.Singleton == null))
		{
			Debug.Log("Client connected callback in gamenetworkmanager");
			if (NetworkManager.Singleton.IsServer)
			{
				totalPlayerCount++;
			}
			if (GameSessionManager.Instance != null)
			{
				GameSessionManager.Instance.OnClientConnectedGameSession(clientId);
			}
		}
	}
    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        Debug.Log("Connection approval callback! Game version of client request: " + Encoding.ASCII.GetString(request.Payload).ToString());
        Debug.Log($"Joining client id: {request.ClientNetworkId}; Local/host client id: {NetworkManager.Singleton.LocalClientId}");
        if (request.ClientNetworkId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Stopped connection approval callback, as the client in question was the host!");
            return;
        }
        bool flag = !disallowConnection;
        if (flag)
        {
            string @string = Encoding.ASCII.GetString(request.Payload);
            string[] array = @string.Split(",");
            if (string.IsNullOrEmpty(@string))
            {
                response.Reason = "Unknown; please verify your game files.";
                flag = false;
            }
            else if (Instance.totalPlayerCount >= 4)
            {
                response.Reason = "Lobby is full!";
                flag = false;
            }
            else if (Instance.gameStarted)
            {
                response.Reason = "Game has already started!";
                flag = false;
            }
            else if (Instance.gameVersionNumber.ToString() != array[0])
            {
                response.Reason = $"Game version mismatch! Their version: {gameVersionNumber}. Your version: {array[0]}";
                flag = false;
            }
            // else if (!disableSteam && *hasBeenKickedBefore*)
            // {
            // 	response.Reason = "You cannot rejoin after being kicked.";
            // 	flag = false;
            // }
        }
        else
        {
            response.Reason = "The host was not accepting connections.";
        }
        Debug.Log($"Approved connection?: {flag}. Connected players #: {Instance.totalPlayerCount}");
        Debug.Log("Disapproval reason: " + response.Reason);
        response.CreatePlayerObject = false;
        response.Approved = flag;
        response.Pending = false;
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
		Debug.Log("Disconnect callback called");
		Debug.Log($"Is server: {NetworkManager.Singleton.IsServer}; ishost: {NetworkManager.Singleton.IsHost}; isConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");
		if (NetworkManager.Singleton == null)
		{
			Debug.Log("Network singleton is null!");
			return;
		}
		if (clientId == NetworkManager.Singleton.LocalClientId && localClientJoinRequestPending)
		{
			OnLocalClientJoinRequestDisapproved(clientId);
			return;
		}

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Disconnect();
            return;
        }

        if (NetworkManager.Singleton.IsServer)
		{
			Debug.Log($"Disconnect callback called in gamenetworkmanager; disconnecting clientId: {clientId}");
            if (GameSessionManager.Instance != null && !GameSessionManager.Instance.ClientPlayerList.ContainsKey(clientId))
            {
                Debug.Log("A Player disconnected but they were not in clientplayerlist");
                return;
            }
            /*if (clientId == NetworkManager.Singleton.LocalClientId)
			{
				Debug.Log("Disconnect callback called for local client; ignoring.");
				return;
			}*/

			totalPlayerCount--;
        }

        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.OnClientDisconnectedGameSession(clientId);
        }
        Debug.Log("Disconnect callback from networkmanager in gamenetworkmanager");
	}

	private void OnLocalClientJoinRequestDisapproved(ulong clientId)
	{
		localClientJoinRequestPending = false;
		if (!string.IsNullOrEmpty(NetworkManager.Singleton.DisconnectReason))
		{
			disconnectionReasonText = NetworkManager.Singleton.DisconnectReason;
		}
		Debug.Log($"Local client connection denied; clientId: {clientId}; reason: {NetworkManager.Singleton.DisconnectReason.ToString()}");
		//UnityEngine.Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: false);
		LeaveCurrentSteamLobby();
		ResetNetworkManagerValues();
		if (NetworkManager.Singleton.IsConnectedClient)
		{
			Debug.Log("Calling shutdown(true) on server in OnLocalClientDisapproved");
			NetworkManager.Singleton.Shutdown(discardMessageQueue: true);
		}
	}
    
	private void ResetGameValues()
	{
		// if (GameSessionManager.Instance != null)
		// {
		// 	GameSessionManager.Instance.OnLocalDisconnect();
		// }
		ResetNetworkManagerValues();
	}

	private void ResetNetworkManagerValues()
	{
		isDisconnecting = false;
		disallowConnection = false;
		totalPlayerCount = 0;
		localPlayerController = null;
		gameStarted = false;
		// if (SoundManager.Instance != null)
		// {
		// 	SoundManager.Instance.ResetValues();
		// }
        UnsubscribeToNetworkManagerCallbacks();
	}

    #endregion
}

public class HostSettings
{
	public string lobbyName = "Unnamed";

	public string serverTag = "";

	public bool isLobbyPublic;

	public HostSettings(string name, bool isPublic, string setTag = "")
	{
		lobbyName = name;
		isLobbyPublic = isPublic;
		serverTag = setTag;
	}
}
