using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "ScriptableObjects/RatingSettings", order = 4)]
[System.Serializable]
public class RatingSettings : ScriptableObject
{
    public List<RatingSetting> ratings;
}


[System.Serializable]
public struct RatingSetting
{
    public PlayerRating.Rating rating;
    public string ratingName;
    public Color ratingColor;
    public float dropRatePerSecond;
    public float scoreMultiplier;
}
