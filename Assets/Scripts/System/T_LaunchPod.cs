using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class T_LaunchPod : NetworkBehaviour
{
    public LaunchPod launchPod;
    public List<PlayerController> readyPlayerList = new List<PlayerController>();

    public void Update()
    {
        if (readyPlayerList.Count == 0)
        {
            GetComponent<Renderer>().material.color = new Color(1, 0, 0, 0.25f);
        }
        else
        {
            GetComponent<Renderer>().material.color = new Color(0, 1, 0, 0.25f);
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !other.isTrigger)
        {
            readyPlayerList.Add(other.gameObject.GetComponent<PlayerController>());
            launchPod.readyPlayerList.Add(other.gameObject.GetComponent<PlayerController>());
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !other.isTrigger)
        {
            readyPlayerList.Remove(other.gameObject.GetComponent<PlayerController>());
            launchPod.readyPlayerList.Remove(other.gameObject.GetComponent<PlayerController>());
        }
    }
}
