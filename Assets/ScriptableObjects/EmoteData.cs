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
    
    [Header("Rotation")]
    [ShowIf("fullBodyAnimation")] public bool lockLookRotation;
    [ShowIf("fullBodyAnimation")] public bool lockBodyRotation;
    
    [Header("Camera")]
    [ShowIf("fullBodyAnimation")] public bool overrideCameraPosition;
    [ShowIf("@fullBodyAnimation && overrideCameraPosition")] public Vector3 targetCameraPosition;
    
    [Header("Arms Animation")]
    [ShowIf("fullBodyAnimation")] public bool overrideArmAnimation;
    [Header("Arms Animation")]
    [ShowIf("@!fullBodyAnimation")] public bool leftArmAnimation;
    [ShowIf("@!fullBodyAnimation")] public bool rightArmAnimation;
}