using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class I_LoadScene : Interactable
{
    public string sceneName;
    public override IEnumerator InteractionEvent()
    {
        GameSessionManager.Instance.StartGame();
        yield return null; 
    }
}
