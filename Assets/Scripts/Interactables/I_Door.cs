using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class I_Door : Interactable
{
    public Transform doorTransform;
    public Vector3 openRotation;
    public Vector3 closedRotation;
    public float doorRotationInterpolationSpeed = 5f;
    public float delay = 0f;
    public I_Door flipSideDoor;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    public override IEnumerator InteractionEvent()
    {
        yield return new WaitForSeconds(delay);
        
        ToggleDoorServerRPC();
    }

    [Rpc(SendTo.Server)]
    public void ToggleDoorServerRPC()
    {
        activated.Value = !activated.Value;

        if (flipSideDoor)
        {
            flipSideDoor.activated.Value = !flipSideDoor.activated.Value;
        }
    }

    public override void InteractableUpdate()
    {
        if (activated.Value)
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
