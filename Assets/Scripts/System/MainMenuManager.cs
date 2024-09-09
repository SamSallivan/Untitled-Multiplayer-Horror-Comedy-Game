using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Steamworks;
using Sirenix.OdinInspector;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;
	public TMP_Text gameVersionNumberText;

    [FoldoutGroup("Title Page")]
	public GameObject titlePageUI;

    [FoldoutGroup("Join Page")]
	public GameObject joinPageUI;

    [FoldoutGroup("Host Page")]
	public GameObject hostPageUI;

    [FoldoutGroup("Host Page")]
	public TMP_InputField lobbyNameInputField;

    [FoldoutGroup("Host Page")]
	public Toggle lobbyIsPublicToggle;	

    [FoldoutGroup("Host Page")]
	public Toggle lobbyIsLocalToggle;	

    [FoldoutGroup("Host Page")]
    public TMP_InputField lobbyTagInputField;

    [FoldoutGroup("Loading Screen")]
	public GameObject loadingScreenUI;

    [FoldoutGroup("Notification")]
	public GameObject notificationUI;

    [FoldoutGroup("Notification")]
	public TMP_Text notificationText;

    [FoldoutGroup("Notification")]
	public TMP_Text notificationButtonText;

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

		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;

		if (GameNetworkManager.Instance != null)
		{
			GameNetworkManager.Instance.disconnecting = false;
		}

		if (gameVersionNumberText != null)
		{
            gameVersionNumberText.text = $"v{GameNetworkManager.Instance.gameVersionNumber}";
		}
		
	}

	public void Start()
	{
		if (!string.IsNullOrEmpty(GameNetworkManager.Instance.disconnectionReasonText))
		{
			DisplayNotification(GameNetworkManager.Instance.disconnectionReasonText ?? "", "BACK");
			GameNetworkManager.Instance.disconnectionReasonText = "";
		}
	}

	public void HostButton()
	{	

		titlePageUI.SetActive(false);
		hostPageUI.SetActive(true);

		if(SteamClient.IsValid && SteamClient.IsLoggedOn)
		{
			lobbyNameInputField.text = SteamClient.Name.ToString() + "'s Lobby";
		}

		OnLocalToggle();
    }

	public void JoinOnlineButton()
	{
		if (!SteamClient.IsValid || !SteamClient.IsLoggedOn)
		{
			DisplayNotification("Could not connect to Steam servers.");
			return;
		}

		titlePageUI.SetActive(false);
		joinPageUI.SetActive(true);
		GetComponent<SteamLobbyManager>().LoadServerList();
    }

	public void JoinLocalButton()
	{
		GameNetworkManager.Instance.StartClientLAN();
	}

	public void QuitButton()
	{
		Application.Quit();
	}

	public void BackButton()
	{
		joinPageUI.SetActive(false);
		hostPageUI.SetActive(false);
		titlePageUI.SetActive(true);
	}


	public void ConfirmHostButton()
	{
		if (string.IsNullOrEmpty(lobbyNameInputField.text))
		{
			if(!GameNetworkManager.Instance.steamDisabled)
			{
				lobbyNameInputField.text = SteamClient.Name.ToString() + "'s Lobby";
			}
			else
			{
				lobbyNameInputField.text = "Unnamed Lobby";
			}
		}

		if (lobbyNameInputField.text.Length > 40)
		{
			lobbyNameInputField.text = lobbyNameInputField.text.Substring(0, 40);
		}
		
		GameNetworkManager.Instance.lobbySettings = new LobbySettings(lobbyNameInputField.text, lobbyIsPublicToggle.isOn, lobbyTagInputField.text);
		GameNetworkManager.Instance.StartHost();
    }

	public void OnLocalToggle()
	{
		if (!lobbyIsLocalToggle.isOn)
		{
			if (!SteamClient.IsValid || !SteamClient.IsLoggedOn)
			{
				DisplayNotification("Could not connect to Steam servers.");
				lobbyIsLocalToggle.isOn = true;
				return;
			}
		}

		GameNetworkManager.Instance.steamDisabled = lobbyIsLocalToggle.isOn;
	}

    public void SetLoadingScreen(bool isLoading, string notificationText = "", RoomEnter result = RoomEnter.Error)
    {
		loadingScreenUI.SetActive(isLoading);

		if(isLoading)
		{
			return;
		}

		if (!string.IsNullOrEmpty(notificationText))
		{
			DisplayNotification(notificationText, "BACK");
			return;
		}

		// if (!string.IsNullOrEmpty(GameNetworkManager.Instance.disconnectionReasonText))
		// {
		// 	DisplayNotification(GameNetworkManager.Instance.disconnectionReasonText ?? "", "BACK");
		// 	GameNetworkManager.Instance.disconnectionReasonText = "";
		// 	return;
		// }

		switch (result)
		{
			case RoomEnter.Full:
				DisplayNotification("The lobby is full!", "BACK");
				break;
			case RoomEnter.DoesntExist:
				DisplayNotification("The server no longer exists!", "BACK");
				break;
			case RoomEnter.RatelimitExceeded:
				DisplayNotification("You are joining/leaving too fast!", "BACK");
				break;
			case RoomEnter.MemberBlockedYou:
				DisplayNotification("A member of the server has blocked you!", "BACK");
				break;
			case RoomEnter.Error:
				DisplayNotification("An error occured!", "BACK");
				break;
			case RoomEnter.NotAllowed:
				DisplayNotification("Connection was not approved!", "BACK");
				break;
			case RoomEnter.YouBlockedMember:
				DisplayNotification("You have blocked someone in this server!", "BACK");
				break;
			case RoomEnter.Banned:
				DisplayNotification("Unable to join because you have been banned!", "BACK");
				break;
			default:
				DisplayNotification("Something went wrong!", "BACK");
				break;
		}
		
    }

	public void DisplayNotification(string notificationText, string notificationButtonText = "Back")
	{
		this.notificationText.text = notificationText;
		this.notificationButtonText.text = notificationButtonText;
		notificationUI.SetActive(true);
	}

	public void NotificationButton(){
		notificationUI.SetActive(false);
	}

}
