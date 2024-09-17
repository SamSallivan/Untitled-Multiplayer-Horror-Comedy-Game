using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class I_Door : Interactable
{
    private NetworkVariable<bool> open = new NetworkVariable<bool>();
    public Vector3 openRotation;
    public Vector3 closedRotation;
    public float doorRotationInterpolationSpeed = 5f;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            open.Value = false;
        }

        open.OnValueChanged += OnOpenChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        open.OnValueChanged -= OnOpenChanged;
    }

    public override IEnumerator InteractionEvent()
    {
        ToggleDoorServerRPC();
        
        yield return null; 
    }

    [Rpc(SendTo.Server)]
    public void ToggleDoorServerRPC()
    {
        open.Value = !open.Value;
    }

    public void OnOpenChanged(bool previous, bool current)
    {
        // note: `State.Value` will be equal to `current` here
        if (open.Value)
        {
            
            activated = true;
            //transform.localEulerAngles = openRotation;
        }
        else
        {
            activated = false;
            //transform.localEulerAngles = closedRotation;
        }
    }

    public void Update()
    {
        if (open.Value)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(openRotation), Time.deltaTime * doorRotationInterpolationSpeed);
        }
        else
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(closedRotation), Time.deltaTime * doorRotationInterpolationSpeed);
        }
    }
}
