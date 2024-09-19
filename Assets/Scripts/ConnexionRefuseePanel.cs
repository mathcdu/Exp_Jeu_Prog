using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnexionRefuseePanel : MonoBehaviour
{
    /* Gestion du panneau qui affiche la raison du refus de connexion (limite de joueur atteinte)
    Lorsque l'objet est activé, on appelle une coroutine qui désactivera le texte après un délai de 5 secondes.
    */
    void OnEnable()
    {
        StartCoroutine(DelaiDesactivation());
    }

    IEnumerator DelaiDesactivation()
    {
        yield return new WaitForSeconds(5f);
        gameObject.SetActive(false);
    }
}
