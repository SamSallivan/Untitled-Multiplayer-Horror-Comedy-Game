using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RatingManager : MonoBehaviour
{
    public static RatingManager instance;
    
    
    public PlayerController playerController;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {

    }

    void Update()
    {
        if (!playerController)
        {
            if(GameSessionManager.Instance.localPlayerController)
            {
                playerController = GameSessionManager.Instance.localPlayerController;
            }
        }
    }

    public void AddScore(int score)
    {
        playerController.GetComponent<PlayerRating>().AddScore(score);
    }
    public void AddScore(int score,PlayerController player)
    {
        player.GetComponent<PlayerRating>().AddScore(score);
    }
    
    public void AddScore(int score,int playerID)
    {
        GameSessionManager.Instance.playerControllerList[playerID].GetComponent<PlayerRating>().AddScore(score);
    }
    
    
    




}
