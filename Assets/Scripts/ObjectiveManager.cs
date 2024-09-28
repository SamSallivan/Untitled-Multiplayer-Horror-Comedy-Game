using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

public class ObjectiveManager : NetworkBehaviour
{
    public static ObjectiveManager instance;
    
    public Objective initialObjective;
    
    public List<Objective> objectiveList = new List<Objective>();
    
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
        
        if(IsServer){
            if (initialObjective)
            {
                instance.AssignObjective(initialObjective);
            }
        }
    }
    
    [Button]
    public void AssignObjective(Objective objective)
    {
        Objective newObjective = Instantiate(objective).GetComponent<Objective>();
        newObjective.NetworkObject.Spawn();
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
            UIManager.instance.objectiveNotificationText.text = objective.GetComponent<Objective>().objectiveName;
            UIManager.instance.FadeInOut(UIManager.instance.objectiveNotificationUI, 1f, 2f, 1f);
        }
    }
    
    [Button]
    public void AddProgressToObjective(string eventTrigger, int value) 
    {
        foreach (Objective objective in objectiveList) 
        {
            if (objective.triggerEvent == eventTrigger)
            {
                objective.AddProgressServerRpc(value);
            }
        }
    }
    
    [Button]
    public void CompleteObjective(Objective objective)
    {
        CompleteObjectiveClientRpc(objective.NetworkObject);
    }
    
    [Rpc(SendTo.Everyone)]
    public void CompleteObjectiveClientRpc(NetworkObjectReference objectiveReference)
    {
        if (objectiveReference.TryGet(out NetworkObject objective))
        {
            UIManager.instance.objectiveTextList[objectiveList.IndexOf(objective.GetComponent<Objective>())].DOFade(0, 1f).OnComplete(() =>
            {
                objectiveList.Remove(objective.GetComponent<Objective>());
                UpdateObjectiveUI();
            }
            );
            RatingManager.instance.AddScore(objective.gameObject.GetComponent<Objective>().score);
        }
    }

    public void UpdateObjectiveUI()
    {
        for (int i = 0; i < UIManager.instance.objectiveTextList.Count; i++)
        {
            if (i < objectiveList.Count)
            {
                string text = objectiveList[i].objectiveName;
                if (objectiveList[i].requiredValue > 1)
                {
                    text += $" ({objectiveList[i].completedValue.Value}/{objectiveList[i].requiredValue})";
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
