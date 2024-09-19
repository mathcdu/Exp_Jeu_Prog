using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
/* 
 * 1.Les objets réseau ne doivent pas dériver de MonoBehavior, mais bien de NetworkBehavior
 * Importation de l'interface IPlayerLeft
 * 2.Variable pour mémoriser l'instance du joueur
 * 3.Fonction Spawned() : Semblable au Start(), mais pour les objets réseaux
 * Sera exécuté lorsque le personnage sera créé (spawn)
* Test si le personnage créé est le personnage contrôlé par l'utilisateur local.
 * HasInputAuthority permet de vérifier cela.
 * Retourne true si on est sur le client qui a généré la création du joueur
 * Retourne false pour les autres clients
 * 4. Lorsqu'un joueur se déconnecte du réseau, on élimine (Despawn) son joueur.
 */
public class JoueurReseau : NetworkBehaviour, IPlayerLeft, IDespawned //1.
{
    //Variable réseau (Networked) contenant le nom du joueur (sera synchronisée)
    [Networked] public string monNom { get; set; }
    [Networked] public Color maCouleur { get; set; }
    //Lorsqu'un joueur est prêt pour une nouvelle partie (il appuyé sur R), on met cette variable à true
    // ce qui déclenchera l'appel de la fonction OnPretAReprendre sur tous les clients connectés.
    [Networked, OnChangedRender(nameof(OnPretAReprendre))] public bool pretNouvellePartie { get; set; }

    // Variable est mise à true lorsque tous les joueurs sont prêts à reprendre une nouvelle partie
    // Il s'agit d'une variable synchronisée sur toues les clients. Lorsqu'un changement est détecté
    // la fonctionne OnNouvellePartie() sera exécutée.

    // Variable pour le pointage (nombre de boules rouge) du joueur qui sera automatiquement synchronisé par le serveur sur tous les clients
    // Lorsqu'un chanegement est détecté, la fonction OnChangementPointage sera automatiquement appelée pour faire
    // une mise à jour de l'affichage du texte.
    [Networked, OnChangedRender(nameof(OnChangementPointage))] public int nbBoulesRouges { get; set; }

    // Variable pour mémoriser la zone de texte au dessus de la tête du joueur et qui afficher le pointage
    // Cette variable doit être définie dans l'inspecteur de Unity
    public TextMeshProUGUI affichagePointageJoueur;
    public static JoueurReseau Local; //.2
    public static int nbClientsPret; // Pour compteur le nombre de joueurs qui sont prêts à reprendre
    // Référence au script GestionnaireInput (pour savoir si touche R a été enfoncée)
    GestionnaireInputs gestionnaireInputs;
    public Transform modeleJoueur;

    /*
     * Au départ, on change la couleur du joueur. La variable maCouleur sera définie
     * par le serveur dans le script GestionnaireReseau.La fonction Start() sera appelée après la fonction Spawned().
     */
    private void Start()
    {
        GetComponentInChildren<MeshRenderer>().material.color = maCouleur;
        gestionnaireInputs = GetComponent<GestionnaireInputs>(); //On récupère le component GestionnaireInput
    }
    /* Fonction appelée par le GameManager lorsque tous les joueurs sont prêts et qu'il faut relancer
   une nouvelle partie.
  */
    public bool Recommence()
    {
        //1.
        GetComponent<GestionnaireInputs>().pretARecommencer = false;
        nbBoulesRouges = 0;
        pretNouvellePartie = false;
        nbClientsPret = 0;
        //2.
        if (Runner.SessionInfo.PlayerCount == 1)
        {
            GameManager.instance.AfficheAttenteAutreJoueur(true);
            return true;
        }
        //3.
        return false;
    }

    /* Le joueur vérifie si la partie est terminée et qu'il n'est pas déjà prêt à reprendre. Si c'est le cas:
     - On va chercher les dernier input et on vérifie si pretARejouer = true. Ce sera le cas si le joueur à appuyé
     sur la touche R.
     - Si le joueur est prêt à rejouer :
     - On met la variable réseau PretNouvelle partie à true. La variable est synchronisée sur tous les clients
     qui appeleront la fonction OnPretAReprendre du JoueurReseau. On remet les variables pretARecommencer du
     gestionneInput à false et pretArejouer du donneesInputReseau à false également.
     */
    public override void FixedUpdateNetwork()
    {
        if (!GameManager.partieEnCours && !pretNouvellePartie)
        {
            GetInput(out DonneesInputReseau donneesInputReseau);
            if (donneesInputReseau.pretARejouer)
            {
                pretNouvellePartie = true;
                gestionnaireInputs.pretARecommencer = false;
                donneesInputReseau.pretARejouer = false;
            }
        }
    }
    /* Fonction appelée automatiquement sur tous les clients lorsque la variable pretNouvellePartie est modifée.
   - Le reste du code s'exécute seulement sur le serveur (if(Runner.IsServer))
   - On ajoute 1 à la variable nbClientsPret;
   - On récupère le nombre de joueurs connectés;
   - Si tout le monde est prête, on appelle la fonction RPC_OnNouvellePartie() qui est un Remote Procedure Call
   */
    public void OnPretAReprendre()
    {
        if (Runner.IsServer)
        {
            nbClientsPret++;
            int nbJoueursTotal = Runner.SessionInfo.PlayerCount;
            if (nbClientsPret >= nbJoueursTotal)
            {
                RPC_OnNouvellePartie();
            }
        }
    }
    /* Fonction RPC (remote procedure call) qui sera exécuté par tous les clients (RpcTargets.All))
   Tous les joueurs connectés exécuteront ainsi la fonction du GameManager DebutNouvellePartie
   */
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_OnNouvellePartie(RpcInfo infos = default)
    {
        GameManager.instance.DebutNouvellePartie();
    }

