using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class BatController : ItemController
{
    public float damage;
    public LayerMask attackMask;

    public override void Update()
    {
        base.Update();
    }
    
    /*public override void OnButtonHeld()
    {
    }
    
    public override void OnButtonReleased()
    {
    }*/

    public override void Activate()
    {
        BatSwingClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    public void BatSwingClientRpc()
    {
        PlayerController owner = GetComponent<I_InventoryItem>().owner;
        RaycastHit[] hits = Physics.SphereCastAll(owner.headTransform.position + owner.headTransform.right * -0.35f, 0.8f, owner.headTransform.forward, 1.5f, attackMask, QueryTriggerInteraction.Collide);
        List<RaycastHit> hitList = hits.OrderBy((RaycastHit x) => x.distance).ToList();
        for (int i = 0; i < hitList.Count; i++)
        {
            IDamagable component;
            Rigidbody rb;
            PlayerController pc;
            MonsterAI ma;
            
            if (hitList[i].transform.TryGetComponent<IDamagable>(out component) && hitList[i].transform != owner.transform)
            {
                Vector3 direction = owner.headTransform.forward;
                component.TakeDamage(damage, direction);
            }
            
            else if (hitList[i].transform.TryGetComponent<Rigidbody>(out rb) && hitList[i].transform != owner.transform)
            {
                Vector3 direction = owner.headTransform.forward;
                rb.AddForce(direction * damage, ForceMode.Impulse);
            }
            
            if (hitList[i].transform.TryGetComponent<PlayerController>(out pc) && hitList[i].transform != owner.transform)
            {
                if(pc!=null)
                    RatingManager.instance.AddScore(20,"Friendly Fire!", owner);
            }
            
            if (hitList[i].transform.TryGetComponent<MonsterAI>(out ma) && hitList[i].transform != owner.transform)
            {
                if(ma!=null)
                    RatingManager.instance.AddScore(30,"Enemy Hit!", owner);
            }
        }
        
    }
}