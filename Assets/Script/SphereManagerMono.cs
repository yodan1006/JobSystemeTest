using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class SphereManager : MonoBehaviour
{
    public GameObject spherePrefab;
    public Vector3 spawnMinArea = new Vector3(-1, 0, -1);
    public Vector3 spawnMaxArea = new Vector3(1, 1, 1);
    public int numberSphere = 100;
    public GameObject player;
    public CheckpointManager checkpointManager;

    [Header("Debug")]
    public List<GameObject> spheres = new List<GameObject>();
    public List<GameObject> checkpoints;

    NativeArray<Vector3> position;
    NativeArray<Vector3> velocity;
    NativeArray<Vector3> targetPositions;

    public int m_sizeNative;
    void Start()
    {
        if (spherePrefab != null && player != null && checkpointManager != null)
        {
            SpawnSphere();
            InitializeNativeArrays();
        }
        else
        {
            Debug.LogError("Prefab de la sphere, player, ou CheckpointManager manquant!");
        }
    }

    void SpawnSphere()
    {
        for (int i = 0; i < numberSphere; i++)
        {
            Vector3 randomPosition = new Vector3(
                Random.Range(spawnMinArea.x, spawnMaxArea.x),
                Random.Range(spawnMinArea.y, spawnMaxArea.y),
                Random.Range(spawnMinArea.z, spawnMaxArea.z)
            );

            GameObject sphere = Instantiate(spherePrefab, randomPosition, Quaternion.identity);
            spheres.Add(sphere);
        }
    }

    void InitializeNativeArrays()
    {
        position = new NativeArray<Vector3>(numberSphere, Allocator.Persistent);
        velocity = new NativeArray<Vector3>(numberSphere, Allocator.Persistent);
        targetPositions = new NativeArray<Vector3>(numberSphere, Allocator.Persistent);

        checkpoints = checkpointManager.GetCheckpoints();

        for (int i = 0; i < numberSphere; i++)
        {
            position[i] = spheres[i].transform.position;
            velocity[i] = Vector3.zero;
            targetPositions[i] = FindClosestTarget(position[i], i);
        }

        m_sizeNative = position.Length;
    }

    public List<Vector3> m_points;
    public List<Vector3> m_check;

    Vector3 FindClosestTarget(Vector3 spherePosition, int sphereIndex)
    {
        float closestDistance = Mathf.Infinity;
        GameObject closestCheckpoint = null;

        foreach (GameObject checkpoint in checkpoints)
        {
            float distance = Vector3.Distance(spherePosition, checkpoint.transform.position);
            if (distance < closestDistance && IsCheckpointAvailable(checkpoint, sphereIndex))
            {
                closestDistance = distance;
                closestCheckpoint = checkpoint;
            }
        }

        if (closestCheckpoint != null)
        {
            return closestCheckpoint.transform.position;
        }
        else
        {
            return player.transform.position;
        }
    }

    bool IsCheckpointAvailable(GameObject checkpoint, int sphereIndex)
    {
        for (int i = 0; i < numberSphere; i++)
        {
            if (i != sphereIndex && targetPositions[i] == checkpoint.transform.position)
            {
                return false;
            }
        }
        return true;
    }

    void Update()
    {
        // Met à jour les cibles des sphères pour chaque frame
        for (int i = 0; i < numberSphere; i++)
        {
            targetPositions[i] = FindClosestTarget(position[i], i);
        }

        MoveSpheresJob moveJob = new MoveSpheresJob
        {
            position = position,
            velocity = velocity,
            targetPositions = targetPositions,
            deltaTime = Time.deltaTime
        };

        JobHandle jobHandle = moveJob.Schedule(numberSphere, 64);
        jobHandle.Complete();

        for (int i = 0; i < numberSphere; i++)
        {
            spheres[i].transform.position = position[i];
        }
        m_points = position.ToList();
        m_check = targetPositions.ToList();
    }

    private void OnDestroy()
    {
        if (position.IsCreated) position.Dispose();
        if (velocity.IsCreated) velocity.Dispose();
        if (targetPositions.IsCreated) targetPositions.Dispose();
    }

    struct MoveSpheresJob : IJobParallelFor
    {
        public NativeArray<Vector3> position;
        public NativeArray<Vector3> velocity;
        public NativeArray<Vector3> targetPositions;
        public float deltaTime;

        public void Execute(int index)
        {
            Vector3 direction = (targetPositions[index] - position[index]).normalized;

            if (direction != Vector3.zero)
            {
                velocity[index] = direction * 2f; // Vitesse constante vers la cible
                position[index] += velocity[index] * deltaTime;
            }
        }
    }
}
