using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.UI;

/* Script qui gère les dommages et autre effets quand un joueur est touché
* Variables :
*
* ##### Pour la gestion des points de vie ################################################
* - ptsVie : variable Networked de type byte (moins lourd qu'un int) pour les points de vie du joueur.
* - estMort : variable bool pour savoir si le joueur est mort ou pas.
* - detecteurDeChangements : variable de type ChangeDetector propre à Fusion. Permet de récupérer les changements de variables networked
* - estInitialise : pour savoir si le joueur est initialisé.
* - ptsVieDepart : le nombre de points de vie au commencement ou après un respawn
*
* ##### Pour les effets de changement de couleur quand le perso est touché ###############
* - uiCouleurTouche:la couleur de l'image quand le perso est touché
* - uiImageTouche : l'image qui s'affiche quand le perso est touché
* - persoRenderer : référence au meshrenderer du perso. Servira à changer la couleur du matériel
* - couleurNormalPerso : la couleur normal du perso
*
* ##### Pour gérer la mort du perso ###############
* - modelJoueur : référence au gameObject avec la partie visuelle du perso
* - particulesMort_Prefab : référence au Prefab des particules de mort à instancier à la mort du perso
* - particulesMateriel : référence au matériel utilisé par les particules de morts
* - hitboxRoot : référence au component photon HitBoxRoot servant à la détection de collision
* - gestionnaireMouvementPersonnage : référence au script gestionnaireMouvementPersonnage sur le perso
* - joueurReseau : référence au script joueurReseau sur le perso
*/

public class GestionnairePointsDeVie : NetworkBehaviour
{
    [Networked] byte ptsVie { get; set; } //(byte : valeur possible entre 0 et 255, aucune valeur négative)
    [Networked] public bool estMort { get; set; }
    ChangeDetector detecteurDeChangements;
    bool estInitialise = false;
    const byte ptsVieDepart = 5;
    public Color uiCouleurTouche; //à définir dans l'inspecteur
    public Image uiImageTouche;//à définir dans l'inspecteur
    public MeshRenderer persoRenderer;//à définir dans l'inspecteur
    Color couleurNormalPerso;
    public GameObject modelJoueur;//à définir dans l'inspecteur
    public GameObject particulesMort_Prefab;//à définir dans l'inspecteur
    public Material particulesMateriel;//à définir dans l'inspecteur
    HitboxRoot hitboxRoot;
    GestionnaireMouvementPersonnage gestionnaireMouvementPersonnage;
    JoueurReseau joueurReseau;

    /*
     * On garde en mémoire la référence au component HitBoxRoot ainsi que les références à deux
     * components (scripts) sur le perso : GestionnaireMouvementPersonnage et JoueurReseau
     */
    private void Awake()
    {
        hitboxRoot = GetComponent<HitboxRoot>();
        gestionnaireMouvementPersonnage = GetComponent<GestionnaireMouvementPersonnage>();
        joueurReseau = GetComponent<JoueurReseau>();
    }

