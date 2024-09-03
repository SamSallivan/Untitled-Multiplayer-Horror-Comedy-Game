using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;


public class SteamLobbySlot : MonoBehaviour
{
    public MainMenuManager mainMenuManager;


    public TextMeshProUGUI LobbyName;


    public TextMeshProUGUI playerCount;


    public SteamId lobbyId;


    public Lobby thisLobby;


    private static Coroutine timeOutLobbyRefreshCoroutine;


    private void Awake()
    {
        mainMenuManager = Object.FindObjectOfType<MainMenuManager>();
    }


    public void JoinButton()
    {
        if (!GameNetworkManager.Instance.waitingForLobbyDataRefresh)
        {
            GameNetworkManager.Instance.VerifyLobbyJoinRequest(thisLobby, lobbyId);
        }
    }
}
