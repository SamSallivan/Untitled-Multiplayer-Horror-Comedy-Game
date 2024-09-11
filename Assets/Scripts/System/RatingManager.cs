using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RatingManager : MonoBehaviour
{
    public static RatingManager instance;
    public enum Rating{
        D = 0,
        C = 1,
        B = 2,
        A = 3,
        S = 4,
        SS = 5,
        SSS = 6
    }

    public Rating rating;
    public float ratingMeter;
    public int score;
    public float scoreTextTimer;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        rating = Rating.B;
        ratingMeter = 0.5f;
        score = 0;
        UpdateRatingText();
    }

    void Update()
    {
        if(scoreTextTimer > 0)
        {
            scoreTextTimer -= Time.deltaTime;
        }
        else
        {
            UIManager.instance.addScoreText.text = "";
        }

        ratingMeter -= Time.deltaTime * GetMeterDropRatePerSecond();
        UIManager.instance.ratingBar.fillAmount = ratingMeter;

        if(ratingMeter <= 0 && rating != Rating.D)
        {
            rating--;
            ratingMeter += 1;
            UpdateRatingText();
        }
        else if(ratingMeter >= 1 && rating != Rating.SSS)
        {
            rating++;
            ratingMeter -= 1;
            UpdateRatingText();
        }
    }

    public void AddScore(int score)
    {
        this.score += score;
        ratingMeter += (float)score/100;
        if(ratingMeter >= 1 && rating == Rating.SSS)
        {
            ratingMeter = 1;
        }
        UIManager.instance.addScoreText.text = "+ " + score;
        scoreTextTimer = 1;
    }

    public float GetMeterDropRatePerSecond()
    {
        switch(rating)
        {
            case Rating.D:
                return 0.033f;

            case Rating.C:
                return 0.033f;
                
            case Rating.B:
                return 0.033f;
                
            case Rating.A:
                return 0.033f;
                
            case Rating.S:
                return 0.033f;
                
            case Rating.SS:
                return 0.033f;
                
            case Rating.SSS:
                return 0.033f;

            default:
                return 0.033f;
        }
    }

    public void UpdateRatingText()
    {
        switch(rating)
        {
            case Rating.D:
                UIManager.instance.ratingText.text = "Dismal";
                break;

            case Rating.C:
                UIManager.instance.ratingText.text = "Cringe";
                break;
                
            case Rating.B:
                UIManager.instance.ratingText.text = "Bland";
                break;
                
            case Rating.A:
                UIManager.instance.ratingText.text = "Alright!";
                break;
                
            case Rating.S:
                UIManager.instance.ratingText.text = "Showtime!";
                break;
                
            case Rating.SS:
                UIManager.instance.ratingText.text = "Super Star!";
                break;
                
            case Rating.SSS:
                UIManager.instance.ratingText.text = "Supreme Showbiz Star!";
                break;
        }
    }
}