    /*On définit la variable detecteurDeChangements. On utilise une commande propre a Fusion qui nous permettra
    de vérifier les changements des variables réseau.
    */
    public override void Spawned()
    {
        detecteurDeChangements = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    /*
     * Initialisation des variables à l'apparition du personnage. On garde aussi en mémoire la couleur
     * du personnage.
     */
    void Start()
    {
        ptsVie = ptsVieDepart;
        estMort = false;
        estInitialise = true;
        couleurNormalPerso = persoRenderer.material.color;
    }

    /* Fonction publique appelée uniquement par le serveur dans le script GestionnairesArmes du joueur qui
     * a tiré.
     * 1. On quitte la fonction immédiatement si le joueur touché est déjà mort
     * 2. Soustraction d'un point de vie.
     * 3. Si le joueur touché avait des points (boules rouges ramassées) :
        - On soustrait un point à son total;
     * 4. Si les points de vie sont à 0 (ou moins), la variable estMort est mise à true et on appelle
     * la coroutine RessurectionServeur_CO qui gérera un éventuel respawn du joueur
     * Important : souvenez-vous que les variables ptsVie et estMort sont de type [Networked] et qu'une
     * fonction sera automatiquement appelée lorsque leur valeur change (voir fonction Render() plus bas)
    */
    public void PersoEstTouche(JoueurReseau dommageFaitParQui, byte dommage)
    {
        //1.
        if (estMort)
            return;
        //2.
        if (dommage > ptsVie)
            dommage = ptsVie;

        ptsVie -= dommage;

        // Perte d'un point
        if (joueurReseau.nbBoulesRouges > 0)
        {
            joueurReseau.nbBoulesRouges--;
            GameManager.instance.AjoutBoulesRouges(1); //Création d'une nouvelle boule rouge
        }
        //4.
        if (ptsVie <= 0)
        {
            StartCoroutine(RessurectionServeur_CO());
            estMort = true;
            GameManager.instance.AjoutBoulesRouges(joueurReseau.nbBoulesRouges); //Création de nouvelles boules rouges
            joueurReseau.nbBoulesRouges = 0;
        }
    }

    /* Enumarator qui attend 2 secondes et qui appelle ensuite la fonction DemandeRespawn
     * du script gestionnaireMouvementPersonnage.
    */
    IEnumerator RessurectionServeur_CO()
    {
        yield return new WaitForSeconds(2);
        gestionnaireMouvementPersonnage.DemandeRespawn();
    }

    /* Fonction Render (semblable au update) dans laquelle on utilise la variable de type ChangeDetector
    "detecteurDeChangements" pour gérer la modification aux variables ptsVie et estMort.
    - Le foreach permet de récupérer tout les changements qu'il y a eu dans les variables networked. La détection de changement
    permet de récupérer la nouvelle valeur de la variable, mais aussi la valeur qu'elle avait auparavant.
    -À l'aide d'un switch, on vérifie si le changement concerne la variable ptsVie ou estMort. Dans les 2 cas, on récupère
    la nouvelle et l'ancienne valeur de la variable et on appelle une fonction (OnPtsVieChange ou OnChangeEtat) en passant les
    deux valeurs de la variable en paramètres(la nouvelle et l'ancienne).
    */
    public override void Render()
    {
        foreach (var change in detecteurDeChangements.DetectChanges(this, out var previousBuffer, out var currentBuffer))
        {
            switch (change)
            {
                case nameof(ptsVie):
                    var byteReader = GetPropertyReader<byte>(nameof(ptsVie));
                    var (previousByte, currentByte) = byteReader.Read(previousBuffer, currentBuffer);
                    OnPtsVieChange(previousByte, currentByte);
                    break;

                case nameof(estMort):
                    var boolReader = GetPropertyReader<bool>(nameof(estMort));
                    var (previousBool, currentBool) = boolReader.Read(previousBuffer, currentBuffer);
                    OnChangeEtat(previousBool, currentBool);
                    break;
            }
        }
    }

    /* Fonction appelée par la fonction Render quand la variable ptsVie est modifiées.
    - On quitte la fonction si le joueur n'est pas initialisé encore;
    - Si la nouvelle valeut de ptsVie est plus petite que l'ancienne valeur :
        - On appelle la coroutine EffetTouche_CO qui s'occupera des effets visuels lorsqu'un personnage est touché.
     */
    void OnPtsVieChange(byte ancienPtsVie, byte nouveauPtsvie)
    {
        if (!estInitialise) return;

        if (nouveauPtsvie < ancienPtsVie)
        {
            StartCoroutine(EffetTouche_CO());
        }
    }

    /* Coroutine qui gère les effets visuels lorsqu'un joueur est touché.
     * 1. Changement de la couleur du joueur pour blanc
     * 2. Changement de la couleur de l'image servant à indiquer au joueur qu'il est touché.
     *    Cette commande est effectuée seulement sur le client qui contrôle le joueur touché
     * 3. Après un délai de 0.2 secondes, on remet la couleur normale au joueur touché
     * 4. On change la couleur de l'image servant à indiquer au joueur qu'il est touché. L'important dans
     *    cette commande est qu'on met la valeur alpha à 0 (complètement transparente) pour la faire disparaître.
     *    Cette commande est effectuée seulement sur le client qui contrôle le joueur touché et que le joueur
     *    touché n'est pas mort.
    */
    IEnumerator EffetTouche_CO()
    {
        //.1
        persoRenderer.material.color = Color.white; // pour tous les clients
                                                    //2.
        if (Object.HasInputAuthority) // seulement pour le joueur qui controle et qui s'est fait touché
            uiImageTouche.color = uiCouleurTouche;
        //3.
        yield return new WaitForSeconds(0.2f);
        persoRenderer.material.color = couleurNormalPerso;
        //4.
        if (Object.HasInputAuthority && !estMort)
            uiImageTouche.color = new Color(0, 0, 0, 0);
    }

    /* Fonction appelée automatiquement lorsque que la variable [Networked] estMort est modifiée (voir fonction Render)
     * Appel de la fonction Mort() seulement quand la valeur actuelle de la variable estMort est true
     * Appel de la fonction Resurection() quand la valeur actuelle de la variable estMort est false
     * et que l'ancienne valeur de la variable estMort est true. Donc, quand le joueur était mort et qu'on
     * change la variable estMort pour la mettre à false.
     */
    void OnChangeEtat(bool estMortAncien, bool estMortNouveau)
    {
        if (estMortNouveau)
        {
            Mort();
        }
        else if (!estMortNouveau && estMortAncien)
        {
            Resurrection();
        }
    }

    /* Fonction appelée à la mort du personnage par la fonction OnChangeEtat
     * 1. Désactivation du joueur et de son hitboxroot qui sert à la détection de collision
     * 2. Appelle de la fonction ActivationCharacterController(false) dans le scriptgestionnaireMouvementPersonnage
     * pour désactiver le CharacterConroller.
     * 3. Instanciation d'un système de particules (particulesMort_Prefab) à la place du joueur. On modifie
     * la couleur du matériel des particules en lui donnant la couleur du joueur qui meurt. Les particules
     * sont détruites après un délai de 3 secondes.
     */
    private void Mort()
    {
        //1.
        modelJoueur.gameObject.SetActive(false);
        hitboxRoot.HitboxRootActive = false;
        //2.
        gestionnaireMouvementPersonnage.ActivationCharacterController(false);
        //3.
        GameObject nouvelleParticule = Instantiate(particulesMort_Prefab, transform.position, Quaternion.identity);
        particulesMateriel.color = joueurReseau.maCouleur;
        Destroy(nouvelleParticule, 3);
    }

    /* Fonction appelée après la mort du personnage, lorsque la variable estMort est remise à false
     * 1. On change la couleur de l'image servant à indiquer au joueur qu'il est touché. L'important dans
     *    cette commande est qu'on met la valeur alpha à 0 (complètement transparente) pour la faire disparaître.
     *    Cette commande est effectuée seulement sur le client qui contrôle le joueur
     * 2. On active le hitboxroot pour réactiver la détection de collisions
     * 3. Appelle de la fonction ActivationCharacterController(true) dans le scriptgestionnaireMouvementPersonnage
     *    pour activer le CharacterConroller.
     * 4. Appel de la coroutine (JoueurVisible) qui réactivera le joueur
     */
    private void Resurrection()
    {
        //1.
        if (Object.HasInputAuthority)
            uiImageTouche.color = new Color(0, 0, 0, 0);
        //2.
        hitboxRoot.HitboxRootActive = true;
        //3.
        gestionnaireMouvementPersonnage.ActivationCharacterController(true);
        //4.
        StartCoroutine(JoueurVisible());
    }

    /* Coroutine qui réactive le joueur après un délai de 0.1 seconde */
    IEnumerator JoueurVisible()
    {
        yield return new WaitForSeconds(0.1f);
        modelJoueur.gameObject.SetActive(true);
    }

    /* Fonction publique appelée par le script GestionnaireMouvementPersonnage
     * Réinitialise les points de vie
     * Change l'état (la variable) estMort pour false
     */
    public void Respawn()
    {
        ptsVie = ptsVieDepart;
        estMort = false;
    }

}