using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

public class Objective : NetworkBehaviour
{
    
    [Header("Settings")]
    public string objectiveName;
    public string objectiveText;
    public string triggerEvent;
    public bool showNotification;
    
    [Space]
    public bool hasTimeLimit;
    [ShowIf("hasTimeLimit")]
    public int timeLimit;
    [ShowIf("hasTimeLimit")]
    public NetworkVariable<float> timer = new (0, writePerm: NetworkVariableWritePermission.Server);
    
    [Space]
    public int requiredValue = 1;
    public NetworkVariable<int> completedValue = new (0, writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isCompleted = new (false, writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<int>  targetPlayerId = new (-1, writePerm: NetworkVariableWritePermission.Server);
    
    [Space]
    public int score = 0;
    public Objective followupObjective;
    public float followupObjectiveAssignDelay = 4f;
    
    [Space]
    public bool isSubObjective;
    [ShowIf("isSubObjective")]
    public Objective parentObjective;
    [HideIf("isSubObjective")]
    public List<Objective> subObjectiveList = new List<Objective>();
    
    //public List<PlayerController> targetPlayerList = new List<PlayerController>();
    //public ObjectiveData objectiveData;
    //public ObjectiveData followupObjectiveData;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        completedValue.OnValueChanged += OnCompletedValueChanged;
        StartCoroutine(OnObjectiveAssignedCoroutine());
    }
    
    public virtual IEnumerator OnObjectiveAssignedCoroutine()
    {
        yield return null;
    }
    
    [Rpc(SendTo.Server)]
    public void AddProgressServerRpc(int value)
    {
        if (isCompleted.Value) {
            return;
        }
        
        StartCoroutine(AddProgressCoroutine(value));
    }

    public IEnumerator AddProgressCoroutine(int value)
    {
        yield return new WaitForSeconds(0.5f);
        
        completedValue.Value += value;
        
        if (completedValue.Value >= requiredValue) 
        {
            isCompleted.Value = true;
            ObjectiveManager.instance.CompleteObjective(this);
            StartCoroutine(OnObjectiveCompletedCoroutine());
            StartCoroutine(AssignFollowupObjectiveCoroutine());
            if (isSubObjective && parentObjective != null)
            {
                parentObjective.AddProgressServerRpc(1);
            }
        }
    }

    public virtual IEnumerator OnObjectiveCompletedCoroutine()
    {
        yield return null;
    }

    public virtual IEnumerator AssignFollowupObjectiveCoroutine()
    {
        yield return new WaitForSeconds(followupObjectiveAssignDelay);
        
        if (followupObjective)
        {
            ObjectiveManager.instance.AssignObjective(followupObjective);
        }
        
        yield return new WaitForSeconds(1.0f);

        if (!isSubObjective)
        {
            NetworkObject.Despawn();
        }
    }

    public void OnCompletedValueChanged(int prev, int curr)
    {
        ObjectiveManager.instance.UpdateObjectiveUI();
    }

    /*public void InitializeObjectiveRpc(ObjectiveData objectiveData)
    {
        this.objectiveData = objectiveData;
        objectiveName = objectiveData.objectiveName;
        objectiveText = objectiveData.objectiveText;
        triggerEvent = objectiveData.triggerEvent;
        requiredValue = objectiveData.requiredValue;
        score = objectiveData.score;
        followupObjectiveData = objectiveData.followupObjectiveData;
        followupObjectiveAssignDelay = objectiveData.followupObjectiveAssignDelay;
    }*/
    
    public void Update()
    {
        if (!IsServer)
        {
            return;
        }
        
        if (hasTimeLimit && !isCompleted.Value)
        {
            if (timer.Value > 0)
            {
                timer.Value -= Time.deltaTime;
            }
            else
            {
                isCompleted.Value = true;
                ObjectiveManager.instance.CompleteObjective(this, true);
                StartCoroutine(OnObjectiveFailedCoroutine());
                StartCoroutine(AssignFollowupObjectiveCoroutine());
            }
        }
        
        ObjectiveUpdate();
    }

    public virtual IEnumerator OnObjectiveFailedCoroutine()
    {
        yield return null;
    }

    public virtual void ObjectiveUpdate()
    {

    }
}
