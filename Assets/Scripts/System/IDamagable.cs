using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamagable
{
    void TakeDamage(float damage, Vector3 direction, float stunTime = 0f);
}