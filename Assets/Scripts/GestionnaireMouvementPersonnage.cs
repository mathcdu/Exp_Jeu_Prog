using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;

/*
* Script qui exécute les déplacements du joueur et ainsi que l'ajustement de direction
* Dérive de NetworkBehaviour. Utilisation de la fonction réseau FixedUpdateNetwork()
* Variables :
* - camLocale : contient la référence à la caméra du joueur actuel
* - NetworkCharacterController : pour mémoriser le component NetworkCharacterController
* du joueur
*/

public class GestionnaireMouvementPersonnage : NetworkBehaviour
{
    Camera camLocale;
    NetworkCharacterController networkCharacterController;
    // variable pour garder une référence au script gestionnairePointsDeVie;
    GestionnairePointsDeVie gestionnairePointsDeVie;
    // variable pour savoir si un Respawn du joueur est demandé
    bool respawnDemande = false;

    /*
     * Avant le Start(), on mémorise la référence au component networkCharacterController du joueur
     * On garde en mémoire la caméra du joueur courant (GetComponentInChildren)
     */
    void Awake()
    {
        networkCharacterController = GetComponent<NetworkCharacterController>();
        camLocale = GetComponentInChildren<Camera>();
        gestionnairePointsDeVie = GetComponent<GestionnairePointsDeVie>();
    }


    /*
     * Fonction récursive réseau pour la simulation. À utiliser pour ce qui doit être synchronisé entre
     * les différents clients.
     * 1.Récupération des Inputs mémorisés dans le script GestionnaireReseau (input.set). Ces données enregistrées
     * sous forme de structure de données (struc) doivent être récupérées sous la même forme.
     * 2.Ajustement de la direction du joueur à partir à partir des données de Input enregistrés dans les script
     * GestionnaireRéseau et GestionnaireInputs.
     * 3. Correction du vecteur de rotation pour garder seulement la rotation Y pour le personnage (la capsule)
     * 4.Calcul du vecteur de direction du déplacement en utilisant les données de Input enregistrés.
     * Avec cette formule,il y a un déplacement latéral (strafe) lié  à l'axe horizontal (mouvementInput.x)
     * Le vecteur est normalisé pour être ramené à une longueur de 1.
     * Appel de la fonction Move() du networkCharacterController (fonction préexistante)
     * 5.Si les données enregistrées indiquent un saut, on appelle la fonction Jump() du script
     * networkCharacterController (fonction préexistante)
     */
    public void DemandeRespawn()
    {
        respawnDemande = true;
    }
    public override void FixedUpdateNetwork()
    {
        //Si on est sur le serveur et qu'un respawn a été demandé, on appele la fonction Respawn()
        if (Object.HasStateAuthority && respawnDemande)
        {
            Respawn();
            return;
        }
        // Si le joueur est mort, on sort du script immédiatement
        if (gestionnairePointsDeVie.estMort)
            return;

        // 1.
        GetInput(out DonneesInputReseau donneesInputReseau);
        // Déplacement seulement si la partie est en cours
        if (GameManager.partieEnCours)
        { // Ne pas oublier de fermer l'accolade plus bas.
          //2.
            transform.forward = donneesInputReseau.vecteurDevant;
        }

        //2.
        transform.forward = donneesInputReseau.vecteurDevant;
        //3.
        Quaternion rotation = transform.rotation;
        rotation.eulerAngles = new Vector3(0, rotation.eulerAngles.y, 0);
        transform.rotation = rotation;

        //4.
        Vector3 directionMouvement = transform.forward * donneesInputReseau.mouvementInput.y + transform.right * donneesInputReseau.mouvementInput.x;
        directionMouvement.Normalize();
        networkCharacterController.Move(directionMouvement);

        //5.saut, important de le faire après le déplacement
        if (donneesInputReseau.saute) networkCharacterController.Jump();

    }
    /* Fonction qui appelle la fonction TeleportToPosition du script networkCharacterControllerPrototypeV2
    * 1. Téléporte à un point aléatoire et modifie la variable respawnDemande à false
    * 2. Appelle la fonction Respawn() du script gestionnairePointsDeVie
    */
    void Respawn()
    {
        //1.
        ActivationCharacterController(true);
        networkCharacterController.Teleport(Utilitaires.GetPositionSpawnAleatoire());
        respawnDemande = false;
        //2.
        gestionnairePointsDeVie.Respawn();
    }

    /* Fonction publique qui active ou désactive le script networkCharacterControllerPrototypeV2
     */
    public void ActivationCharacterController(bool estActif)
    {
        networkCharacterController.enabled = estActif;
    }


}