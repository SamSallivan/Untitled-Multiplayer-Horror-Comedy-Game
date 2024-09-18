using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerRating : NetworkBehaviour
{
    
    public enum Rating{
        D = 0,
        C = 1,
        B = 2,
        A = 3,
        S = 4,
        SS = 5,
        SSS = 6
    }

    private NetworkVariable<float> score = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
    private NetworkVariable<Rating> rating = new NetworkVariable<Rating>(writePerm: NetworkVariableWritePermission.Owner);
    public float ratingMeter;
    public float scoreTextTimer;

    private PlayerController playerController;
    

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
        if (IsOwner&& playerController.controlledByClient)
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
    
    public float GetMeterDropRatePerSecond()
    {
        switch(rating.Value)
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
        switch(rating.Value)
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
    
    public void AddScore(int score)
    {
        AddScore(score,"");
    }
    public void AddScore(int score, string message)
    {
        if (IsOwner&& playerController.controlledByClient)
        {
            this.score.Value += score;
            ratingMeter += (float)score/100;
            if(ratingMeter >= 1 && rating.Value == Rating.SSS)
            {
                ratingMeter = 1;
            }
            UIManager.instance.addScoreText.text = "+ " + score + " " + message;
            scoreTextTimer = 1;
        }

    }

    
    
}


