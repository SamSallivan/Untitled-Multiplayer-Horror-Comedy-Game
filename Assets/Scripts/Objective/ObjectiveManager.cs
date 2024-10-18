using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

public class ObjectiveManager : NetworkBehaviour
{
    public static ObjectiveManager instance;
    
    public List<Objective> initialObjective = new List<Objective>();
    
    public List<Objective> objectiveList = new List<Objective>();
    
    public List<Objective> personalObjectiveList = new List<Objective>();
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if(IsServer)
        {
            StartCoroutine(AssignInitialObjectiveCoroutine());
        }
    }

    public void Update()
    {
        UpdateObjectiveUI();
    }

    public virtual IEnumerator AssignInitialObjectiveCoroutine()
    {
        yield return new WaitForSeconds(2.5f);
        
        foreach (Objective objective in initialObjective)
        {
            AssignObjective(objective);
        }
    }

    
    [Button]
    public void AssignObjective(Objective objective, int targetPlayerId = -1)
    {
        Objective newObjective = Instantiate(objective, gameObject.transform).GetComponent<Objective>();
        newObjective.NetworkObject.Spawn();
        newObjective.targetPlayerId.Value = targetPlayerId;
        newObjective.timer.Value = newObjective.timeLimit;

        foreach (Objective subObjective in newObjective.subObjectiveList)
        {
            subObjective.targetPlayerId.Value = targetPlayerId;
        }
        
        StartCoroutine(AssignObjectiveCoroutine(newObjective));
    }

    public IEnumerator AssignObjectiveCoroutine(Objective objective)
    {
        yield return new WaitForSeconds(0.5f);
        AssignObjectiveClientRpc(objective.NetworkObject);
    }
    
    [Rpc(SendTo.Everyone)]
    public void AssignObjectiveClientRpc(NetworkObjectReference objectiveReference)
    {
        if (!objectiveReference.TryGet(out NetworkObject objectiveObject))
        {
            return;
        }
        Objective objective = objectiveObject.GetComponent<Objective>();
        
        objectiveList.Add(objective);
        foreach (Objective subObjective in objective.subObjectiveList)
        {
            objectiveList.Add(subObjective);
        }
        
        if (objective.targetPlayerId.Value == -1 || objective.targetPlayerId.Value == GameSessionManager.Instance.localPlayerController.localPlayerId)
        {
            if (objective.showNotification && !objective.isSubObjective)
            {
                UIManager.instance.objectiveNotificationTitle.text = "NEW OBJECTIVE";
                UIManager.instance.objectiveNotificationText.text = objective.objectiveName;
                UIManager.instance.FadeInOut(UIManager.instance.objectiveNotificationUI, 1f, 2f, 1f);
            }
            
            personalObjectiveList.Add(objective);
            //UpdateObjectiveUI();
            //UIManager.instance.objectiveTextList[personalObjectiveList.IndexOf(objective)].DOFade(1, 1f);
            
            foreach (Objective subObjective in objective.subObjectiveList)
            {
                personalObjectiveList.Add(subObjective);
                //UpdateObjectiveUI();
                //UIManager.instance.objectiveTextList[personalObjectiveList.IndexOf(subObjective)].DOFade(1, 1f);
            }
        }
    }
    
    [Button]
    public void AddProgressToObjective(string eventTrigger, int value) 
    {
        foreach (Objective objective in objectiveList) 
        {
            if (objective.triggerEvent == eventTrigger)
            {
                if (objective.targetPlayerId.Value == -1 || objective.targetPlayerId.Value == GameSessionManager.Instance.localPlayerController.localPlayerId)
                {
                    objective.AddProgressServerRpc(value);
                }
            }
        }
    }
    
    [Button]
    public void CompleteObjective(Objective completedObjective, bool failed = false)
    {
        if (completedObjective.isSubObjective)
        {
            //UpdateObjectiveUI();
            return;
        }
        
        CompleteObjectiveClientRpc(completedObjective.NetworkObject, failed);
    }
    
    [Rpc(SendTo.Everyone)]
    public void CompleteObjectiveClientRpc(NetworkObjectReference objectiveReference, bool failed = false)
    {
        if (!objectiveReference.TryGet(out NetworkObject objectiveObject))
        {
            return;
        }
        Objective objective = objectiveObject.GetComponent<Objective>();
        
        objectiveList.Remove(objective);
        foreach (Objective subObjective in objective.subObjectiveList)
        {
            objectiveList.Remove(subObjective);
        }
        
        if (objective.targetPlayerId.Value == -1 || objective.targetPlayerId.Value == GameSessionManager.Instance.localPlayerController.localPlayerId)
        {
            if (!failed)
            {
                RatingManager.instance.AddScore(objective.score);

                if (objective.showNotification && !objective.isSubObjective)
                {
                    UIManager.instance.objectiveNotificationTitle.text = "OBJECTIVE COMPLETED";
                    UIManager.instance.objectiveNotificationText.text = objective.objectiveName;
                    UIManager.instance.FadeInOut(UIManager.instance.objectiveNotificationUI, 1f, 2f, 1f);
                }
            }
            else
            {
                if (objective.showNotification && !objective.isSubObjective)
                {
                    UIManager.instance.objectiveNotificationTitle.text = "OBJECTIVE FAILED";
                    UIManager.instance.objectiveNotificationText.text = objective.objectiveName;
                    UIManager.instance.FadeInOut(UIManager.instance.objectiveNotificationUI, 1f, 2f, 1f);
                }
            }
            
            personalObjectiveList.Remove(objective);
            //UpdateObjectiveUI();
            
            /*UIManager.instance.objectiveTextList[personalObjectiveList.IndexOf(objective)].DOFade(0, 1f).OnComplete(() =>
                {
                    personalObjectiveList.Remove(objective);
                    UpdateObjectiveUI();
                }
            );*/
            
            foreach (Objective subObjective in objective.subObjectiveList)
            {
                personalObjectiveList.Remove(subObjective);
                //UpdateObjectiveUI();
                
                /*UIManager.instance.objectiveTextList[personalObjectiveList.IndexOf(subObjective)].DOFade(0, 1f).OnComplete(() =>
                    {
                        personalObjectiveList.Remove(subObjective);
                        UpdateObjectiveUI();
                    }
                );*/
            }
        }
    }

    public void UpdateObjectiveUI()
    {
        for (int i = 0; i < UIManager.instance.objectiveTextList.Count; i++)
        {
            if (i < personalObjectiveList.Count)
            {
                string text = "[ ] ";

                if (personalObjectiveList[i].isCompleted.Value)
                {
                    text += $"<s>{personalObjectiveList[i].objectiveText}</s>";
                }
                else
                {
                    text += personalObjectiveList[i].objectiveText;
                }
                
                if (personalObjectiveList[i].isSubObjective)
                {
                    text = text.Insert(0, "      ");
                }
                
                if (personalObjectiveList[i].requiredValue > 1)
                {
                    text += $" ({personalObjectiveList[i].completedValue.Value}/{personalObjectiveList[i].requiredValue})";
                }

                if (personalObjectiveList[i].hasTimeLimit)
                {
                    text += "  " + GetDisplayTime(personalObjectiveList[i].timer.Value);
                }
                
                UIManager.instance.objectiveTextList[i].text = text;
                //UIManager.instance.objectiveTextList[i].alpha = 1;

            }
            else
            {
                UIManager.instance.objectiveTextList[i].text = "";
                //UIManager.instance.objectiveTextList[i].alpha = 0;
            }
        }
    }

    public string GetDisplayTime(float seconds)
    {
        string textfieldMinutes = TimeSpan.FromSeconds(seconds).Minutes.ToString();
        string textfieldSeconds = TimeSpan.FromSeconds(seconds).Seconds.ToString();
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
        
        return timeDisplay;
    }
}
