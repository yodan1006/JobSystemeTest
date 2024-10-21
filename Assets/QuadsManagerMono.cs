using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class QuadsManagerMono : MonoBehaviour
{
    [Header("arena setting")]
    public Vector3 spawnMinArea = new Vector3(-10, 0, -10);
    public Vector3 spawnMaxArea = new Vector3(10, 0, 10);
    public int numberQuads = 100;
    public GameObject players;
    public CheckpointManager checkPointManager;

    private List<GameObject> quads = new List<GameObject>();
    private List<GameObject> checkpoints;

    private NativeArray<Vector3> quadsPosition;
    private NativeArray<Vector3> velocities;
    private NativeArray<Vector3> targetsPositions;
    private void Start()
    {
        if (players != null && checkPointManager != null)
        {
            SpawnQuads();
            InitializeNativeArray();
        }
        else
        {
            Debug.LogError("oublie du player ou du checkPointManager");
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
            //creer les objet pour quads
            GameObject quad = new GameObject("Quads_" + i);
            quad.transform.position = randomPosition;

            //filter + renderer
            MeshFilter meshfilter = quad.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = quad.AddComponent<MeshRenderer>();

            //creer le quads mesh et l'assigner
            meshfilter.mesh = CreateQuadMesh();

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

        //definition des triangles du quad
        int[] triangles = new int[6]
            {
                0,2,1,  //premier triangle
                2,3,1   //second triangle
            };

        //assignes les vertices et triangles du mesh 
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        //calcule les normales pour la l'eclairageµ

        mesh.RecalculateNormals();
        
        return mesh;
    }

    void InitializeNativeArray()
    {

        quadsPosition = new NativeArray<Vector3>(numberQuads, Allocator.Persistent);
        velocities = new NativeArray<Vector3>(numberQuads, Allocator.Persistent);
        targetsPositions = new NativeArray<Vector3>(numberQuads, Allocator.Persistent);

        checkpoints = checkPointManager.GetCheckpoints();

        for (int i = 0; i < numberQuads; i++) {
            quadsPosition[i] = quads[i].transform.position;
            velocities[i] = Vector3.zero;
            targetsPositions[i] = FindClosestTarget(quadsPosition[i], i);

    }
}

    private Vector3 FindClosestTarget(Vector3 quadPosition, int quadIndex)
    {
        float closestDistance = Mathf.Infinity;
        GameObject closestCheckpoint = null;

        foreach (GameObject checkpoint in checkpoints)
        {
            float distance = Vector3.Distance(quadPosition, checkpoint.transform.position);
            if (distance < closestDistance && IsCheckpointAvailable(checkpoint, quadIndex))
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
            return players.transform.position;
        }
    }
    private bool IsCheckpointAvailable(GameObject checkpointPosition, int quadIndex)
    {
        for (int i = 0; i < numberQuads; i++)
        {
            if (i != quadIndex && targetsPositions[i] == checkpointPosition.transform.position) { return false; }
        }
        return true;
    }

    private void Update()
    {
        for (int i = 0; i < numberQuads; i++)
        {
            targetsPositions[i] = FindClosestTarget(quadsPosition[i], i);
        }

        MoveQuadsJob moveQuadsJob = new MoveQuadsJob
        {
            quadsPosition = quadsPosition,
            velocities = velocities,
            targetsPosition = targetsPositions,
            deltaTime = Time.deltaTime
        };

        JobHandle jobHandlen = moveQuadsJob.Schedule(numberQuads, 64);
        jobHandlen.Complete();

        for (int i = 0; i < numberQuads; i++)
        {
            quads[i].transform.position = quadsPosition[i];
        }

    }

    void OnDestroy() 
    {
        {
            if (quadsPosition.IsCreated) quadsPosition.Dispose();
            if (velocities.IsCreated) velocities.Dispose();
            if (targetsPositions.IsCreated) targetsPositions.Dispose();
        }
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