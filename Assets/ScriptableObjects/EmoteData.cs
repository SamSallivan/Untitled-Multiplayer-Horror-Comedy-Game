using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


[CreateAssetMenu(menuName = "ScriptableObjects/EmoteData", order = 3)]
[System.Serializable]
public class EmoteData : ScriptableObject
{
    public string name;
    public string animatorTrigger;
    public bool fullBodyAnimation;
    [ShowIf("fullBodyAnimation")] public bool lockLookRotation;
    [ShowIf("fullBodyAnimation")] public bool overrideArmAnimation;
    [HideIf("fullBodyAnimation")] public bool leftArmAnimation;
    [HideIf("fullBodyAnimation")] public bool rightArmAnimation;
}