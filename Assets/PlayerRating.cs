using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerRating : NetworkBehaviour
{
    public NetworkVariable<float> score = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
    public NetworkVariable<Rating> rating = new NetworkVariable<Rating>(writePerm: NetworkVariableWritePermission.Owner);
    public float ratingMeter;
    public float scoreTextTimer;

    private PlayerController playerController;
    
    public RatingSettings ratingSettings;
    

    // Start is called before the first frame update
    void Start()
    {
        playerController = GetComponent<PlayerController>();
        if (IsOwner)
        {
            rating.Value = Rating.B;
            ratingMeter = 0.5f;
            score.Value = 0;
            UpdateRatingText();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (IsOwner && playerController.controlledByClient)
        {
            UIManager.instance.ratingBar.fillAmount = ratingMeter;
            UIManager.instance.scoreText.text = "Score: " + score.Value;
            
            if(scoreTextTimer > 0)
            {
                scoreTextTimer -= Time.deltaTime;
                UIManager.instance.addScoreText.alpha = scoreTextTimer;
            }
            else
            {
                UIManager.instance.addScoreText.text = "";
            }

            if (GameSessionManager.Instance.gameStarted.Value && !playerController.isPlayerExtracted.Value)
            {
                if (ratingMeter <= 0 && rating.Value == Rating.D)
                {
                    if (!playerController.isPlayerDead.Value)
                    {
                        playerController.Die();
                    }
                    return;
                }
                
                ratingMeter -= Time.deltaTime * GetMeterDropRatePerSecond();

                if(ratingMeter <= 0 && rating.Value != Rating.D)
                {
                    rating.Value--;
                    ratingMeter += 1;
                    UpdateRatingText();
                }
                else if(ratingMeter >= 1 && rating.Value != Rating.SSS)
                {
                    rating.Value++;
                    ratingMeter -= 1;
                    UpdateRatingText();
                }
            
            }
        }
        
    }
    
    public float GetMeterDropRatePerSecond()
    {
        return ratingSettings.ratings[(int)rating.Value].dropRatePerSecond;
    }
    
    public float GetScoreMultiplier()
    {
        return ratingSettings.ratings[(int)rating.Value].scoreMultiplier;
    }

    public void UpdateRatingText()
    {
        UIManager.instance.ratingText.text = ratingSettings.ratings[(int)rating.Value].ratingName;
        UIManager.instance.ratingText.color = ratingSettings.ratings[(int)rating.Value].ratingColor;
        if (GetScoreMultiplier() != 1.0f)
        {
            UIManager.instance.multiplierText.text = "x" + GetScoreMultiplier();
        }
        else
        {
            UIManager.instance.multiplierText.text = "";
        }
    }
    
    public void AddScore(float score, string message = "")
    {
        if (IsOwner&& playerController.controlledByClient)
        {
            score *= GetScoreMultiplier();
            this.score.Value += (int)score;
            ratingMeter += score/10;
            if(ratingMeter >= 1 && rating.Value == Rating.SSS)
            {
                ratingMeter = 1;
            }
            UIManager.instance.addScoreText.text = "+ " + score + " " + message;
            scoreTextTimer = 1;
        }

    }

    [System.Serializable]
    public enum Rating{
        D = 0,
        C = 1,
        B = 2,
        A = 3,
        S = 4,
        SS = 5,
        SSS = 6
    }
}

