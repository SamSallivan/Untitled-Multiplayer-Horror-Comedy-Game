using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorSyncPosition : MonoBehaviour
{

    // Update is called once per frame
    void Update()
    {
        if(Camera.main)
        {
            transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, transform.position.z);
        }
    }
}
