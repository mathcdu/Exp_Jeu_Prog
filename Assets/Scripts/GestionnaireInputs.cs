using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Script qui récupère la valeur des inputs (clavier et souris) à chaque update()
 * Dérive de MonoBehavior
 * Exécution locale. Les données seront transmises au réseau sur demande
 * Variables
 * - mouvementInputVecteur :Vector2 pour mémoriser axes vertical et horizontal
 * - vueInputVecteur : Vector2 pour mémoriser les déplacements de la souris, horizontal et vertical.
 * - ilSaute : bool qui sera activée lorsque le joueur saute
 * - GestionnaireCameraLocale : pour mémoriser le component GestionnaireCameraLocale de la caméra du joueur
 */

public class GestionnaireInputs : MonoBehaviour
{
    Vector2 mouvementInputVecteur = Vector2.zero;
    Vector2 vueInputVecteur = Vector2.zero;
    bool ilSaute;
    public bool pretARecommencer; // Variable pour savoir si le joueur est prêt à recommencer
    bool ilTir = false; // Détection locale du clic de souris.
    GestionnaireMouvementPersonnage gestionnaireMouvementPersonnage; // référence au script de mouvement
    GestionnaireCameraLocale gestionnaireCameraLocale;

    /*
     * Avant le Start(), on mémorise la référence au component GestionnaireCameraLocale de la caméra du joueur
     */
    void Awake()
    {
        gestionnaireCameraLocale = GetComponentInChildren<GestionnaireCameraLocale>();
        gestionnaireMouvementPersonnage = GetComponent<GestionnaireMouvementPersonnage>();
    }

    /*
     * On s'assure que le curseur est invisible et verrouillé au centre
     */
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /*
     * On mémorise à chaque frame la valeurs des inputs 
     *  - clavier : Axis Horizontal et vertical 
     *  - souris : Axis Mouse X et Mouse Y
     * Appel de la fonction SetInputVue dans le script GestionnaireCameraLocale en lui passant l'information
     * nécessaire pour la rotation de la caméra (vueInputVecteur)
     * 2.Si la touche de saut est appuyée, on met la variable ilSaute à true.
     */
    void Update()
    {
        // Optimisation : on exécute seulement le Update si le client contrôle ce joueur
        if (!gestionnaireMouvementPersonnage.Object.HasInputAuthority)
            return;
        // Déplacement
        mouvementInputVecteur.x = Input.GetAxis("Horizontal");
        mouvementInputVecteur.y = Input.GetAxis("Vertical");

        // Vue
        vueInputVecteur.x = Input.GetAxis("Mouse X");
        vueInputVecteur.y = Input.GetAxis("Mouse Y");
        gestionnaireCameraLocale.SetInputVue(vueInputVecteur); // ligne a décommenter plus tard

        //Saut
        if (Input.GetButtonDown("Jump"))
            ilSaute = true;

        //Tir
        if (Input.GetButtonDown("Fire1"))
            ilTir = true;

        // Si la partie n'est pas en cours (donc terminée!) on écoute la toucher R. Quand la touche
        // est enfoncée, on met la variable pretARecommencer à true.
        if (!GameManager.partieEnCours)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                pretARecommencer = true;
            }
        }
    }

    /*
     * Fonction qui sera appelée par le Runner qui gère la simulation (GestionnaireReseau). 
     * Lorsqu'elle est appelée, son rôle est de :
     * 1. créer une structure de données (struc) à partir du modèle DonneesInputReseau;
     * 2. définir les trois variables de la structure (mouvement, vecteurDevant et saute);
     * Le vecteur de direction "vecteurDevant" est déterminé par le forward de la cameraFPS
     * Une fois la donnée de saut enregistrée pour le input réseau, on remet la variable ilSaute à false
     * 3. retourne au Runner la structure de données
     */
    public DonneesInputReseau GetInputReseau()
    {
        //1.
        DonneesInputReseau donneesInputReseau = new DonneesInputReseau();

        //2.
        donneesInputReseau.mouvementInput = mouvementInputVecteur;
        donneesInputReseau.vecteurDevant = gestionnaireCameraLocale.gameObject.transform.forward;
        donneesInputReseau.saute = ilSaute;
        donneesInputReseau.appuieBoutonTir = ilTir;
        ilSaute = false;
        ilTir = false;
        donneesInputReseau.pretARejouer = pretARecommencer;
        //3.
        return donneesInputReseau;
    }
}