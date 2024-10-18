using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(I_BearTrap))]
public class T_BearTrap : Trigger
{
    public override IEnumerator TriggerEvent()
    {
        if (GetComponent<I_BearTrap>().activated.Value || playerIds.Count < 1)
        {
            yield break;
        }
        
        PlayerController player = GameSessionManager.Instance.playerControllerList[playerIds[0]];
        GetComponent<I_BearTrap>().ActivateTrapRpc(player.localPlayerId);
    }
}
