using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; } = null;
    
    [FoldoutGroup("Match Time")]
    [SerializeField] 
    float preExtractionTime = 300f;
    
    [FoldoutGroup("Match Time")]
    [SerializeField] 
    float extractionTime = 60f;
    
    [FoldoutGroup("Match Time")]
    public NetworkVariable<float> matchTimer = new NetworkVariable<float>(0);

    public List<GameObject> ExtractionLocations;
    
    public Transform playerSpawnTransform;

    [FoldoutGroup("Monster Spawning")] public LevelSpawnData levelSpawnData;
    [FoldoutGroup("Monster Spawning")] public Transform spawnLocations;
    [FoldoutGroup("Monster Spawning")] public float monsterSpawnTimer = 0;
    public int spawnIndex = 0;

    public enum GameState
    {
        NotStarted,
        PreExtraction,
        Extraction,
        Finished
    }

    public NetworkVariable<GameState> currentGameState = new NetworkVariable<GameState>(GameState.NotStarted);
    
    private void Awake()
    {
        if (Instance == null){
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentGameState.Value = GameState.NotStarted;

            StartCoroutine(RespawnScenePlacedInventoryItemsCoroutine());
        }
        currentGameState.OnValueChanged += OnStateChanged;
    }

    IEnumerator RespawnScenePlacedInventoryItemsCoroutine()
    {
        yield return new WaitForSeconds(2.5f);
            
        I_InventoryItem[] inventoryItems = FindObjectsOfType<I_InventoryItem>();
        foreach (I_InventoryItem inventoryItem in inventoryItems)
        {
            if (inventoryItem.owner == null)
            {
                var gameObject = Instantiate(inventoryItem.itemData.dropObject, inventoryItem.transform.position, inventoryItem.transform.rotation);
                gameObject.GetComponent<NetworkObject>().Spawn();
                inventoryItem.NetworkObject.Despawn();
                SceneManager.MoveGameObjectToScene(gameObject.gameObject, SceneManager.GetSceneAt(1));
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            I_InventoryItem[] inventoryItems = FindObjectsOfType<I_InventoryItem>();
            foreach (I_InventoryItem inventoryItem in inventoryItems)
            {
                if (inventoryItem.owner == null)
                {
                    inventoryItem.NetworkObject.Despawn();
                }
            }
            
            NightCrawler[] nightCrawlers = FindObjectsOfType<NightCrawler>();
            foreach (NightCrawler nightCrawler in nightCrawlers)
            {
                nightCrawler.NetworkObject.Despawn();
            }
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

        CheckGameOver();
        
        if (currentGameState.Value == GameState.NotStarted)
        {
            //temp just start game
            //matchTimer.Value = preExtractionTime;
            //currentGameState.Value = GameState.PreExtraction;
        }
        else if (currentGameState.Value == GameState.PreExtraction)
        {
            matchTimer.Value -= Time.deltaTime;
            SpawnMonsters();
            if (matchTimer.Value <= 0)
            {
                BeginExtraction();
            }
        }
        else if (currentGameState.Value == GameState.Extraction)
        {
            matchTimer.Value -= Time.deltaTime;
            SpawnMonsters();
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
        if (current == GameState.PreExtraction)
        {
            if (IsServer)
            {
                monsterSpawnTimer = levelSpawnData.spawnData[spawnIndex].spawnTime;
            }
        }
        if (current == GameState.Finished)
        {
            if (IsServer)
            {
                EndGame();
            }
        }
    }

    public void BeginExtraction()
    {
        matchTimer.Value = extractionTime;
        currentGameState.Value = GameState.Extraction;
        ActivateExtractRPC();
    }

    public void EndGame()
    {
        GameSessionManager.Instance.EndGame();
    }
    public void KillAllPLayers()
    {
        foreach (var p in GameSessionManager.Instance.playerControllerList)
        {
            if (p.controlledByClient.Value && !p.isPlayerDead.Value)
            {
                p.Die();
            }
        }
    }
    
    public void CheckGameOver()
    {
        foreach (PlayerController pc in GameSessionManager.Instance.playerControllerList)
        {
            if (pc.controlledByClient.Value)
            {
                if (!pc.isPlayerDead.Value && !pc.isPlayerExtracted.Value)
                {
                    return;
                }
            }
            
        }

        currentGameState.Value = GameState.Finished;
    }

    public void SpawnMonsters()
    {
        if (monsterSpawnTimer <= 0&& spawnIndex<levelSpawnData.spawnData.Count)
        {
            LevelSpawnData.SpawnData sdata = levelSpawnData.spawnData[spawnIndex];
            
            GameObject monsterToSpawn;
            switch (sdata.mType)
            {
                case LevelSpawnData.MonsterType.NightCrawler:
                    monsterToSpawn = levelSpawnData.monsters[0];
                    break;
                default:
                    monsterToSpawn = null;
                    break;
            }

            if (monsterToSpawn != null)
            {
                var instance = Instantiate(monsterToSpawn);
                var instanceNetworkObject = instance.GetComponent<NetworkObject>();
                SceneManager.MoveGameObjectToScene(instance,SceneManager.GetSceneAt(1));
                instance.transform.position = spawnLocations.transform.GetChild(sdata.spawnLocationIndex).position;
                instanceNetworkObject.Spawn();
                

            }
            spawnIndex++;
            if (spawnIndex < levelSpawnData.spawnData.Count)
            {
                monsterSpawnTimer = levelSpawnData.spawnData[spawnIndex].spawnTime;
            }
        }
        else
        {
            monsterSpawnTimer -= Time.deltaTime;
        }
    }

    [Rpc(SendTo.Everyone)]
    public void ActivateExtractRPC()
    {
        foreach (GameObject location in ExtractionLocations)
        {
            location.SetActive(true);
        }
    }
    
}
