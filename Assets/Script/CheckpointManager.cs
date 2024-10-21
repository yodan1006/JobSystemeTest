using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public int numberCheckpoint = 10; // Nombre de checkpoints à générer
    public GameObject checkpointPrefab; // Prefab de la sphère qui servira de checkpoint
    public Vector3 checkpointMinArea = new Vector3(-10, 0, -10); // Limite inférieure de la zone de spawn des checkpoints
    public Vector3 checkpointMaxArea = new Vector3(10, 1, 10); // Limite supérieure de la zone de spawn des checkpoints

    private List<GameObject> checkpoints = new List<GameObject>(); // Liste pour stocker les objets GameObject des checkpoints

    private void Start()
    {
        if (checkpointPrefab == null)
        {
            Debug.LogError("Checkpoint Prefab is missing! Please assign it in the inspector.");
            return;
        }

        GenerateCheckpoints();
    }

    // Génère les checkpoints à des positions aléatoires et instancie les sphères
    void GenerateCheckpoints()
    {
        for (int i = 0; i < numberCheckpoint; i++)
        {
            // Génération d'une position aléatoire dans la zone définie
            Vector3 randomCheckpointPosition = new Vector3(
                Random.Range(checkpointMinArea.x, checkpointMaxArea.x),
                Random.Range(checkpointMinArea.y, checkpointMaxArea.y),
                Random.Range(checkpointMinArea.z, checkpointMaxArea.z)
            );

            // Instanciation du prefab de checkpoint (la sphère) à cette position
            GameObject checkpoint = Instantiate(checkpointPrefab, randomCheckpointPosition, Quaternion.identity);

            // Ajoute le checkpoint à la liste
            checkpoints.Add(checkpoint);
        }
    }

    // Retourne la liste des checkpoints (les GameObjects)
    public List<GameObject> GetCheckpoints()
    {
        return checkpoints;
    }
}
