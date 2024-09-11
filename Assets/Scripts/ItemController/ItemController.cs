using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public abstract class ItemController : NetworkBehaviour
{
    public abstract void UseItem();
}
