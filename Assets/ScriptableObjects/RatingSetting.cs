using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "ScriptableObjects/RatingSettings", order = 4)]
[System.Serializable]
public class RatingSettings : ScriptableObject
{
    public List<RatingSetting> ratings;
}
