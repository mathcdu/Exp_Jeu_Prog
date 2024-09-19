using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion; // ne pas oublier ce namespace

/* Script qui gère le tir du joueur qui dérive de NetworkBehaviour
* Variables :
* - ilTir : variable réseau [Networked] qui sera synchronisée sur tous les clients
* - detecteurDeChangements : variable de type ChangeDetector (propre à Fusion) qui permet de récupérer
les changements de variables réseau.
* - tempsDernierTir : pour limiter la cadence de tir
* - delaiTirLocal : delai entre 2 tir (local)
* - delaiTirServeur:delai entre 2 tir (réseau)
*
* - origineTir : point d'origine du rayon généré pour le tir (la caméra)
* - layersCollisionTir : layers à considérer pour la détection de collision.
*   En choisir deux dans l'inspecteur: Default et HitBoxReseau
* - distanceTir : la distance de portée du tir
*
* - particulesTir : le système de particules à activer à chaque tir. Définir dans l'inspecteur en
* glissant le l'objet ParticulesTir qui est enfant du fusil
*/

public class GestionnaireArmes : NetworkBehaviour
{
    [Networked] public bool ilTir { get; set; } // variable réseau peuvent seulement être changée par le serveur (stateAuthority)
    ChangeDetector detecteurDeChangements;
    float tempsDernierTir = 0;
    float delaiTirLocal = 0.15f;
    float delaiTirServeur = 0.1f;

    // pour le raycast
    public Transform origineTir; // définir dans Unity avec la caméra
    public LayerMask layersCollisionTir; // définir dans Unity
    public float distanceTir = 100f;

    public ParticleSystem particulesTir;
    JoueurReseau joueurReseau; // référence au script JoueurReseau

    /*
     * On garde en mémoire le component (script) JoueurReseau pour pouvoir
     * communiquer avec lui.
     */
    void Awake()
    {
        joueurReseau = GetComponent<JoueurReseau>();
    }

