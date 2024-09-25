using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class I_ExtractionPoint : Interactable
{
    public override IEnumerator InteractionEvent()
    {
        GameSessionManager.Instance.localPlayerController.Extract();
        GameSessionManager.Instance.localPlayerController.TeleportPlayer(GameSessionManager.Instance.playerSpawnTransform.position);
        LevelManager.Instance.CheckGameOver();
        yield return null; 
    }
}
