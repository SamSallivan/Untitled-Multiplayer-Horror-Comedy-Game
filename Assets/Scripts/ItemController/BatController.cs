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

    /*public override void UseItem(bool buttonDown = true)
    {
        if(buttonDown && cooldown <= 0)
        {
            
        }
        else if (!buttonDown && cooldown <= 0)
        {
            BatSwingServerRpc();
            cooldown = 1;
        }
    }*/
    
    
    /*public override void OnButtonHeld()
    {
    }
    
    public override void OnButtonReleased()
    {
    }*/

    public override void Activate()
    {
        BatSwingServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void BatSwingServerRpc()
    {
        BatSwingClientRpc();
    }

    [ClientRpc]
    public void BatSwingClientRpc()
    {
        PlayerController owner = GetComponent<I_InventoryItem>().owner;
        RaycastHit[] hits = Physics.SphereCastAll(owner.headTransform.position + owner.headTransform.right * -0.35f, 0.8f, owner.headTransform.forward, 1.5f, attackMask, QueryTriggerInteraction.Collide);
        List<RaycastHit> hitList = hits.OrderBy((RaycastHit x) => x.distance).ToList();
        for (int i = 0; i < hitList.Count; i++)
        {
            IDamagable component;
            Rigidbody rb;
            
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
        }
        
    }
}