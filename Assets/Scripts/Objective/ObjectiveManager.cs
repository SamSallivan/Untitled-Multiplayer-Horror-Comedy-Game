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
    
    //public Objective objectivePrefab;
    //public List<ObjectiveData> initialObjectiveData = new List<ObjectiveData>();
    
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

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        
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

        foreach (Objective subObjective in newObjective.subObjectiveList)
        {
            subObjective.targetPlayerId.Value = targetPlayerId;
        }
        
        AssignObjectiveClientRpc(newObjective.NetworkObject);
    }
    
    [Rpc(SendTo.Everyone)]
    public void AssignObjectiveClientRpc(NetworkObjectReference objectiveReference)
    {
        if (objectiveReference.TryGet(out NetworkObject objective))
        {
            objectiveList.Add(objective.GetComponent<Objective>());
            UpdateObjectiveUI();

            UIManager.instance.objectiveTextList[objectiveList.IndexOf(objective.GetComponent<Objective>())].DOFade(1, 1f);

            if (objective.GetComponent<Objective>().targetPlayerId.Value == -1 || objective.GetComponent<Objective>().targetPlayerId.Value == GameSessionManager.Instance.localPlayerController.localPlayerId)
            {
                if (objective.GetComponent<Objective>().showNotification &&
                    !objective.GetComponent<Objective>().isSubObjective)
                {
                    UIManager.instance.objectiveNotificationTitle.text = "NEW OBJECTIVE";
                    UIManager.instance.objectiveNotificationText.text =
                        objective.GetComponent<Objective>().objectiveName;
                    UIManager.instance.FadeInOut(UIManager.instance.objectiveNotificationUI, 1f, 2f, 1f);
                }
            }

            foreach (Objective subObjective in objective.GetComponent<Objective>().subObjectiveList)
            {
                objectiveList.Add(subObjective);
                UpdateObjectiveUI();

                UIManager.instance.objectiveTextList[objectiveList.IndexOf(subObjective)].DOFade(1, 1f);
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
                Debug.Log(objective.targetPlayerId.Value + ", " + GameSessionManager.Instance.localPlayerController.localPlayerId);
                if (objective.targetPlayerId.Value == -1 || objective.targetPlayerId.Value == GameSessionManager.Instance.localPlayerController.localPlayerId)
                {
                    objective.AddProgressServerRpc(value);
                }
            }
        }
    }
    
    [Button]
    public void CompleteObjective(Objective completedObjective)
    {
        if (completedObjective.isSubObjective)
        {
            return;
        }
        
        CompleteObjectiveClientRpc(completedObjective.NetworkObject);
    }
    
    [Rpc(SendTo.Everyone)]
    public void CompleteObjectiveClientRpc(NetworkObjectReference objectiveReference)
    {
        if (objectiveReference.TryGet(out NetworkObject objective))
        {

            if (objective.GetComponent<Objective>().targetPlayerId.Value == -1 || objective.GetComponent<Objective>().targetPlayerId.Value == GameSessionManager.Instance.localPlayerController.localPlayerId)
            {
                RatingManager.instance.AddScore(objective.gameObject.GetComponent<Objective>().score);
            }
            
            foreach (Objective subObjective in objective.GetComponent<Objective>().subObjectiveList)
            {
                UIManager.instance.objectiveTextList[objectiveList.IndexOf(subObjective)].DOFade(0, 1f).OnComplete(() =>
                    {
                        objectiveList.Remove(subObjective);
                        UpdateObjectiveUI();

                        if (IsServer)
                        {
                            objective.Despawn();
                        }
                    }
                );
            }
            
            if (objective.GetComponent<Objective>().showNotification && !objective.GetComponent<Objective>().isSubObjective)
            {
                UIManager.instance.objectiveNotificationTitle.text = "OBJECTIVE COMPLETED";
                UIManager.instance.objectiveNotificationText.text = objective.GetComponent<Objective>().objectiveName;
                UIManager.instance.FadeInOut(UIManager.instance.objectiveNotificationUI, 1f, 2f, 1f);
            }

            UIManager.instance.objectiveTextList[objectiveList.IndexOf(objective.GetComponent<Objective>())].DOFade(0, 1f).OnComplete(() =>
                {
                    objectiveList.Remove(objective.GetComponent<Objective>());
                    UpdateObjectiveUI();

                    // if (IsServer)
                    // {
                    //     objective.Despawn();
                    // }
                }
            );
        }
    }

    public void UpdateObjectiveUI()
    {
        //List<Objective> personalObjectiveList = new List<Objective>();
        personalObjectiveList.Clear();
        foreach (Objective objective in objectiveList)
        {
            if (objective.targetPlayerId.Value == -1 || objective.targetPlayerId.Value == GameSessionManager.Instance.localPlayerController.localPlayerId)
            {
                personalObjectiveList.Add(objective);
            }
        }
        
        for (int i = 0; i < UIManager.instance.objectiveTextList.Count; i++)
        {
            if (i < personalObjectiveList.Count)
            {
                string text = "\u2748 ";

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
                
                UIManager.instance.objectiveTextList[i].text = text;
                
            }
            else
            {
                UIManager.instance.objectiveTextList[i].text = "";
            }
        }
    }
}
