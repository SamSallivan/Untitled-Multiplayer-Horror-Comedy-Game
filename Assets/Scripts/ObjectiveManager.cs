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
    public void AssignObjective(Objective objective)
    {
        Objective newObjective = Instantiate(objective, gameObject.transform).GetComponent<Objective>();
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

            if (objective.GetComponent<Objective>().showNotification)
            {
                UIManager.instance.objectiveNotificationTitle.text = "NEW OBJECTIVE";
                UIManager.instance.objectiveNotificationText.text = objective.GetComponent<Objective>().objectiveName;
                UIManager.instance.FadeInOut(UIManager.instance.objectiveNotificationUI, 1f, 2f, 1f);
            }

            UIManager.instance.objectiveTextList[objectiveList.IndexOf(objective.GetComponent<Objective>())].DOFade(1, 1f);
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
    public void CompleteObjective(Objective completedObjective)
    {
        CompleteObjectiveClientRpc(completedObjective.NetworkObject);
    }
    
    [Rpc(SendTo.Everyone)]
    public void CompleteObjectiveClientRpc(NetworkObjectReference objectiveReference)
    {
        if (objectiveReference.TryGet(out NetworkObject objective))
        {
            if (objective.GetComponent<Objective>().showNotification)
            {
                UIManager.instance.objectiveNotificationTitle.text = "OBJECTIVE COMPLETED";
                UIManager.instance.objectiveNotificationText.text = objective.GetComponent<Objective>().objectiveName;
                UIManager.instance.FadeInOut(UIManager.instance.objectiveNotificationUI, 1f, 2f, 1f);
            }

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
                string text = "\u2748 " + objectiveList[i].objectiveText;
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
