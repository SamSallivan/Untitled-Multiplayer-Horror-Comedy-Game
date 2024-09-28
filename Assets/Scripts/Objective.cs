using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Objective : NetworkBehaviour
{
    public string objectiveName;
    public string triggerEvent;
    public int requiredValue = 1;
    public NetworkVariable<int> completedValue = new (0, writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isCompleted = new (false, writePerm: NetworkVariableWritePermission.Server);
    public List<PlayerController> targetPlayerList = new List<PlayerController>();
    public int score = 0;
    public Objective followUpObjective;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        completedValue.OnValueChanged += OnCompletedValueChanged;
        OnObjectiveAssigned();
    }
    
    public void Update()
    {
        ObjectiveUpdate();
    }

    public virtual void ObjectiveUpdate()
    {
        
    }

    [Rpc(SendTo.Server)]
    public void AddProgressServerRpc(int value)
    {
        if (isCompleted.Value) {
            return;
        }
        
        completedValue.Value += value;
        
        if (completedValue.Value >= requiredValue) {
            isCompleted.Value = true;
            OnObjectiveCompleted();
        }
    }

    public void OnObjectiveAssigned()
    {
        StartCoroutine(OnObjectiveAssignedCoroutine());
    }

    public void OnObjectiveCompleted()
    {
        ObjectiveManager.instance.CompleteObjective(this);
        if (followUpObjective)
        {
            ObjectiveManager.instance.AssignObjective(followUpObjective);
        }
        StartCoroutine(OnObjectiveCompletedCoroutine());
    }

    public virtual IEnumerator OnObjectiveAssignedCoroutine()
    {
        yield return null;
    }

    public virtual IEnumerator OnObjectiveCompletedCoroutine()
    {
        yield return null;
    }

    public void OnCompletedValueChanged(int prev, int curr)
    {
        ObjectiveManager.instance.UpdateObjectiveUI();
    }
}
