using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class O_Deposit : Objective
{

    public override IEnumerator OnObjectiveCompletedCoroutine()
    {
        if (LevelManager.Instance)
        {
            LevelManager.Instance.BeginExtraction();
        }
        yield return null;
    }

    public override IEnumerator OnObjectiveFailedCoroutine()
    {
        if (LevelManager.Instance)
        {
            LevelManager.Instance.BeginExtraction();
        }
        yield return null;
    }
}
