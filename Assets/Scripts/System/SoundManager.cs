using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SoundManager : NetworkBehaviour
{
    public static SoundManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            UnityEngine.Object.Destroy(Instance.gameObject);
            Instance = this;
        }
    }

    public GameObject SoundEffectPrefab;
    public GameObject SoundEffectNetworkPrefab;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void PlayServerSoundEffect(AudioClip clip, Vector3 location,float volume = 1)
    {
        GameObject sfx = Instantiate(SoundEffectNetworkPrefab);
        var instanceNetworkObject = sfx.GetComponent<NetworkObject>();
        sfx.transform.position = location;
        instanceNetworkObject.Spawn();
        sfx.GetComponent<AudioSource>().PlayOneShot(clip,volume);
        Destroy(sfx,clip.length);
    }

    public void PlayClientSoundEffect(AudioClip clip, Vector3 location,float volume = 1)
    {
        GameObject sfx = Instantiate(SoundEffectPrefab);
        sfx.transform.position = location;
        sfx.GetComponent<AudioSource>().PlayOneShot(clip,volume);
        Destroy(sfx,clip.length);
    }
    
}
