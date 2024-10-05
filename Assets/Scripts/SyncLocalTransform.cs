using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyncLocalTransform : MonoBehaviour
{
    public Transform target;
    void Update()
    {
        if (target != null)
        {
            transform.localPosition = target.localPosition;
            transform.localRotation = target.localRotation;
        }
    }
}
