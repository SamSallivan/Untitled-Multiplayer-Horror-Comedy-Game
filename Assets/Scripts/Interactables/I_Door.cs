using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class I_Door : Interactable
{
    private NetworkVariable<bool> open = new NetworkVariable<bool>();
    public Vector3 openRotation;
    public Vector3 closedRotation;

    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            open.Value = false;
        }

        open.OnValueChanged += OnStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        open.OnValueChanged -= OnStateChanged;
    }

    public void OnStateChanged(bool previous, bool current)
    {
        // note: `State.Value` will be equal to `current` here
        if (open.Value)
        {
            
            activated = true;
            transform.localEulerAngles = openRotation;
        }
        else
        {
            activated = false;
            transform.localEulerAngles = closedRotation;
        }
    }
    public override IEnumerator InteractionEvent()
    {
        ToggleDoorServerRPC();
        
        yield return null; 
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleDoorServerRPC()
    {
        open.Value = !open.Value;
    }
}
