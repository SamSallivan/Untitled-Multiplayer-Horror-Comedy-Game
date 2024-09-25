using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class LaunchPod : NetworkBehaviour
{
    public List<PlayerController> readyPlayerList = new List<PlayerController>();
    public TMP_Text launchText;
    public float launchCountdown = 4f;

    void Update()
    {
        if (!GameSessionManager.Instance.gameStarted.Value)
        {
            if (readyPlayerList.Count == GameSessionManager.Instance.connectedPlayerCount)
            {
                if (launchCountdown > 0)
                {
                    launchCountdown -= Time.deltaTime;
                    launchText.text = $"Starting in {(int)launchCountdown}";
                }
                else 
                {
                    launchText.text = $"";
                    if (IsServer)
                    {
                        GameSessionManager.Instance.StartGame();
                    }
                }
            }
            else
            {
                launchCountdown = 4f;
                launchText.text = $"{readyPlayerList.Count}/{GameSessionManager.Instance.connectedPlayerCount} Ready";
            }
        }
    }
}
