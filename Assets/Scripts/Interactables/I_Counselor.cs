using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class I_Counselor : Interactable
{
    public override IEnumerator InteractionEvent()
    {
        ObjectiveManager.instance.AddProgressToObjective("Counselor", 1);
        yield break;
    }
}
