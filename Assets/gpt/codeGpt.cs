using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class CodeGPT : MonoBehaviour
{
    [Header("arena setting")]
    public Vector3 spawnMinArea = new Vector3(-10, 0, -10);
    public Vector3 spawnMaxArea = new Vector3(10, 0, 10);
    public int numberQuads = 100;
    public Transform players;
    public CheckpointManager checkPointManager;

    private List<GameObject> quads = new List<GameObject>();
    private NativeArray<Vector3> quadsPosition;
    private NativeArray<Vector3> velocities;
    private NativeArray<Vector3> targetsPositions;
    private List<Transform> checkpoints;

    private void Start()
    {
        if (players != null && checkPointManager != null)
        {
            SpawnQuads();
            InitializeNativeArray();
        }
        else
        {
            Debug.LogError("Player ou CheckPointManager manquant");
        }
    }

    void SpawnQuads()
    {
        for (int i = 0; i < numberQuads; i++)
        {
            Vector3 randomPosition = new Vector3
                (
                    Random.Range(spawnMinArea.x, spawnMaxArea.x),
                    Random.Range(spawnMinArea.y, spawnMaxArea.y),
                    Random.Range(spawnMinArea.z, spawnMaxArea.z)
                );
            // Créer les objets pour quads
            GameObject quad = new GameObject("Quads_" + i);
            quad.transform.position = randomPosition;

            // Ajouter MeshFilter + MeshRenderer
            MeshFilter meshfilter = quad.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = quad.AddComponent<MeshRenderer>();

            // Créer le quad mesh et l'assigner
            meshfilter.mesh = CreateQuadMesh();

            // Créer et appliquer un material simple
            Material quadMaterial = new Material(Shader.Find("Unlit/Color"));
            quadMaterial.color = GetRandomColor();
            meshRenderer.material = quadMaterial;

            quads.Add(quad);
        }
    }

    Color GetRandomColor()
    {
        return new Color(Random.value, Random.value, Random.value);
    }

    private Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, 0, -0.5f), // Bottom left
            new Vector3(0.5f, 0, -0.5f),  // Bottom right
            new Vector3(-0.5f, 0, 0.5f),  // Top left
            new Vector3(0.5f, 0, 0.5f)    // Top right
        };

        // Définir les triangles du quad
        int[] triangles = new int[6]
        {
            0, 2, 1,  // Premier triangle
            2, 3, 1   // Second triangle
        };

        // Assigner les vertices et triangles au mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // Calculer les normales pour l'éclairage
        mesh.RecalculateNormals();

        return mesh;
    }

    void InitializeNativeArray()
    {
        if (quads.Count == 0)
        {
            Debug.LogError("Les quads ne sont pas initialisés !");
            return;
        }

        quadsPosition = new NativeArray<Vector3>(numberQuads, Allocator.Persistent);
        velocities = new NativeArray<Vector3>(numberQuads, Allocator.Persistent);
        targetsPositions = new NativeArray<Vector3>(numberQuads, Allocator.Persistent);

        checkpoints = checkPointManager.GetCheckpoints().Select(k => k.transform).ToList();

        for (int i = 0; i < numberQuads; i++)
        {
            quadsPosition[i] = quads[i].transform.position;
            velocities[i] = Vector3.zero;
            targetsPositions[i] = FindClosestTarget(quadsPosition[i], i);
        }
    }

    private Vector3 FindClosestTarget(Vector3 quadPosition, int quadIndex)
    {
        float closestDistance = Mathf.Infinity;
        Transform closestCheckpoint = null;

        foreach (Transform checkpoint in checkpoints)
        {
            float distance = Vector3.Distance(quadPosition, checkpoint.position);
            if (distance < closestDistance && IsCheckpointAvailable(checkpoint.position, quadIndex))
            {
                closestDistance = distance;
                closestCheckpoint = checkpoint;
            }
        }

        // Si aucun checkpoint libre, cibler le joueur
        return closestCheckpoint != null ? closestCheckpoint.position : players.position;
    }

    private bool IsCheckpointAvailable(Vector3 checkpointPosition, int quadIndex)
    {
        for (int i = 0; i < numberQuads; i++)
        {
            if (i != quadIndex && targetsPositions[i] == checkpointPosition)
            {
                return false;
            }
        }
        return true;
    }

    private void Update()
    {
        // Vérifier si les NativeArrays sont bien initialisés
        if (!quadsPosition.IsCreated || !velocities.IsCreated || !targetsPositions.IsCreated)
        {
            Debug.LogError("Les NativeArrays ne sont pas initialisés correctement.");
            return;
        }

        MoveQuadsJob moveQuadsJob = new MoveQuadsJob
        {
            quadsPosition = quadsPosition,
            velocities = velocities,
            targetsPosition = targetsPositions,
            deltaTime = Time.deltaTime
        };

        JobHandle jobHandle = moveQuadsJob.Schedule(numberQuads, 64);
        jobHandle.Complete();

        for (int i = 0; i < numberQuads; i++)
        {
            quads[i].transform.position = quadsPosition[i];
        }
    }

    void OnDestroy()
    {
        if (quadsPosition.IsCreated) quadsPosition.Dispose();
        if (velocities.IsCreated) velocities.Dispose();
        if (targetsPositions.IsCreated) targetsPositions.Dispose();
    }

    [BurstCompile]
    struct MoveQuadsJob : IJobParallelFor
    {
        public NativeArray<Vector3> quadsPosition;
        public NativeArray<Vector3> velocities;
        public NativeArray<Vector3> targetsPosition;
        public float deltaTime;

        public void Execute(int index)
        {
            Vector3 direction = (targetsPosition[index] - quadsPosition[index]).normalized;
            velocities[index] = direction * 2f;
            quadsPosition[index] += velocities[index] * deltaTime;
        }
    }
}
