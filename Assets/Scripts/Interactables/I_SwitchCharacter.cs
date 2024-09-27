using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class I_SwitchCharacter : Interactable
{
    public override IEnumerator InteractionEvent()
    {
        //PlayerAnimationController controller = GameSessionManager.Instance.localPlayerController.playerAnimationController;
        GameSessionManager.Instance.localPlayerController.SwitchCharacterModel();
        yield break;
    }
}
