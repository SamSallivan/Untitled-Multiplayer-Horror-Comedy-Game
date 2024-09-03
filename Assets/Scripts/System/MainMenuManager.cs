using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
using Steamworks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Netcode.Transports.Facepunch;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;
	public TMP_InputField lobbyNameInputField;
	public Toggle lobbyIsPublicToggle;	
	public Toggle lobbyIsLocalToggle;	
    public TMP_InputField lobbyTagInputField;
	public TMP_Text gameVersionNumberText;
	public GameObject loadingScreenUI;
	public GameObject notificationUI;
	public TMP_Text notificationText;
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

		// if (gameVersionNumberText != null)
		// {
        //     gameVersionNumberText.text = $"v{GameNetworkManager.Instance.gameVersionNumber}";
		// }
		
	}

	public void Start()
	{
		if (!string.IsNullOrEmpty(GameNetworkManager.Instance.disconnectionReasonText))
		{
			DisplayNotification(GameNetworkManager.Instance.disconnectionReasonText ?? "", "BACK");
			GameNetworkManager.Instance.disconnectionReasonText = "";
		}
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

	public void JoinLocalButton(){
		GameNetworkManager.Instance.StartClientLAN();
	}

	public void OnLocalToggle(){
		GameNetworkManager.Instance.steamDisabled = lobbyIsLocalToggle.isOn;
	}

    public void SetLoadingScreen(bool isLoading, RoomEnter result = RoomEnter.Error, string notificationText = "")
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

	public void DisplayNotification(string notificationText, string notificationButtonText)
	{
		this.notificationText.text = notificationText;
		this.notificationButtonText.text = notificationButtonText;
		notificationUI.SetActive(true);
	}

	public void NotificationButton(){
		notificationUI.SetActive(false);
	}

}
