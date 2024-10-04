using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using Unity.Netcode;
using UnityEngine;
using Dissonance.Integrations.Unity_NFGO;
using Mono.CSharp;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;


public class GameSessionManager : NetworkBehaviour
{
   public static GameSessionManager Instance { get; private set; }
  
   [Header("References")]
   public List<PlayerController> playerControllerList = new List<PlayerController>();
   
   public PlayerController localPlayerController;

   public ItemList itemList;
  
   public Transform lobbySpawnTransform;
  
   public Transform lobbyDespawnTransform;
  
   public AudioListener audioListener;
  
   public List<Transform> lobbySummaryPlayerTransformList = new List<Transform>();
   
   [Header("Values")]
   public NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(writePerm: NetworkVariableWritePermission.Owner);
   
   public bool hasHostSpawned;
  
   public NetworkVariable<int> connectedPlayerCount = new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Owner);
   
   public Dictionary<ulong, int> ClientIdToPlayerIdDictionary = new Dictionary<ulong, int>();
  
   //public NetworkList<ulong> loadedClientIdList;
  
   public bool loaded;
   

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
     
      //loadedClientIdList = new NetworkList<ulong>();
   }

   private void Start()
   {
      NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>(includeInactive: true);
      foreach(NetworkObject networkObject in networkObjects)
      {
         networkObject.DontDestroyWithOwner = true;
      }
   }


   public void OnEnable()
   {
      NetworkManager.Singleton.SceneManager.OnLoadComplete += SceneManager_OnLoadComplete;
      //NetworkManager.Singleton.SceneManager.OnLoad += SceneManager_OnLoad;
      NetworkManager.Singleton.SceneManager.OnUnloadComplete += SceneManager_OnUnloadComplete;
   }
  
   /*private void SceneManager_OnLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
   {
      if (sceneName == "MainMenu" || sceneName == "Lobby")
      {
         return;
      }
     
      //else update current level variable
   }*/
  
   private void SceneManager_OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
   {
      loaded = true;
   }


   private void SceneManager_OnUnloadComplete(ulong clientId, string sceneNam)
   {
      loaded = true;
   }


   private void Update()
   {
      if (base.IsServer && !hasHostSpawned)
      {
         OnHostConnectedGameSession();
         hasHostSpawned = true;
      }
      
   }
  
   #region Save&Load
   
   public class InventoryItemSaveData
   {
      public int index = -1;
      public int amount;
      public float durability;
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
                  list[i].durability = InventoryManager.instance.storageSlotList[i].inventoryItem.itemStatus.durability;
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
      
      ES3.Save("BaseCurrencyBalanceSaveData", localPlayerController.baseCurrencyBalance);
   }


   [Button("Load")]
   public void Load()
   {
      InventoryItemSaveData[] list = ES3.Load<InventoryItemSaveData[]>("StorageSlotSaveData");
      
      for(int i = 0; i < list.Length; i++)
      {
         if(list[i].index != -1)
         {
            InventoryManager.instance.InstantiatePocketedItemServerRpc(list[i].index, list[i].amount, list[i].durability, i, localPlayerController.localPlayerId);
         }
      }
      
      localPlayerController.baseCurrencyBalance = ES3.Load<int>("BaseCurrencyBalanceSaveData", 0);
   }


   public IEnumerator LoadCoroutine()
   {
      yield return new WaitUntil(() =>  localPlayerController != null && localPlayerController.controlledByClient.Value);
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
         Debug.Log($"OnHostConnectedGameSession: Connecting host.");
         
         
         PlayerController playerController = playerControllerList[0];
         playerController.GetComponent<NetworkObject>().ChangeOwnership(NetworkManager.Singleton.LocalClientId);
         playerController.GetComponent<PlayerController>().localClientId.Value = NetworkManager.Singleton.LocalClientId;
         playerController.GetComponent<PlayerController>().controlledByClient.Value = true;
         ClientIdToPlayerIdDictionary.Add(NetworkManager.Singleton.LocalClientId, 0);
         connectedPlayerCount.Value = ClientIdToPlayerIdDictionary.Count;


         if (!GameNetworkManager.Instance.isSteamDisabled)
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
         
         
         //Find the first available player controller
         List<int> connectedPlayerIdList = ClientIdToPlayerIdDictionary.Values.ToList();
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
         playerController.GetComponent<NetworkObject>().ChangeOwnership(clientId);
         playerController.localClientId.Value = clientId;
         playerController.controlledByClient.Value = true;
         ClientIdToPlayerIdDictionary.Add(clientId, targetPlayerId);
         connectedPlayerCount.Value = ClientIdToPlayerIdDictionary.Count;
         OnClientConnectedGameSessionClientRpc(clientId, targetPlayerId);//, ClientIdToPlayerIdDictionary.Keys.ToArray(), ClientIdToPlayerIdDictionary.Values.ToArray());


         Debug.Log($"OnClientConnectedGameSession: New client {clientId} assigned PlayerController: {playerController}");


      }
      catch (Exception arg)
      {
         Debug.LogError($"OnClientConnectedGameSession: Error: {arg}. Shutting server down. clientId: {clientId}.");
         GameNetworkManager.Instance.Disconnect();
      }


   }


   [Rpc(SendTo.Everyone)]
   private void OnClientConnectedGameSessionClientRpc(ulong clientId, int connectingPlayerId)//, ulong[] connectedClientIds, int[] connectedPlayerIds)
   {
      try
      {
         Debug.Log($"OnClientConnectedGameSessionClientRpc: Connecting clientId: {clientId}");


         if (clientId == NetworkManager.Singleton.LocalClientId && GameNetworkManager.Instance.localClientJoinRequestPending)
         {
            GameNetworkManager.Instance.localClientJoinRequestPending = false;
         }


         //Reconstructing ClientIdToPlayerIdDictionary on all clients
         /*if (!base.IsServer)
         {
            ClientIdToPlayerIdDictionary.Clear();
            for (int i = 0; i < connectedClientIds.Length; i++)
            {
               ClientIdToPlayerIdDictionary.Add(connectedClientIds[i], connectedPlayerIds[i]);
            }
         }*/


         PlayerController playerController = playerControllerList[connectingPlayerId];
        
         //Reinitialize NfgoPlayer Component Tracking for every PlayerController on all clients
         foreach (PlayerController playerController1 in playerControllerList)
         {
            if (playerController1.GetComponent<NfgoPlayer>()) // && !playerController1.GetComponent<NfgoPlayer>().IsTracking)
            {
               playerController1.GetComponent<NfgoPlayer>().InitializeVoiceChatTracking();
            }
         }

         
         if (NetworkManager.Singleton.LocalClientId == clientId)
         { 
            
         }
         else
         {
            if (localPlayerController.currentEquippedItem != null)
            {
               localPlayerController.currentEquippedItem.EquipItemRpc(localPlayerController.localPlayerId);
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
       
      if (!ClientIdToPlayerIdDictionary.ContainsKey(clientId)) 
      {
         return;
      }
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


       if (!IsServer)
       {
          if (disconnectingPlayerId == 0 && !GameNetworkManager.Instance.isDisconnecting)
          { 
             Debug.Log("OnClientDisconnectedGameSession: Host disconnected. Returning to Main Menu."); 
             GameNetworkManager.Instance.Disconnect("Host Disconnected."); 
          }
          return;
       }


       PlayerController playerController = playerControllerList[disconnectingPlayerId];
       playerController.gameObject.GetComponent<NetworkObject>().RemoveOwnership(); 
       playerController.controlledByClient.Value = false;
       playerController.TeleportPlayer(lobbyDespawnTransform.position);
       ClientIdToPlayerIdDictionary.Remove(clientId);
       connectedPlayerCount.Value = ClientIdToPlayerIdDictionary.Count;
       
       OnClientDisconnectedGameSessionClientRpc(clientId, disconnectingPlayerId);//, ClientIdToPlayerIdDictionary.Keys.ToArray(), ClientIdToPlayerIdDictionary.Values.ToArray());
       
        //if (loadedClientIdList.Contains(clientId))
        //{
            //RemoveLoadedClientIdsRpc(clientId);
        //}
   }


   [Rpc(SendTo.Everyone)]
   public void OnClientDisconnectedGameSessionClientRpc(ulong clientId, int disconnectingPlayerId)//, ulong[] connectedClientIds, int[] connectedPlayerIds)
   {
      /*if (!ClientIdToPlayerIdDictionary.ContainsKey(clientId))
      {
           Debug.Log("OnClientDisconnectClientRpc: Target clientId key already removed, ignoring");
           return;
      }*/
      if (localPlayerController != null && disconnectingPlayerId == localPlayerController.localPlayerId)
      {
         Debug.Log("OnClientDisconnectClientRpc: Local client disconnecting, ignoring");
         return;
      }
      if (NetworkManager.ShutdownInProgress || NetworkManager.Singleton == null)
      { 
         Debug.Log("OnClientDisconnectClientRpc: Shutdown in progress, returning");
         return;
      }

      
      //Reconstructing ClientIdToPlayerIdDictionary on all clients
      /*if (!base.IsServer)
      {
         ClientIdToPlayerIdDictionary.Clear();
         for (int i = 0; i < connectedClientIds.Length; i++)
         {
            ClientIdToPlayerIdDictionary.Add(connectedClientIds[i], connectedPlayerIds[i]);
         }
      }*/


      //Reset PlayerController values
      try
      {
         PlayerController playerController = playerControllerList[disconnectingPlayerId].GetComponent<PlayerController>();
         Destroy(playerController.playerVoiceChatPlaybackObject.gameObject);
         playerController.GetComponent<NfgoPlayer>().StopTracking();
         Debug.Log($"OnClientDisconnectedGameSessionClientRpc: Client #{clientId} disconnected.");
      }
      catch (Exception arg)
      {
         Debug.LogError($"OnClientDisconnectedGameSessionClientRpc: Error while handling player disconnect: {arg}");
      }
   }


   #endregion


   [Button]
   public void StartGame()
   {
      /*if (loadedClientIdList.Count < connectedPlayerCount)
      {
         return;
      }*/
     
      if (!gameStarted.Value)
      {
         gameStarted.Value = true;
      }
      //ClearLoadedClientIdsRpc();
     
      StartGameRpc();
   }


   [Rpc(SendTo.Everyone)]
   public void StartGameRpc()
   {
      StartCoroutine(StartGameCoroutine());
   }


   IEnumerator StartGameCoroutine()
   {
      loaded = false;
      if (IsServer)
      {
         NetworkManager.SceneManager.LoadScene("Level 1", LoadSceneMode.Additive);
      }
     
      yield return new WaitUntil(() => loaded == true && LevelManager.Instance != null);
      yield return new WaitForSeconds(1f);
      
      localPlayerController.TeleportPlayer(LevelManager.Instance.playerSpawnTransform.position);
      UIManager.instance.extractionUI.SetActive(true);
      UIManager.instance.ratingUI.SetActive(true);
      UIManager.instance.currencyUI.SetActive(false);
     
      if (!GameNetworkManager.Instance.isSteamDisabled)
      {
         SteamFriends.SetRichPresence("steam_player_group", GameNetworkManager.Instance.currentSteamLobbyName);
         SteamFriends.SetRichPresence("steam_player_group_size", connectedPlayerCount.Value.ToString());
         SteamFriends.SetRichPresence("steam_display", "Running the show.");
      }
   }


   [Button]
   public void EndGame()
   {
      /*if (loadedClientIdList.Count < connectedPlayerCount)
      {
         return;
      }*/
     
      if (gameStarted.Value)
      {
         gameStarted.Value = false;
      }
      //ClearLoadedClientIdsRpc();
     
      EndGameRpc();
   }


   [Rpc(SendTo.Everyone)]
   public void EndGameRpc()
   {
      StartCoroutine(EndGameRpcCoroutine());
   }


   IEnumerator EndGameRpcCoroutine()
   {
      if (!localPlayerController.isPlayerExtracted.Value)
      {
         if (localPlayerController.controlledByClient.Value && !localPlayerController.isPlayerDead.Value)
         {
            localPlayerController.Die();
         }
      }
      
      UIManager.instance.extractionUI.SetActive(false);
      UIManager.instance.ratingUI.SetActive(false);
      UIManager.instance.currencyUI.SetActive(true);
     
      localPlayerController.TeleportPlayer(GameSessionManager.Instance.lobbySummaryPlayerTransformList[GameSessionManager.Instance.localPlayerController.localPlayerId].position);
     
      yield return new WaitForSeconds(1f);
      if (localPlayerController.isPlayerDead.Value)
      {
         localPlayerController.Respawn();
      }


      if (localPlayerController.isPlayerExtracted.Value)
      {
         localPlayerController.Unextract();
      }


      yield return new WaitForSeconds(1f);
     
      //GameSessionManager.Instance.localPlayerController.LockMovement(true);
      //GameSessionManager.Instance.localPlayerController.LockCamera(true);
      localPlayerController.ResetCamera();
      UIManager.instance.OpenSummary();
     
      loaded = false;
      if (IsServer)
      {
         base.NetworkManager.SceneManager.UnloadScene(SceneManager.GetSceneAt(1));
      }
      yield return new WaitUntil(() => loaded == true);


      if (!GameNetworkManager.Instance.isSteamDisabled)
      {
         SteamFriends.SetRichPresence("steam_player_group", GameNetworkManager.Instance.currentSteamLobbyName);
         SteamFriends.SetRichPresence("steam_player_group_size", connectedPlayerCount.Value.ToString());
         SteamFriends.SetRichPresence("text", "Preparing the show.");
      }
   }
}

