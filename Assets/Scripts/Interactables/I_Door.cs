using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class I_Door : Interactable
{
    public NetworkVariable<bool> open = new NetworkVariable<bool>();
    public Transform doorTransform;
    public Vector3 openRotation;
    public Vector3 closedRotation;
    public float doorRotationInterpolationSpeed = 5f;
    public float delay = 0f;
    
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
        yield return new WaitForSeconds(delay);
        
        ToggleDoorServerRPC();
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
        }
        else
        {
            activated = false;
        }
    }

    public override void InteractableUpdate()
    {
        if (open.Value)
        {
            if (doorTransform != null)
            {
                doorTransform.localRotation = Quaternion.Lerp(doorTransform.localRotation, Quaternion.Euler(openRotation), Time.deltaTime * doorRotationInterpolationSpeed);
            }
            else
            {
                transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(openRotation), Time.deltaTime * doorRotationInterpolationSpeed);
            }
        }
        else
        {
            if (doorTransform != null)
            {
                doorTransform.localRotation = Quaternion.Lerp(doorTransform.localRotation, Quaternion.Euler(closedRotation), Time.deltaTime * doorRotationInterpolationSpeed);
            }
            else
            {
                transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(closedRotation), Time.deltaTime * doorRotationInterpolationSpeed);
            }
        }
    }
}
