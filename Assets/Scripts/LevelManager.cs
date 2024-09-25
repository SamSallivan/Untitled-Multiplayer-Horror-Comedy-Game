using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; } = null;
    
    private void Awake()
    {
        if (Instance == null){
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    [FoldoutGroup("Match Time")]
    [SerializeField] 
    float preExtractionTime = 300f;
    [FoldoutGroup("Match Time")]
    [SerializeField] 
    float extractionTime = 60f;
    [FoldoutGroup("Match Time")]
    public NetworkVariable<float> matchTimer = new NetworkVariable<float>(0);

    public List<GameObject> ExtractionLocations;

    public bool gameOver = false;
    
    public Transform playerSpawnTransform;
    

    public enum GameState
    {
        NotStarted,
        PreExtraction,
        Extraction,
        Finished
    }

    public NetworkVariable<GameState> currentGameState = new NetworkVariable<GameState>(GameState.NotStarted);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentGameState.Value = GameState.NotStarted;
            matchTimer.Value = preExtractionTime;
        }
        else
        {
            currentGameState.OnValueChanged += OnStateChanged;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer)
        {
            return;
        }

        if (currentGameState.Value == GameState.NotStarted)
        {
            //temp just start game
            currentGameState.Value = GameState.PreExtraction;
        }
        else if (currentGameState.Value == GameState.PreExtraction)
        {
            matchTimer.Value -= Time.deltaTime;
            if (matchTimer.Value <= 0)
            {
                matchTimer.Value = extractionTime;
                currentGameState.Value = GameState.Extraction;
                foreach (GameObject location in ExtractionLocations)
                {
                    location.SetActive(true);
                }
            }
        }
        else if (currentGameState.Value == GameState.Extraction)
        {
            matchTimer.Value -= Time.deltaTime;
            if (matchTimer.Value <= 0)
            {
                matchTimer.Value = 0;
                currentGameState.Value = GameState.Finished;
            }
        }
        else if (currentGameState.Value == GameState.Finished)
        {
            
        }
    }

    public void OnStateChanged(GameState previous, GameState current)
    {
        if (current == GameState.Finished)
        {
            EndGame();
        }
    }

    public void EndGame()
    {
        foreach (var p in GameSessionManager.Instance.playerControllerList)
        {
            if (p.controlledByClient && !p.isPlayerDead.Value)
            {
                p.Die();
            }
        }
    }
}
