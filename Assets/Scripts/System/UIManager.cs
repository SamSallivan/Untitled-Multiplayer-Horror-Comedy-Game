using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [FoldoutGroup("Gameplay")]
    public GameObject gameplayUI;

    [FoldoutGroup("Gameplay")]
    public GameObject generalTips;

    [FoldoutGroup("Gameplay")]
    public GameObject notificationUI;

    // [Header("Objective")]
    // public GameObject objectiveUI;
    // public GameObject objectiveUIAnim;

    [FoldoutGroup("Gameplay")]
    public TMP_Text interactionName;

    [FoldoutGroup("Gameplay")]
    public TMP_Text interactionPrompt;

    [FoldoutGroup("Gameplay")]
    public Animation interactionPromptAnimation;
    
    [FoldoutGroup("Gameplay")]
    public Image healthBar;
    
    [FoldoutGroup("Gameplay")]
    public Image staminaBar;

    [FoldoutGroup("Score & Rating")]
    public GameObject ratingUI;

    [FoldoutGroup("Score & Rating")]
    public TMP_Text ratingText;
    
    [FoldoutGroup("Score & Rating")]
    public Image ratingBar;

    [FoldoutGroup("Score & Rating")]
    public TMP_Text scoreText;

    [FoldoutGroup("Score & Rating")]
    public TMP_Text addScoreText;
    
    [FoldoutGroup("Extraction")]
    public TMP_Text timerText;

    // [Header("Subtitle")]
    //     public TMP_Text radioSubtitleUI;
    //     public TMP_Text dialogueSubtitleUI;

    // [Header("Fishing")]
    //     public GameObject fishingUI;
    //     public Slider reelSlider;
    //     public Image reelImage;

    [FoldoutGroup("Inventory")]
    public GameObject inventoryUI;

    [FoldoutGroup("Inventory")]
    public GameObject detailPanel;

    [FoldoutGroup("Inventory")]
    public GameObject StoragePanel;

    [FoldoutGroup("Inventory")]
    public GameObject inventorySlotGrid;

    [FoldoutGroup("Inventory")]
    public GameObject storageSlotGrid;

    [FoldoutGroup("Inventory")]
    public GameObject inventoryTypeGrid;
    
    [FoldoutGroup("Inventory")]
    public TMP_Text detailName;

    [FoldoutGroup("Inventory")]
    public TMP_Text detailDescription;

    [FoldoutGroup("Inventory")]
    public Transform detailObjectPivot;

    [FoldoutGroup("Inventory")]
    public bool detailObjectInBound;

    [FoldoutGroup("Inventory")]
    public GameObject draggedItemDisplay;

    [FoldoutGroup("Inventory")]
    public Image draggedImage;

    // [Foldout("Upgrade", true)]
    // public GameObject upgradeUI;
    // public TMP_Text upgradeTitle;
    // public GameObject UpgradeOptionList;

    /*[FoldoutGroup("Examine")]
    public GameObject examineUI;

    [FoldoutGroup("Examine")]
    public TMP_Text examineText;

    [FoldoutGroup("Examine")]
    public Image examineImage;

    [FoldoutGroup("Game Over")]
    public GameObject gameOverUI;

    [FoldoutGroup("Game Over")]
    public TMP_Text deathText;*/

    [FoldoutGroup("Pause Menu")]
    public GameObject pauseUI;
    
    [FoldoutGroup("Scoreboard")]
    public GameObject scoreBoardUI;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        }

    // Update is called once per frame
    void Update()
    {
        /*if (examineUI.activeInHierarchy)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Q))
            {
                Unexamine();
            }
        }*/
        if (!gameplayUI.transform.parent.GetComponent<Canvas>().worldCamera && GameSessionManager.Instance.localPlayerController)
        {
            gameplayUI.transform.parent.GetComponent<Canvas>().worldCamera = GameSessionManager.Instance.localPlayerController.cameraList[1];
        }
        
        if (GameSessionManager.Instance.localPlayerController)
        {
            healthBar.fillAmount = GameSessionManager.Instance.localPlayerController.health.Value / 100f;
            staminaBar.fillAmount = GameSessionManager.Instance.localPlayerController.stamina.Value / 100f;
        }


        if (Input.GetKey(KeyCode.Tab))
        {
            ShowScoreBoard();
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            scoreBoardUI.SetActive(false);
        }
        if (LevelManager.Instance)
        {
            if (LevelManager.Instance.currentGameState.Value != LevelManager.GameState.NotStarted)
            {
                float timeLeft = LevelManager.Instance.matchTimer.Value;
                string textfieldMinutes = TimeSpan.FromSeconds(timeLeft).Minutes.ToString();
                string textfieldSeconds = TimeSpan.FromSeconds(timeLeft).Seconds.ToString();
                string timeDisplay = "";
                if (textfieldMinutes.Length == 2 && textfieldSeconds.Length == 2)
                    timeDisplay = textfieldMinutes + ":" + textfieldSeconds;
                else if (textfieldMinutes.Length == 2 && textfieldSeconds.Length == 1)
                    timeDisplay = textfieldMinutes + ":0" + textfieldSeconds;
                else if (textfieldMinutes.Length == 1 && textfieldSeconds.Length == 1)
                    timeDisplay = "0" + textfieldMinutes + ":0" + textfieldSeconds;
                else if (textfieldMinutes.Length == 1 && textfieldSeconds.Length == 2)
                    timeDisplay = "0" + textfieldMinutes + ":" + textfieldSeconds;
                else
                    timeDisplay = textfieldMinutes + ":" + textfieldSeconds;

                if (LevelManager.Instance.currentGameState.Value == LevelManager.GameState.PreExtraction)
                    timerText.text = "Time until extraction: " + timeDisplay;
                else if (LevelManager.Instance.currentGameState.Value == LevelManager.GameState.Extraction)
                    timerText.text = "Time left: " + timeDisplay;
            }
        }

    }

    public void Notify(string text)
    {
        notificationUI.GetComponentInChildren<TMP_Text>().text = text;
        FadeInOut(notificationUI);
    }

    public void FadeInOut(GameObject UI, float inTime = 0.5f, float duration = 1f, float outTime = 0.5f)
    {
        StartCoroutine(CoFadeInOutUI(UI, inTime, duration, outTime));
    }

    public IEnumerator CoFadeInOutUI(GameObject UI, float inTime, float duration, float outTime)
    {
        //UI.SetActive(true);

        foreach (Image image in UI.transform.GetComponentsInChildren<Image>())
        {
            image.DOFade(1, inTime);
        }
        foreach (TMP_Text text in UI.transform.GetComponentsInChildren<TMP_Text>())
        {
            text.DOFade(1, inTime);
        }

        yield return new WaitForSeconds(duration);

        foreach (Image image in UI.transform.GetComponentsInChildren<Image>())
        {
            image.DOFade(0, outTime);
        }
        foreach (TMP_Text text in UI.transform.GetComponentsInChildren<TMP_Text>())
        {
            text.DOFade(0, outTime);
        }

        //UI.SetActive(false);
    }

    /*public void Examine(string text, Sprite image)
    {
        GameSessionManager.Instance.localPlayerController.LockMovement(true);
        GameSessionManager.Instance.localPlayerController.LockCamera(true);

        examineUI.SetActive(true);
        gameplayUI.SetActive(false);
        examineText.text = text;
        if(image != null){
            examineImage.enabled = true;
            examineImage.sprite = image;
        }
    }

    public void Unexamine()
    {
        GameSessionManager.Instance.localPlayerController.LockMovement(false);
        GameSessionManager.Instance.localPlayerController.LockCamera(false);

        examineUI.SetActive(false);
        gameplayUI.SetActive(true);
        examineText.text = "";
        examineImage.enabled = false;
    }*/

    public void ShowScoreBoard()
    {
        scoreBoardUI.SetActive(true);
        
        for (int i = 0; i < 4; i++)
        {
            Transform currentLine = scoreBoardUI.transform.GetChild(1).GetChild(i);
            
            if (i < GameSessionManager.Instance.connectedPlayerCount)
            {
                
                currentLine.gameObject.SetActive(true);
                currentLine.GetChild(0).GetComponent<TMP_Text>().text =
                    GameSessionManager.Instance.playerControllerList[i].playerUsername;
                currentLine.GetChild(1).GetComponent<TMP_Text>().text =
                    GameSessionManager.Instance.playerControllerList[i].GetComponent<PlayerRating>().score.Value+"";
            }
            else
            {
                currentLine.gameObject.SetActive(false);
            }
            
        }
        
    }
    // public enum SubtitleType
    // {
    //     Radio,
    //     Dialogue
    // }

    // public void FadeInSubtitle(CharacterName speaker, string tempSubtitle, SubtitleType type)
    // {
    //     TMP_Text subtitleUI;
    //     switch (type)
    //     {
    //         case SubtitleType.Radio:
    //             subtitleUI = radioSubtitleUI;
    //             break;

    //         case SubtitleType.Dialogue:
    //             subtitleUI = dialogueSubtitleUI;
    //             break;

    //         default:
    //             subtitleUI = dialogueSubtitleUI;
    //             break;
    //     }

    //     subtitleUI.gameObject.SetActive(true);
    //     string name = speaker.ToString();
    //     switch (speaker)
    //     {
    //         case CharacterName.Mark:
    //             name = "<style=\"Blue\">" + name + "</style>";
    //             break;
    //         case CharacterName.GrandFather:
    //             name = "<style=\"Yellow\">" + name + "</style>";
    //             break;
    //         case CharacterName.Mira:
    //             name = "<style=\"Blue\">" + name + "</style>";
    //             break;
    //         case CharacterName.Arnii:
    //             name = "<style=\"Yellow\">" + name + "</style>";
    //             break;
    //         default:
    //             name = "";
    //             break;
    //     }

    //     switch (type)
    //     {
    //         case SubtitleType.Radio:
    //             name = "(Recording) " + name;
    //             break;
    //     }

    //     float subtitleFadeDuration = 0.25f;
    //     subtitleUI.DOFade(0, subtitleFadeDuration).OnComplete(() =>
    //     {
    //         subtitleUI.text = name + ": " + tempSubtitle;

    //         int line = 1 + subtitleUI.text.Length / 72;
    //         subtitleUI.rectTransform.SetHeight(Math.Clamp(50 * line, 50, 200));

    //         subtitleUI.DOFade(1, subtitleFadeDuration);
    //     }
    //     );
    // }

    // public void FadeOutSubtitle(SubtitleType type)
    // {
    //     TMP_Text subtitleUI;
    //     switch (type)
    //     {
    //         case SubtitleType.Radio:
    //             subtitleUI = radioSubtitleUI;
    //             break;

    //         case SubtitleType.Dialogue:
    //             subtitleUI = dialogueSubtitleUI;
    //             break;

    //         default:
    //             subtitleUI = dialogueSubtitleUI;
    //             break;
    //     }

    //     float subtitleFadeDuration = 0.25f;
    //     subtitleUI.DOFade(0, subtitleFadeDuration).OnComplete(() =>
    //     {
    //         subtitleUI.gameObject.SetActive(false);
    //     }
    //     );
    // }

    // public void ClearSubtitle(SubtitleType type)
    // {
    //     TMP_Text subtitleUI;
    //     switch (type)
    //     {
    //         case SubtitleType.Radio:
    //             subtitleUI = radioSubtitleUI;
    //             break;

    //         case SubtitleType.Dialogue:
    //             subtitleUI = dialogueSubtitleUI;
    //             break;

    //         default:
    //             subtitleUI = dialogueSubtitleUI;
    //             break;
    //     }

    //     subtitleUI.DOFade(0, 0.01f).OnComplete(() =>
    //     {
    //         subtitleUI.text = "";
    //         subtitleUI.gameObject.SetActive(false);
    //     }
    //     );
    // }
}