    /*
     * On définit la variable detecteurDeChangements. On utilise une commande propre a Fusion qui nous permettra
     de vérifier les changements des variables réseau.
     */
    public override void Spawned()
    {
        detecteurDeChangements = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    /*
     * Fonction qui détecte le tir et déclenche tout le processus
     * On récupère les données enregistrées dans la structure de données donneesInputReseau et on
     * vérifie la variable appuieBoutonTir. Si elle est à true, on active la fonction TirLocal en passant
     * comme paramètre le vector indiquant le devant du personnage.
     */
    public override void FixedUpdateNetwork()
    {

        if (GetInput(out DonneesInputReseau donneesInputReseau))
        {
            if (donneesInputReseau.appuieBoutonTir)
            {
                TirLocal(donneesInputReseau.vecteurDevant);
            }
        }
    }

    /* Gestion local du tir (sur le client seulement)
    * 1.On sort de la fonction si le tir ne respecte pas le délais entre 2 tir.
    * 2.Appel de la coroutine qui activera les particules et lancera le Tir pour le réseau (autres clients)
    * 3.Raycast réseau propre à Fusion avec une compensation de délai.
    * Paramètres:
    *   - origineTir.position (vector3) : position d'origine du rayon;
    *   - vecteurDevant (vector3) : direction du rayon;
    *   - distanceTir (float) : longueur du rayon
    *   - Object.InputAuthority : Indique au serveur le joueur à l'origine du tir
    *   - out var infosCollisions : variable pour récupérer les informations si le rayon touche un objet
    *   - layersCollisionTir : indique les layers sensibles au rayon. Seuls les objets sur ces layers seront considérés.
    *   - HitOptions.IncludePhysX : précise quels type de collider sont sensibles au rayon.IncludePhysX permet
    *   de détecter les colliders normaux en plus des collider fusion de type Hitbox.
    * 4.Vérification du type d'objet touché par le rayon.
    * - Si c'est un hitbox (objet réseau), on change la variable toucheAutreJoueur
    * - Si c'est un collider normal, on affiche un message dans la console
    * 5.Mémorisation du temps du tir. Servira pour empêcher des tirs trop rapides.

    */
    void TirLocal(Vector3 vecteurDevant)
    {
        //1.
        if (Time.time - tempsDernierTir < delaiTirLocal) return;

        //2.
        StartCoroutine(EffetTirCoroutine());

        //3.
        Runner.LagCompensation.Raycast(origineTir.position, vecteurDevant, distanceTir, Object.InputAuthority, out var infosCollisions, layersCollisionTir, HitOptions.IgnoreInputAuthority);

        //4.
        if (infosCollisions.Hitbox != null)
        {
            // si nous sommes sur le code exécuté sur le serveur :
            // On appelle la fonction PersoEstTouche du joueur touché dans le script GestionnairePointsDeVie
            if (Object.HasStateAuthority)
            {
                infosCollisions.Hitbox.transform.root.GetComponent<GestionnairePointsDeVie>().PersoEstTouche(joueurReseau, 1);
            }
        }
        //5.
        tempsDernierTir = Time.time;
    }

    /* Coroutine qui déclenche le système de particules localement et qui gère la variable bool ilTir en l'activant
     * d'abord (true) puis en la désactivant après un délai définit dans la variable delaiTirServeur.
     */
    IEnumerator EffetTirCoroutine()
    {
        ilTir = true; // comme la variable networked est changé, la fonction OnTir sera appelée (voir fonction Render plus bas)
        if (Object.HasInputAuthority)
        {
            if (!Runner.IsResimulation) particulesTir.Play(); // pour que les particules soient activées une seule fois
        }
        yield return new WaitForSeconds(delaiTirServeur);

        ilTir = false;
    }

    /* Fonction Render (semblable au update) dans laquelle on utilise la variable de type ChangeDetector
    "detecteurDeChangements" pour gérer le tir d'un joueur sur les autres clients
    - Le foreach permet de récupérer tout les changements qu'il y a eu dans les variables networked. La détection de changement
    permet de récupérer la nouvelle valeur de la variable, mais aussi la valeur qu'elle avait auparavant.
    -À l'aide d'un switch, on vérifie si le changement concerne la variable ilTir. Si c'est le cas, on récupère
    la nouvelle et l'ancienne valeur de cette variable de type bool.
    - On déclenche la fonction OnTir en passant la nouvelle et l'ancienne valeur de la variable ilTir.
        */
    public override void Render()
    {
        foreach (var change in detecteurDeChangements.DetectChanges(this, out var previousBuffer, out var currentBuffer))
        {
            switch (change)
            {
                case nameof(ilTir):
                    var boolReader = GetPropertyReader<bool>(nameof(ilTir));
                    var (previousBool, currentBool) = boolReader.Read(previousBuffer, currentBuffer);
                    OnTir(previousBool, currentBool);
                    break;
            }
        }
    }

    /* Fonction appelée par le serveur lorsque la variable ilTir est modifiée
     * On appelle la fonction TirDistant() seulement si la variable ilTire actuelle est = true alors qu'elle était = false juste
     avant. Cela permet d'éviter que le joueur tir plus d'une fois.
     */
    void OnTir(bool ilTirValeurAncienne, bool ilTirValeurActuelle)
    {
        if (ilTirValeurActuelle && !ilTirValeurAncienne) // pour tirer seulement une fois
            TirDistant();
    }

    /* Fonction qui permet d'activer le système de particule pour le personnage qui a tiré
    * sur tous les client connectés. Sur l'ordinateur du joueur qui a tiré, l'activation du système
    * de particules à déjà été faite dans la fonction TirLocal(). Il faut cependant s'assurer que ce joueur
    * tirera aussi sur l'ordinateur des autres joueurs.
    * On déclenche ainsi le système de particules seulement si le client ne possède pas le InputAuthority
    * sur le joueur.
    *
    */
    void TirDistant()
    {
        //seulement pour les objets distants (par pour le joueur local car c'est déjà fait)
        if (!Object.HasInputAuthority)
        {
            particulesTir.Play();
        }
    }
}

