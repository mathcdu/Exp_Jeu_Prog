using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Classe statique qui contient des fonctions utilitaires générales utilisées par plusieurs scripss

public static class Utilitaires
{

    // Focntion statique qui retourne un vector3 aléatoire
    public static Vector3 GetPositionSpawnAleatoire()
    {
        return new Vector3(Random.Range(-20, 20), 4, Random.Range(-20, 20));
    }

    /*
     * Fonction qui permet de changer le layer de tous les enfants d'un gameOject
     * Transform transform : le transform du parent pour lequel ont veut changer le layer des enfants
     * int numLayer : le layer sur lequel on veut mettre les enfants
     * Avec un foreach, on boucle a à travers tous les enfants du parent et on change le layer.
     * Le (true) à la fin du GetComponentsInChildren indique que même les objets désactivés seront affectés.
     */
    public static void SetRenderLayerInChildren(Transform transform, int numLayer)
    {
        foreach (Transform trans in transform.GetComponentsInChildren<Transform>(true))
        {
            trans.gameObject.layer = numLayer;
        }
    }

}