    /* Fonction appelée lorsque la variable réseau recommence = true.
    1. Si c'est le joueur local (hasInputAuthority), on désactive les panneux de victoire et d'attente
    2. Si la variable recommence est bien égale à true, on remet différentes variales à leur valeur
    de base, c'est-à-dire celle qu'elles doivent avoir en début de partie, comme le nbBoulesRouges = 0;
   */


    public override void Spawned() //3.
    {
        // À sa création, le joueur ajoute sa référence (son script JoueurReseau) et son pointage (var nbBoulesRouges) au dictionnaire
        // du GameManager.
        GameManager.joueursPointagesData.Add(this, nbBoulesRouges);

        if (Object.HasInputAuthority)
        {
            Local = this;

            //Si c'est le joueur du client, on appel la fonction pour le rendre invisible
            Utilitaires.SetRenderLayerInChildren(modeleJoueur, LayerMask.NameToLayer("JoueurLocal"));

            /*À la création du joueur et s'il est le joueur local (HasInputAuthority), ont doit défénir son nom en allant
            chercher la variable nomJoueurLocal du GameManager.
            Pour que le nom soit synchronisé sur tous les clients, appelle d'une fonction RPC (RemoteProcedureCall) qui permet
            de dire à tous les clients d'exécuter la fonction  "RPC_ChangementdeNom"
            */
            monNom = GameManager.nomJoueurLocal;
            RPC_ChangementdeNom(monNom);

            //On désactive la mainCamera. Assurez-vous que la caméra de départ possède bien le tag MainCamera
            Camera.main.gameObject.SetActive(false);

            Debug.Log("Un joueur local a été créé");

            // on affiche le nom du joueur créé et son pointage
            affichagePointageJoueur.text = $"{monNom}:{nbBoulesRouges.ToString()}";



        }
        else
        {
            //Si le joueur créé est contrôlé par un autre joueur, on désactive le component caméra de cet objet
            Camera camLocale = GetComponentInChildren<Camera>();
            camLocale.enabled = false;

            // On désactive aussi le component AudioListener
            AudioListener audioListener = GetComponentInChildren<AudioListener>();
            audioListener.enabled = false;
            Debug.Log("Un joueur réseau a été créé");
        }
        /* Au moment où un joueur est crée, on vérifie s'il est seul. Si c'est le cas, on appelle la fonction
            AfficheAttenteAutreJoueur du GameManager. S'il y a plus d'un joueur, on appelle la fonction qui permet
            de créer les boules rouges. Notez bien que la fonction NouvellesBoulesRouges sera appelée uniquement
            par le serveur (Runner.IsServer)
            */
        print("Il y a " + Runner.SessionInfo.PlayerCount + " joueurs connectés");
        if (Runner.SessionInfo.PlayerCount == 1)
        {
            GameManager.instance.AfficheAttenteAutreJoueur(true);
        }
        else if (Runner.SessionInfo.PlayerCount > 1)
        {
            print("devrait générer des boules rouges");
            GameManager.instance.AfficheAttenteAutreJoueur(false);
            if (Runner.IsServer) GameManager.instance.NouvellesBoulesRouges();
        }

        // on affiche le nom du joueur créé et son pointage
        affichagePointageJoueur.text = $"{monNom}:{nbBoulesRouges.ToString()}";
    }

    public void PlayerLeft(PlayerRef player) //.4
    {
        if (player == Object.InputAuthority)
        {
            Runner.Despawn(Object);
        }
    }

    /* Fonction appelée automatiquement lorsqu'un changement est détecté dans la variable nbBoulesRouges du joueur (variable Networked)
    Mise à jour du pointage du joueur qui sera égal au nombre de boules rouges ramassées
    */
    public void OnChangementPointage()
    {
        affichagePointageJoueur.text = $"{monNom}:{nbBoulesRouges.ToString()}";

        // On vérifie si le nombre de boules rouge == l'objectif de points à atteindre
        // Si oui, on appelle la fonction FinPartie en passant le nom du joueur gagnant.
        // Cette fonction sera appelée dans le script du gagnant, sur tous les clients connectés
        if (nbBoulesRouges >= GameManager.instance.objectifPoints)
        {
            GameManager.instance.FinPartie(monNom);
        }
    }

    /* Fonction RPC (RemoteProcedureCall) déclenché par un joueur local qui permet la mise à jour du nom du joueur
    sur tous les autres clients. La source (l'émetteur) est le joueur local (RpcSources.InputAuthority). La cible est tous les joueurs
    connectés (RpcTargets.All). Le paramètre reçu contient le nom du joueur à défénir.
    Pour bien comprendre : Mathieu se connecte au serveur en inscrivant son nom. Il envoir un message à tous les autres clients. Sur
    chaque client, le joueur contrôlé par Mathieu exécutera cette fonction ce qui permettra une mise à jour du nom.
    1. On définit la variable nomNom
    2. On affiche le nom et le poitage au dessus de la tête du joueur.
    */
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_ChangementdeNom(string leNom, RpcInfo infos = default)
    {
        //1.
        monNom = leNom;
        //2.
        affichagePointageJoueur.text = $"{monNom}:{nbBoulesRouges.ToString()}";
    }
    // Fonction exécutée lorsqu'un JoueurReseau est despawned. Soit lorsqu'il quitte volontairement ou
    // encore quand la connection au serveur est interrompue pour une autre raison.
    // Quand cela se produit, on s'assure de mettre à jour notre dictionnaire JoueursPointagesData en
    // supprimant la référence au joueur déconnecté.
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        GameManager.joueursPointagesData.Remove(this);
    }
}
