using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noises
{

    public static void MakeSound(Noise noise) 
    {
        Collider[] col = Physics.OverlapSphere(noise.pos, noise.range);

        for (int i = 0; i < col.Length; i++)
        {
            if (col[i].TryGetComponent(out IHear hearer))
                hearer.RespondToSound(noise);
        }

    }
}
