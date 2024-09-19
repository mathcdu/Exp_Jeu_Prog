using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using System;
public class GameManager : MonoBehaviour
{
    public static GameManager instance; // Référence à l'instance du GameManager
    public int objectifPoints = 10; // Nombre de point pour finir la partie
    public int nbBoulesRougesDepart = 20; // Nombre de boules rouges a spawner au début d'une partie.
    public static bool partieEnCours = true;  // Est que la partie est en cours (static)
    [SerializeField] GestionnaireReseau gestionnaireReseau; // Reférence au gestionnaire réseau
    public static string nomJoueurLocal; // Le nom du joueur local
    public static Dictionary<JoueurReseau, int> joueursPointagesData = new Dictionary<JoueurReseau, int>();
    //Dictionnaire pour mémoriser chaque JoueurReseau et son pointage. Au moment de la création d'un joueur (fonction Spawned() du joueur)
    // il ajoutera lui même sa référence au dictionnaire du GameManager.

    [Header("Éléments UI")]
    public GameObject refCanvasDepart; // Référence au canvas de départ
    public GameObject refCanvasJeu; // Référence au canvas de jeu
    public TextMeshProUGUI refTxtNomJoueur; // Référence à la zone texte contenant le nom du joueur (dans CanvasDepart)
    public TextMeshProUGUI refTxtPointage; // Référence à la zone d'affichage de tous les pointages (dans CanvasJeu)
    public GameObject txtAttenteAutreJoueur; // Texte sous forme de bandeau rouge pour indiquer au joueur qu'il est en attente. À défénir dans l'inspecteur de Unity.
    // Liste static vide de type JoueurReseau qui servira à garder en mémoire tous les
    // joueurs connectés. Sera utilisé entre 2 parties pour gérer la reprise.

    [Header("Éléments UI")]
    public GameObject refPanelGagnant; // Référence au panel affichant le texte du gagnant.
    public TextMeshProUGUI refTxtGagnant; // Référence à la zone de texte pour afficher le nom du gagnant.
    public GameObject refPanelAttente; // Référence au panel affichant le d'attente entre deux partie.
    public string nomDeLapartie; // Le nom de la partie entrée par le joueur
    public int nombreDeJoueurMax; // Le nombre maximum de joueurs décidé par le joueur qui crée la partie

    public TextMeshProUGUI refTextNomPartieJoindre; //Texte entré dans le champs pour rejoindre une partie
    public TextMeshProUGUI refTextNomPartieNouvelle; // Texte entré dans le champs pour créer une nouvelle partie

    // Attention ici, bogue avec TextMesh Pro. Le type TextMeshProUGUI ne permet pas d'utiliser
    //la commande TryParse(). Il faut absolument utiliser le type TMP_InputField
    public TMP_InputField refTextNbJoueursNouvelle; // Référence au nombre de joueurs maximum entré par l'utilisateur
    public GameObject panelNom; // Référence au panel qui demande le nom du joueur
    public GameObject panelChoix; // Référence au panel qui permet au joueur de choisir de créer ou joindre une partie
    public GameObject panelConnexionRefusee; // Référence au panel qui s'affiche si la connexion a une partie est refusée

    // Référence au Prefab GestionnaireReseau. Sera utilisé lorsqu'une connexion a une partie est refusée parce que le nombre
    // de joueur max a été atteint. Dans ce cas, Fusion supprimer le GestionnaireReseau original. Il faudra donc en créer un autre...
    public GameObject gestionnaireReseauSource;

    // Au départ, on définit la variable "instance" qui permettra au autre script de communiquer avec l'instance du GameManager.
    void Awake()
    {
        instance = this;
    }

