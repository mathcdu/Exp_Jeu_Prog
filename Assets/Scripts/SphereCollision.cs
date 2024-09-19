using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class SphereCollision : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other) //1.
    {
        if (Runner.IsServer && other.gameObject.TryGetComponent(out JoueurReseau joueurReseau)) //2.
        {
            joueurReseau.nbBoulesRouges++; //3.
            Runner.Despawn(Object);//4.
        }
    }
    /* Chaque boule vérifie sur le serveur uniquement (Runner.IsServer) si la partie est terminée. Si c'est
   le cas, elle se despawn elle-même.
   */
    public override void FixedUpdateNetwork()
    {
        if (Runner.IsServer)
        {
            if (!GameManager.partieEnCours)
            {
                Runner.Despawn(Object);
            }
        }
    }
}
