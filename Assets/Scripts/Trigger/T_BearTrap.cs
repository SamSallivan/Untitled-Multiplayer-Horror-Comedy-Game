using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(I_BearTrap))]
public class T_BearTrap : Trigger
{
    public override IEnumerator TriggerEvent()
    {
        if(colliderGameObject.TryGetComponent<PlayerController>(out PlayerController player))
        GetComponent<I_BearTrap>().ActivateTrapRpc(player.localPlayerId);
        yield break;
    }
}
