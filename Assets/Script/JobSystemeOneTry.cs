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
    // R�f�rence au prefab de la sph�re que l'on va instancier
    public GameObject spherePrefab;
    public Transform player;

    // Limites de la zone dans laquelle les sph�res seront g�n�r�es al�atoirement
    public Vector3 spawnMinArea = new Vector3(-1, 0, -1); // Limite minimale de la zone de spawn
    public Vector3 spawnMaxArea = new Vector3(1, 1, 1);   // Limite maximale de la zone de spawn
    public Vector3 respawnMinArea = new Vector3(-10, 0, -10); // Limite minimale du respawn
    public Vector3 respawnMaxArea = new Vector3(10, 1, 10); // Limite max du respawn

    // Nombre total de sph�res � g�n�rer
    public int numberSphere = 100;
    //vitesse de follow sur joueur
    public float followSpeed = 2f;
    //distance si la sphere a bien toucher le joueur
    public float resetDistance = 0.5f;

    // Liste pour stocker les sph�res cr��es afin de pouvoir les manipuler par la suite
    private List<GameObject> spheres = new List<GameObject>();
    //stock la position joueur
    private Vector3 playerInitialPosition;

    // NativeArray pour stocker les positions et les v�locit�s (directions de d�placement) des sph�res
    NativeArray<Vector3> position; // Positions actuelles des sph�res
    NativeArray<Vector3> velocity; // V�locit�s des sph�res (la direction et vitesse de mouvement)

    // Fonction pour g�n�rer les sph�res et les positionner al�atoirement dans la zone d�finie
    void SpawnSphere()
    {
        for (int i = 0; i < numberSphere; i++)
        {
            // G�n�ration d'une position al�atoire dans les limites de la zone de spawn
            Vector3 randomPosition = new Vector3(
                Random.Range(spawnMinArea.x, spawnMaxArea.x), // X al�atoire
                Random.Range(spawnMinArea.y, spawnMaxArea.y), // Y al�atoire
                Random.Range(spawnMinArea.z, spawnMaxArea.z)  // Z al�atoire
            );

            // Instanciation d'une sph�re � cette position avec une rotation par d�faut (Quaternion.identity)
            GameObject sphere = Instantiate(spherePrefab, randomPosition, Quaternion.identity);
            // Ajout de la sph�re � la liste pour pouvoir y acc�der plus tard
            spheres.Add(sphere);
        }
    }

    // Fonction pour initialiser les NativeArray (positions et v�locit�s)
    void InitializeNativeArrays()
    {
        // Initialisation du tableau des positions et des v�locit�s, allou�s en m�moire persistante
        position = new NativeArray<Vector3>(numberSphere, Allocator.Persistent);
        velocity = new NativeArray<Vector3>(numberSphere, Allocator.Persistent);

        // On parcourt chaque sph�re pour initialiser ses donn�es de position et v�locit�
        for (int i = 0; i < numberSphere; i++)
        {
            position[i] = spheres[i].transform.position; // On enregistre la position initiale de la sph�re
            // On donne une v�locit� al�atoire (direction) pour chaque sph�re (ici sur X et Z, Y reste � 0)
            velocity[i] = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        }
    }

    // Fonction appel�e au d�marrage du script
    void Start()
    {
        // On v�rifie que le prefab de la sph�re est bien assign�
        if (spherePrefab != null)
        {
            // Si c'est le cas, on g�n�re les sph�res et on initialise les tableaux
            SpawnSphere();
            InitializeNativeArrays();

            playerInitialPosition = player.position;
        }
        else
        {
            // Si le prefab n'est pas assign�, on affiche un message d'erreur dans la console
            Debug.LogError("Prefab de la sphere manquant!");
        }
    }

    // Fonction appel�e � chaque frame (c'est ici que l'on va d�placer les sph�res)
    private void Update()
    {
        // On cr�e une instance de notre job qui va calculer le mouvement des sph�res
        MoveSpheresJob moveJob = new MoveSpheresJob
        {
            // On passe les positions, les v�locit�s et le deltaTime (temps �coul� entre deux frames) au job
            position = position,
            velocity = velocity,
            deltaTime = Time.deltaTime,
            playerPosition = player.position,
            followSpeed = followSpeed
        };

        // On programme l'ex�cution du job pour qu'il soit ex�cut� sur plusieurs threads (en parall�le)
        JobHandle jobHandle = moveJob.Schedule(numberSphere, 32); // 64 est la taille des batchs de travail
        // On attend que le job soit termin� avant de continuer
        jobHandle.Complete();

        // Une fois le job termin�, on applique les nouvelles positions aux sph�res dans la sc�ne Unity
        for (int i = 0; i < numberSphere; i++)
        {
            // On met � jour la position de chaque sph�re dans la sc�ne Unity
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

    // Fonction appel�e lors de la destruction de l'objet (par exemple quand la sc�ne est ferm�e)
    private void OnDestroy()
    {
        // On lib�re la m�moire allou�e pour les NativeArray, si elles sont cr��es
        if (position.IsCreated) position.Dispose();
        if (velocity.IsCreated) velocity.Dispose();
    }

    // D�finition du Job pour d�placer les sph�res
    // Ce job est ex�cut� sur plusieurs threads pour am�liorer la performance
    [BurstCompile]
    struct MoveSpheresJob : IJobParallelFor
    {
        // NativeArray pour stocker et manipuler les positions et v�locit�s
        public NativeArray<Vector3> position;
        public NativeArray<Vector3> velocity;

        // Temps �coul� depuis la derni�re frame, utilis� pour ajuster la vitesse de d�placement
        public float deltaTime;
        public float followSpeed;
        //position du joueur a suivre
        public Vector3 playerPosition;

        // Fonction qui sera ex�cut�e pour chaque sph�re
        public void Execute(int index)
        {
            Vector3 directionToPlayer = (playerPosition -  position[index]).normalized;
            // On met � jour la position en fonction de la v�locit� (direction * vitesse * deltaTime)
            position[index] += directionToPlayer * followSpeed * deltaTime;
        }
    }
}