    /* Fonction appelée par les boutons pour joindre et créer une nouvelle partie. La paramètre reçu déterminera si une
       nouvelle partie est créé (true) ou si on tente de rejoindre une partie en cours (false)
       1. Récupération du nom du joueur (string)
       2. Récupération du nom de la partie à créer ou à rejoindre
       3. Récupération du nombre de joueurs maximum entré. Pour convertir un string en int, on utilise la commande TryParse
       4. Appel de la fonction CreationPartie pour établir la connexion au serveur (dans script gestionnaireReseau)
       5. Désactivation du canvas de départ et activation du canvas de jeu
       */
    public void OnRejoindrePartie(bool nouvellePartie)
    {
        //.1
        nomJoueurLocal = refTxtNomJoueur.text;
        //.2
        if (nouvellePartie)
        {
            nomDeLapartie = refTextNomPartieNouvelle.text;
        }
        else
        {
            nomDeLapartie = refTextNomPartieJoindre.text;
        }
        //.3
        if (refTextNbJoueursNouvelle.text != "")
        {
            int leNombre;
            if (int.TryParse(refTextNbJoueursNouvelle.text, out leNombre))
            {
                GameManager.instance.nombreDeJoueurMax = leNombre;
            }
        }
        //.4
        gestionnaireReseau.CreationPartie(GameMode.AutoHostOrClient);
        //.5
        refCanvasDepart.SetActive(false);
        refCanvasJeu.SetActive(true);
    }
    /* Affichage du pointage des différents joueurs connectés à la partie.
   1. Si la partie est en cours...
   2. Création d'une variable locale de type string "lesPointages"
   3. Boucle qui passera tous les éléments du dictionnaire contenant la référence à chaque joueur et à son pointage.
   On va chercher le nom du joueur ainsi que son pointage et on l'ajoute à la variable locale "lesPointages". À la fin
   la chaine de caractère contientra tous les noms et tous les pointages.
   4. Affichage des noms et des pointages (var lesPointages ) dans la zone de texte située en haut de l'écran.
   */
    void Update()
    {
        if (partieEnCours)
        {
            string lesPointages = "";
            foreach (JoueurReseau joueurReseau in joueursPointagesData.Keys)
            {
                lesPointages += $"{joueurReseau.monNom} : {joueurReseau.nbBoulesRouges}   ";
            }
            refTxtPointage.text = lesPointages;
        }
    }
    /* Fonction appelée par le GestionnaireMouvementPersonnage qui vérifie si la touche "R" a été
enfoncée pour reprendre une nouvelle partie. Cette fonction sera exécuté seulement sur le
serveur.
1. On retire de la liste lstJoueurReseau la référence au joueur qui est prêt à reprendre.
2. Si la liste lstJoueurReseau est rendu vide (== 0), c'est que tous les joueurs sont prêt
a reprendre. Si c'est le cas, on appelle la fonction Recommence présente dans le script
JoueurReseau. Tous les joueurs exécuteront cette fonction.
*/

    /* Fonction permettant d'afficher ou de masquer le texte d'attente d'un autre joueur (bandeau rouge) */
    public void AfficheAttenteAutreJoueur(bool etat)
    {
        txtAttenteAutreJoueur.SetActive(etat);
    }
    /* Fonction appelée lors qu'il est temps d'instancier de nouvelles boules rouges en début de partie
   On appelle simplement une autre fonction CreationBoulleRouge dans le script gestionnaireReseau.
   */
    public void NouvellesBoulesRouges()
    {
        gestionnaireReseau.CreationBoulleRouge();
    }
    /* Fonction qui déclenchera une nouvelle partie si toutes les conditions sont réunis.
1. Désactivation des panneaux de fin de partie
2. On met la variable partieEnCours à true;
3. Variable unSeulJoueur : pour gérer le cas où un seul joueur serait resté connecté.
4. Appel de la fonction Recommence pour chaque JoueurReseau. Si le joueur est seul, cette fonction
renverra true, sinon false;
5. S'il y a plus d'un joueur, on appelle la fonction NouvellesBoulesRouges pour spawner des boules
*/
    public void DebutNouvellePartie()
    {
        //.1
        refPanelAttente.SetActive(false);
        refPanelGagnant.SetActive(false);
        //2.
        partieEnCours = true;
        //3.
        bool unSeulJoueur = false;
        //4.
        foreach (JoueurReseau leJoueur in joueursPointagesData.Keys)
        {
            unSeulJoueur = leJoueur.Recommence();
        }
        if (!unSeulJoueur)
        {
            print("devrait créer des boulettes");
            NouvellesBoulesRouges();
        }

    }

    public void FinPartie(string nomGagnant)
    {
        partieEnCours = false;
        refPanelGagnant.SetActive(true);
        gestionnaireReseau.spheresDejaSpawn = false;

        refTxtGagnant.text = nomGagnant;
    }
    /* Fonction du GameManager qui recoit le nombre de boules rouges à créer. Appelle ensuite
une fonction du même nom dans le GestionnaireRéseau.
*/
    public void AjoutBoulesRouges(int combien)
    {
        gestionnaireReseau.AjoutBoulesRouges(combien);
    }

    /* Fonction appelée par le bouton "clic ici pour commencer" lorsque le joueur entre son nom. Dans ce cas,
   elle redevra "false" comme paramètre.
   Cette fonction est aussi appelée lorsqu'une connexion à une partie est refusée (max de joueurs atteints). Elle recevra
   alors true comme paramètre
   - Activation du panneau du choix de partie (rejoindre ou créer nouvelle)
   - Désactivation du panneau de saisie de nom du joueur
   - Activation du canvas de départ et désactivation du canvas de jeu
   - Dans le cas ou cette fonction est appelé suite à un refus de connexion, on créer un nouvel objet gestionnaire de réseau et on
   mémorise sa référence. On affiche également le pannel qui indique au joueur la raison du refus de connexion.
   */
    public void NavigationPanel(bool nouveauGestionnaireReseau)
    {
        panelChoix.SetActive(true);
        panelNom.SetActive(false);
        refCanvasDepart.SetActive(true);
        refCanvasJeu.SetActive(false);

        if (nouveauGestionnaireReseau)
        {
            GameObject nouveauGestionnaire = Instantiate(gestionnaireReseauSource);
            gestionnaireReseau = nouveauGestionnaire.GetComponent<GestionnaireReseau>();
            panelConnexionRefusee.SetActive(true);
        }
    }




}

