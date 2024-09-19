using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System;

public class GestionnaireReseau : MonoBehaviour, INetworkRunnerCallbacks
{


    public void OnConnectedToServer(NetworkRunner runner)
    {

    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {

    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {

    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {

    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {

    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {

    }

    /* Fonction du Runner pour définir les inputs du client dans la simulation
    * 1. On récupère le component GestionnaireInputs du joueur local
    * 2. On définit (set) le paramètre input en lui donnant la structure de données (struc) qu'on récupère
    * en appelant la fonction GestInputReseau du script GestionnaireInputs. Les valeurs seront mémorisées
    * et nous pourrons les utilisées pour le déplacement du joueur dans un autre script. Ouf...*/
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        //1.
        if (gestionnaireInputs == null && JoueurReseau.Local != null)
        {

            gestionnaireInputs = JoueurReseau.Local.GetComponent<GestionnaireInputs>();
        }

        //2.
        if (gestionnaireInputs != null)
        {
            input.Set(gestionnaireInputs.GetInputReseau());
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {

    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (_runner.IsServer)
        {
            Debug.Log("Un joueur s'est connecté comme serveur. Spawn d'un joueur");
            JoueurReseau leNouveuJoueur = _runner.Spawn(joueurPrefab, Utilitaires.GetPositionSpawnAleatoire(),
                                          Quaternion.identity, player);
            /*On change la variable maCouleur du nouveauJoueur et on augmente le nombre de joueurs connectés
            Comme j'ai seulement 10 couleurs de définies, je m'assure de ne pas dépasser la longueur de mon
            tableau*/
            leNouveuJoueur.maCouleur = couleurJoueurs[nbJoueurs];
            nbJoueurs++;
            if (nbJoueurs >= 10) nbJoueurs = 0;
        }
        else
        {
            Debug.Log("Un joueur s'est connecté comme client. Spawn d'un joueur");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {

    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {

    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {

    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {

    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {

    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {

    }


    /*
        * Fonction appelée lorsqu'une connexion réseau est refusée ou lorsqu'un client perd
        * la connexion suite à une erreur réseau. Le paramètre ShutdownReason est une énumération (enum)
        * contenant différentes causes possibles.
        * Ici, lorsque la connexion est refusée car le nombre maximal de joueurs est atteint, on appelle la
        * fonction NavigationPanel du GameManager en passant la valeur true en parmètre.
        */
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (shutdownReason == ShutdownReason.GameIsFull)
        {
            GameManager.instance.NavigationPanel(true);
        }
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {

    }


    // Update is called once per frame
    void Update()
    {

    }
    //Contient une référence au component NetworkRunner
    NetworkRunner _runner;
    //Index de la scène du jeu

    // pour mémoriser le component GestionnaireMouvementPersonnage du joueur
    GestionnaireInputs gestionnaireInputs;
    public int IndexSceneJeu;
    //Contient la référence au component NetworkRunner
    public JoueurReseau joueurPrefab;
    public SphereCollision sphereCollision; // référence au prefab de la boule rouge
    public bool spheresDejaSpawn; // Permet de savoir les boules ont déjà été créées.

    // Tableau de couleurs à définir dans l'inspecteur
    public Color[] couleurJoueurs;
    // Pour compteur le nombre de joueurs connectés
    public int nbJoueurs = 0;

    void Start()
    {
        // Création d'une partie dès le départ
        //CreationPartie(GameMode.AutoHostOrClient);
    }

    // Fonction asynchrone pour démarrer Fusion et créer une partie
    public async void CreationPartie(GameMode mode)
    {
        /*  1.Mémorisation du component NetworkRunner . On garde en mémoire
            la référence à ce component dans la variable _runner.
            2.Indique au NetworkRunner qu'il doit fournir les entrées (inputs) au 
            simulateur (Fusion)
        */
        _runner = gameObject.GetComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        /*Méthode du NetworkRunner qui permet d'initialiser une partie
         * GameMode : reçu en argument. Valeur possible : Client, Host, Server,
           AutoHostOrClient, etc.)
         * SessionName : Nom de la chambre (room) pour cette partie
         * Scene : la scène qui doit être utilisée pour la simulation
         * SceneManager : référence au component script
          NetworkSceneManagerDefault qui est ajouté au même moment
         */
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = GameManager.instance.nomDeLapartie,
            Scene = SceneRef.FromIndex(IndexSceneJeu),
            PlayerCount = GameManager.instance.nombreDeJoueurMax,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }
    /* Fonction exécuté sur le serveur seulement qui spawn le nombre de boules rouges déterminés au lancement
   d'une nouvelle partie.
   */
    public void CreationBoulleRouge()
    {
        print("CreationBoulleRouge entrée fonction");
        if (_runner.IsServer && !spheresDejaSpawn)
        {
            print("CreationBoulleRouge dans le if");
            GameManager.partieEnCours = true;
            for (int i = 0; i < GameManager.instance.nbBoulesRougesDepart; i++)
            {
                _runner.Spawn(sphereCollision, Utilitaires.GetPositionSpawnAleatoire(), Quaternion.identity);
            }
            spheresDejaSpawn = true;
        }
    }
    /*Fonction appelé pendant le jeu, lorsqu'il est nécessaire de créer de nouvelles
boule rouges. Réception en paramètre du nombre de boules à créer.
*/
    public void AjoutBoulesRouges(int combien)
    {
        if (_runner.IsServer)
        {
            for (int i = 0; i < combien; i++)
            {
                _runner.Spawn(sphereCollision, Utilitaires.GetPositionSpawnAleatoire(), Quaternion.identity);
            }
        }
    }
}

