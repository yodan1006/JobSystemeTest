using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public class JobSystemeOneTry : MonoBehaviour
{
    // Référence au prefab de la sphère que l'on va instancier
    public GameObject spherePrefab;
    public Transform player;

    // Limites de la zone dans laquelle les sphères seront générées aléatoirement
    public Vector3 spawnMinArea = new Vector3(-1, 0, -1); // Limite minimale de la zone de spawn
    public Vector3 spawnMaxArea = new Vector3(1, 1, 1);   // Limite maximale de la zone de spawn
    public Vector3 respawnMinArea = new Vector3(-10, 0, -10); // Limite minimale du respawn
    public Vector3 respawnMaxArea = new Vector3(10, 1, 10); // Limite max du respawn

    // Nombre total de sphères à générer
    public int numberSphere = 100;
    //vitesse de follow sur joueur
    public float followSpeed = 2f;
    //distance si la sphere a bien toucher le joueur
    public float resetDistance = 0.5f;

    // Liste pour stocker les sphères créées afin de pouvoir les manipuler par la suite
    private List<GameObject> spheres = new List<GameObject>();
    //stock la position joueur
    private Vector3 playerInitialPosition;

    // NativeArray pour stocker les positions et les vélocités (directions de déplacement) des sphères
    NativeArray<Vector3> position; // Positions actuelles des sphères
    NativeArray<Vector3> velocity; // Vélocités des sphères (la direction et vitesse de mouvement)

    // Fonction pour générer les sphères et les positionner aléatoirement dans la zone définie
    void SpawnSphere()
    {
        for (int i = 0; i < numberSphere; i++)
        {
            // Génération d'une position aléatoire dans les limites de la zone de spawn
            Vector3 randomPosition = new Vector3(
                Random.Range(spawnMinArea.x, spawnMaxArea.x), // X aléatoire
                Random.Range(spawnMinArea.y, spawnMaxArea.y), // Y aléatoire
                Random.Range(spawnMinArea.z, spawnMaxArea.z)  // Z aléatoire
            );

            // Instanciation d'une sphère à cette position avec une rotation par défaut (Quaternion.identity)
            GameObject sphere = Instantiate(spherePrefab, randomPosition, Quaternion.identity);
            // Ajout de la sphère à la liste pour pouvoir y accéder plus tard
            spheres.Add(sphere);
        }
    }

    // Fonction pour initialiser les NativeArray (positions et vélocités)
    void InitializeNativeArrays()
    {
        // Initialisation du tableau des positions et des vélocités, alloués en mémoire persistante
        position = new NativeArray<Vector3>(numberSphere, Allocator.Persistent);
        velocity = new NativeArray<Vector3>(numberSphere, Allocator.Persistent);

        // On parcourt chaque sphère pour initialiser ses données de position et vélocité
        for (int i = 0; i < numberSphere; i++)
        {
            position[i] = spheres[i].transform.position; // On enregistre la position initiale de la sphère
            // On donne une vélocité aléatoire (direction) pour chaque sphère (ici sur X et Z, Y reste à 0)
            velocity[i] = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        }
    }

    // Fonction appelée au démarrage du script
    void Start()
    {
        // On vérifie que le prefab de la sphère est bien assigné
        if (spherePrefab != null)
        {
            // Si c'est le cas, on génère les sphères et on initialise les tableaux
            SpawnSphere();
            InitializeNativeArrays();

            playerInitialPosition = player.position;
        }
        else
        {
            // Si le prefab n'est pas assigné, on affiche un message d'erreur dans la console
            Debug.LogError("Prefab de la sphere manquant!");
        }
    }

    // Fonction appelée à chaque frame (c'est ici que l'on va déplacer les sphères)
    private void Update()
    {
        // On crée une instance de notre job qui va calculer le mouvement des sphères
        MoveSpheresJob moveJob = new MoveSpheresJob
        {
            // On passe les positions, les vélocités et le deltaTime (temps écoulé entre deux frames) au job
            position = position,
            velocity = velocity,
            deltaTime = Time.deltaTime,
            playerPosition = player.position,
            followSpeed = followSpeed
        };

        // On programme l'exécution du job pour qu'il soit exécuté sur plusieurs threads (en parallèle)
        JobHandle jobHandle = moveJob.Schedule(numberSphere, 32); // 64 est la taille des batchs de travail
        // On attend que le job soit terminé avant de continuer
        jobHandle.Complete();

        // Une fois le job terminé, on applique les nouvelles positions aux sphères dans la scène Unity
        for (int i = 0; i < numberSphere; i++)
        {
            // On met à jour la position de chaque sphère dans la scène Unity
            spheres[i].transform.position = position[i];

            if (Vector3.Distance(position[i], player.position) <= resetDistance)
            {
                Vector3 randomRespawnPosition = new Vector3(
                    Random.Range(respawnMinArea.x, respawnMaxArea.x),
                    Random.Range(respawnMinArea.y, respawnMaxArea.y),
                    Random.Range(respawnMinArea.z, respawnMaxArea.z));
                player.position = randomRespawnPosition;
            }
        }
    }

    // Fonction appelée lors de la destruction de l'objet (par exemple quand la scène est fermée)
    private void OnDestroy()
    {
        // On libère la mémoire allouée pour les NativeArray, si elles sont créées
        if (position.IsCreated) position.Dispose();
        if (velocity.IsCreated) velocity.Dispose();
    }

    // Définition du Job pour déplacer les sphères
    // Ce job est exécuté sur plusieurs threads pour améliorer la performance
    [BurstCompile]
    struct MoveSpheresJob : IJobParallelFor
    {
        // NativeArray pour stocker et manipuler les positions et vélocités
        public NativeArray<Vector3> position;
        public NativeArray<Vector3> velocity;

        // Temps écoulé depuis la dernière frame, utilisé pour ajuster la vitesse de déplacement
        public float deltaTime;
        public float followSpeed;
        //position du joueur a suivre
        public Vector3 playerPosition;

        // Fonction qui sera exécutée pour chaque sphère
        public void Execute(int index)
        {
            Vector3 directionToPlayer = (playerPosition -  position[index]).normalized;
            // On met à jour la position en fonction de la vélocité (direction * vitesse * deltaTime)
            position[index] += directionToPlayer * followSpeed * deltaTime;
        }
    }
}
