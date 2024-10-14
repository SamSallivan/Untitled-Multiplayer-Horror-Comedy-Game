using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;

public class BeerBottleController : ItemController
{
    public bool thrown;
    public float force = 10f;
    public float damage = 25f;
    public float radius = 1f;
    public LayerMask attackMask;
    public float thrownTime;
    public float collisionDetectionDelay = 0.5f;

    public override void OnButtonHeld()
    {
        if (heldTime > minHeldTime)
        {
            Activate();
        }
    }
    
    public override void OnButtonHeldSecondary()
    {
        inventoryItem.owner.cameraBob.targetFOV = 75f;
    }
    
    public override void OnButtonReleasedSecondary()
    {
        inventoryItem.owner.cameraBob.targetFOV = 90f;
    }
    
    public override void Activate()
    {
        base.Activate();
        StartCoroutine(ThrowBottleCoroutine());
    }

    public IEnumerator ThrowBottleCoroutine()
    {
        //yield return new WaitForSeconds(0.5f);
        
        PlayerController playerController = inventoryItem.owner;
        Vector3 direction = playerController.headTransform.forward;
        
        InventoryManager.instance.DiscardEquippedItem();
        ThrowBottleServerRpc(direction);
        
        yield return null;
    }

    [Rpc(SendTo.Server)]
    public void ThrowBottleServerRpc(Vector3 direction)
    {
        thrown = true;
        GetComponent<Rigidbody>().AddForce(direction * force, ForceMode.Impulse);
        GetComponent<Rigidbody>().AddTorque(direction * force, ForceMode.Impulse);
    }

    public override void ItemUpdate()
    {
        if (IsServer)
        {
            if (thrown)
            {
                thrownTime += Time.deltaTime;
            }
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) 
        {
            return;
        }

        if (!thrown || thrownTime < collisionDetectionDelay)
        {
            return;
        }

        BottleHitClientRpc();
        thrown = false;
        thrownTime = 0f;
    }
    
    [Rpc(SendTo.Everyone)]
    public void BottleHitClientRpc()
    {
        Collider[] hitss = Physics.OverlapSphere(transform.position, radius, attackMask);
            
        for (int i = 0; i < hitss.Length; i++)
        {
            if (hitss[i].transform.TryGetComponent<IDamagable>(out IDamagable component))
            {
                Vector3 direction = hitss[i].transform.position - transform.position;
                component.TakeDamage(damage, direction);
            }
        }
        
        Destroy(gameObject);

        /*
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, 0.5f, Vector3.zero, 0.5f, attackMask, QueryTriggerInteraction.Collide);
        List<RaycastHit> hitList = hits.OrderBy((RaycastHit x) => x.distance).ToList();
        bool hitSomething = false;
        for (int i = 0; i < hitList.Count; i++)
        {
            IDamagable component;
            Rigidbody rb;
            PlayerController pc;
            NightCrawler ma;
            
            if (hitList[i].transform.TryGetComponent<IDamagable>(out component))
            {
                Vector3 direction = hitList[i].transform.position - transform.position;
                hitSomething = true;
                component.TakeDamage(damage, direction);
            }
            
            else if (hitList[i].transform.TryGetComponent<Rigidbody>(out rb))
            {
                Vector3 direction = hitList[i].transform.position - transform.position;
                rb.AddForce(direction * damage, ForceMode.Impulse);
            }
        }

        if (hitSomething)
        {
            //SoundManager.Instance.PlayClientSoundEffect(hitSound,transform.position);
        }*/
        
    }

    public override void Cancel()
    {
        base.Cancel();
        OnButtonReleasedSecondary();
    }
}
