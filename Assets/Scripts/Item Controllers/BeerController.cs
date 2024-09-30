using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BeerController : ItemController
{
    public float totalDrinkCount = 0.5f;
    public float totalHealAmount;
    public float totalDrunkTime = 30f;
    public Vector2 fillAmountRange = new Vector2(0.6f, 0.4f);
    public float targetLookRotationY = 45f;
    public float lookRotationInterpolationSpeed = 1f;
    //public ItemData beerBottleItemData;
    public int beerBottleItemDataListIndex = 0;

    public bool chugged = true;

    public AudioClip drinkSound;
    public AudioClip chuggedSound;
    public AudioClip finishedSound;
    
    public override void ItemUpdate()
    {
        if (buttonHeld)
        {
            inventoryItem.owner.mouseLookY.SetRotation(Mathf.Lerp(inventoryItem.owner.mouseLookY.rotationY, targetLookRotationY, Time.deltaTime * lookRotationInterpolationSpeed));
            
            if (heldTime > minHeldTime && cooldown <= 0)
            {
                Activate();
            }
        }

        float currentFillAmount = transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material.GetFloat("_FillAmount");
        float targetFillAmount = Mathf.Lerp(fillAmountRange.x, fillAmountRange.y, inventoryItem.itemStatus.durability);
        transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material.SetFloat("_FillAmount", Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * 5f));
    }

    public override void OnButtonReleased()
    {
        base.OnButtonReleased();
        if (inventoryItem.itemStatus.durability > 0)
        {
            chugged = false;
        }
    }

    public override void Activate()
    {
        base.Activate();
        StartCoroutine(DrinkBeerCoroutine());
    }
    
    public IEnumerator DrinkBeerCoroutine()
    {
        heldTime = 0;
        cooldown = cooldownSetting;
                    
        inventoryItem.itemStatus.durability -= 1 / totalDrinkCount;
        InventoryManager.instance.SetItemDurarbilityRpc(inventoryItem.NetworkObject, inventoryItem.itemStatus.durability);

        if (inventoryItem.owner.health.Value < inventoryItem.owner.maxHp)
        {
            inventoryItem.owner.health.Value += totalHealAmount / totalDrinkCount;
        }
        inventoryItem.owner.drunkTimer +=  totalDrunkTime / totalDrinkCount;

        RatingManager.instance.AddScore(5, "Chug");
        SoundManager.Instance.PlayClientSoundEffect(drinkSound,transform.position);
        
        yield return new WaitForSeconds(0.25f);

        if(inventoryItem.itemStatus.durability <= 0)
        {
            if (chugged)
            {
                SoundManager.Instance.PlayClientSoundEffect(chuggedSound,transform.position);
                RatingManager.instance.AddScore(20, "CHUUUUUUGGED!"); 
            }
            else
            {
                SoundManager.Instance.PlayClientSoundEffect(finishedSound,transform.position);
                RatingManager.instance.AddScore(10, "Refreshing!"); 
            }
            
            
            ReplaceBeerWithBottle();
        }
    }
    
    public void ReplaceBeerWithBottle()
    {
        PlayerController playerController = inventoryItem.owner;
        InventoryManager.instance.DiscardEquippedItem();
        InventoryManager.instance.InstantiateReplaceItemServerRpc(beerBottleItemDataListIndex, playerController.NetworkObject);
        InventoryManager.instance.DestoryItemServerRpc(inventoryItem.NetworkObject); 
    }
}
