using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
using Steamworks;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;
	public TMP_InputField lobbyNameInputField;
	public Toggle lobbyIsPublicToggle;	
    public TMP_InputField lobbyTagInputField;
	public TMP_Text gameVersionNumberText;

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
			GameNetworkManager.Instance.isDisconnecting = false;
			GameNetworkManager.Instance.isHostingGame = false;
		}

		if (gameVersionNumberText != null)
		{
            gameVersionNumberText.text = $"v{GameNetworkManager.Instance.gameVersionNumber}";
		}
		
		if (string.IsNullOrEmpty(lobbyNameInputField.text))
		{
			lobbyNameInputField.text = SteamClient.Name.ToString() + "'s Lobby";
		}

	}

	public void ConfirmHostButton()
	{
		if (string.IsNullOrEmpty(lobbyNameInputField.text))
		{
			lobbyNameInputField.text = SteamClient.Name.ToString() + "'s Lobby";
		}

		if (lobbyNameInputField.text.Length > 40)
		{
			lobbyNameInputField.text = lobbyNameInputField.text.Substring(0, 40);
		}

		GameNetworkManager.Instance.lobbySettings = new HostSettings(lobbyNameInputField.text, lobbyIsPublicToggle.isOn, lobbyTagInputField.text);
		GameNetworkManager.Instance.StartHost();
    }

/*    public void SetLoadingScreen(bool isLoading, RoomEnter result = RoomEnter.Error)
    {
        Debug.Log("Displaying menu message");
        if (isLoading)
        {
            //loadingScreen.SetActive(value: true);
            return;
        }
        else
        {
            //loadingScreen.SetActive(value: false);
        }
        Debug.Log("result: " + result);
    }*/

}
