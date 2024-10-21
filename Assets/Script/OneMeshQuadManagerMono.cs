using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class OneMeshQuadManagerMono : MonoBehaviour
{
    [Header("Arena Setting")]
    public int numberQuads = 100;
    public GameObject player;
    public CheckpointManager pointManager;
    public Vector3 spawnMinArea = new Vector3(-10, 0, -10);
    public Vector3 spawnMaxArea = new Vector3(10, 0, 10);

    private Mesh mesh;
    private Vector3[] vertices;
    private NativeArray<Vector3> quadsPositionsMono;
    private NativeArray<Vector3> velocitiesMono;
    private NativeArray<Vector3> targetPositionsMono;
    private List<GameObject> checkpointsMono;

    private void Start()
    {
        if (player != null && pointManager != null)
    {
        checkpointsMono = pointManager.GetCheckpoints();

        if (checkpointsMono == null || checkpointsMono.Count == 0)
        {
            Debug.LogError("Checkpoints are missing.");
            return; // Sortir de la méthode si les checkpoints ne sont pas valides
        }

        mesh = CreateMeshWithMultipleQuads(numberQuads);
        InitializeNativeArrays(); // Initialiser les NativeArrays ici
        GetComponent<MeshFilter>().mesh = mesh;
        vertices = mesh.vertices;
    }
    else
    {
        Debug.LogError("Missing player or checkpoint manager reference.");
    }
    }

    Mesh CreateMeshWithMultipleQuads(int quadCount)
    {
        Mesh mesh = new Mesh();
        vertices = new Vector3[quadCount * 4];
        int[] triangles = new int[quadCount * 6];
        Color[] colors = new Color[quadCount * 4];

        for (int i = 0; i < quadCount; i++)
        {
            Vector3 randomPosition = new Vector3
                (
                Random.Range(spawnMinArea.x, spawnMaxArea.x),
                Random.Range(spawnMinArea.y, spawnMaxArea.y),
                Random.Range(spawnMinArea.z, spawnMaxArea.z)
                );
            int vertexIndex = i * 4;
            int triangleIndex = i * 6;

            // Définir les vertices 
            vertices[vertexIndex] = randomPosition + new Vector3(-0.5f, 0, -0.5f);
            vertices[vertexIndex + 1] = randomPosition + new Vector3(0.5f, 0, -0.5f);
            vertices[vertexIndex + 2] = randomPosition + new Vector3(-0.5f, 0, 0.5f);
            vertices[vertexIndex + 3] = randomPosition + new Vector3(0.5f, 0, 0.5f);

            // Définir les triangles
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 2;
            triangles[triangleIndex + 2] = vertexIndex + 1;
            triangles[triangleIndex + 3] = vertexIndex + 2;
            triangles[triangleIndex + 4] = vertexIndex + 3;
            triangles[triangleIndex + 5] = vertexIndex + 1;

            //color
            Color randomColor = new Color(Random.value, Random.value, Random.value);

            colors[vertexIndex] = randomColor;
            colors[vertexIndex + 1] = randomColor;
            colors[vertexIndex + 2] = randomColor;
            colors[vertexIndex + 3] = randomColor;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();

        return mesh;
    }

    void InitializeNativeArrays()
    {
        if (vertices == null || vertices.Length == 0)
        {
            Debug.LogError("Vertices array is not initialized or is empty.");
            return; // Sortir de la méthode si vertices n'est pas valide
        }

        quadsPositionsMono = new NativeArray<Vector3>(numberQuads, Allocator.Persistent);
        velocitiesMono = new NativeArray<Vector3>(numberQuads, Allocator.Persistent);
        targetPositionsMono = new NativeArray<Vector3>(numberQuads, Allocator.Persistent);

        for (int i = 0; i < numberQuads; i++)
        {
            quadsPositionsMono[i] = GetQuadsCenter(i * 4);
            velocitiesMono[i] = Vector3.zero;
            targetPositionsMono[i] = FindClosestTarget(quadsPositionsMono[i], i);
        }
    }

    private Vector3 FindClosestTarget(Vector3 quadPosition, int quadIndex)
    {
        float closestDistace = Mathf.Infinity;
        GameObject closestCheckpoint = null;

        foreach (GameObject checkpoint in checkpointsMono)
        {
            float distance = Vector3.Distance(quadPosition, checkpoint.transform.position);
            if (distance < closestDistace && IsCheckpointAvailable(checkpoint, quadIndex))
            {
                closestDistace = distance;
                closestCheckpoint = checkpoint;
            }
        }
        return closestCheckpoint != null ? closestCheckpoint.transform.position : player.transform.position;
    }

    private bool IsCheckpointAvailable(GameObject checkpoint, int quadIndex)
    {
        for (int i = 0; i < numberQuads; i++)
        {
            if (i != quadIndex && targetPositionsMono[i] == checkpoint.transform.position)
            {
                return false;
            }
        }
            return true;
    }

    private Vector3 GetQuadsCenter(int vertexIndex)
    {
        return (vertices[vertexIndex] + vertices[vertexIndex + 1] + vertices[vertexIndex + 2] + vertices[vertexIndex + 3]) / 4;
    }

    private void Update()
    {
        for (int i = 0; i < numberQuads; i++) 
        { 
            targetPositionsMono[i] = FindClosestTarget(quadsPositionsMono[i], i);
        }

        MoveQuadsJob moveQuadsJob = new MoveQuadsJob
        {
            quadsPosition = quadsPositionsMono,
            velocities = velocitiesMono,
            targetPositions = targetPositionsMono,
            deltaTime = Time.deltaTime
        };

        JobHandle jobHandle = moveQuadsJob.Schedule(numberQuads, 64);
        jobHandle.Complete();

        for (int i = 0;i < numberQuads; i++)
        {
            Vector3 offset = quadsPositionsMono[i] - GetQuadsCenter(i * 4);
            for (int j = 0; j < 4; j++)
            {
                vertices[i * 4 + j] += offset;
            }
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();


    }

    private void OnDestroy()
    {
        if(quadsPositionsMono.IsCreated) quadsPositionsMono.Dispose();
        if (velocitiesMono.IsCreated) velocitiesMono.Dispose();
        if (targetPositionsMono.IsCreated) targetPositionsMono.Dispose();
    }

    [BurstCompile]
    struct MoveQuadsJob : IJobParallelFor
    {
        public NativeArray<Vector3> quadsPosition;
        public NativeArray<Vector3> velocities;
        public NativeArray<Vector3> targetPositions;
        public float deltaTime;

        public void Execute(int index)
        {
            Vector3 direction = (targetPositions[index] - quadsPosition[index]).normalized;
            velocities[index] = direction * 2f;
            quadsPosition[index] += velocities[index] * deltaTime;
        }
    }

}
