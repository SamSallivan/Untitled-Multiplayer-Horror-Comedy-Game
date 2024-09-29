using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PostProcessEffects : MonoBehaviour
{
    public static PostProcessEffects Instance { get; private set; }
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


    public GameObject flashVolume;
    public GameObject gameVolume;
    public GameObject uiVolume;
    private int width, height;
    private Animator anim;
    
    // Start is called before the first frame update
    void Start()
    {
        width = Screen.width;
        height = Screen.height;
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void FlashBlind()
    {
        StartCoroutine(FlashCoroutine());
    }

    public IEnumerator FlashCoroutine()
    {
        yield return new WaitForEndOfFrame();
        Texture2D tex = new Texture2D(width, height,TextureFormat.RGB24,false);
        tex.ReadPixels(new Rect(0,0,width,height),0,0);
        tex.Apply();

        flashVolume.GetComponentInChildren<Image>().sprite =
            Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100);
        anim.SetTrigger("Blind");
    }
    
}
