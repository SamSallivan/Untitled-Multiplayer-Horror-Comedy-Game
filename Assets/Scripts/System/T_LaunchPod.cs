using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class T_LaunchPod : NetworkBehaviour
{
    public List<PlayerController> readyPlayerCount = new List<PlayerController>();

    void Update()
    {
        if (IsServer)
        {
            if (!GameSessionManager.Instance.gameStarted)
            {
                if (readyPlayerCount.Count == GameSessionManager.Instance.connectedPlayerCount)
                {
                    GameSessionManager.Instance.StartGame();
                }
            }
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log(other.name);
            readyPlayerCount.Add(other.gameObject.GetComponent<PlayerController>());
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            readyPlayerCount.Remove(other.gameObject.GetComponent<PlayerController>());
        }
    }
}
