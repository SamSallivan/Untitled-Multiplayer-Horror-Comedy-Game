using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "ScriptableObjects/EmoteData", order = 5)]
[System.Serializable]
public class ObjectiveData : ScriptableObject
{
    public string objectiveName;
    public string objectiveText;
    public string triggerEvent;
    public int requiredValue = 1;
    public int score = 0;
    public ObjectiveData followupObjectiveData;
    public float followupObjectiveAssignDelay = 4f;
}
