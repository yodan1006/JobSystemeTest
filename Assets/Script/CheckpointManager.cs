using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public int numberCheckpoint = 10; // Nombre de checkpoints � g�n�rer
    public GameObject checkpointPrefab; // Prefab de la sph�re qui servira de checkpoint
    public Vector3 checkpointMinArea = new Vector3(-10, 0, -10); // Limite inf�rieure de la zone de spawn des checkpoints
    public Vector3 checkpointMaxArea = new Vector3(10, 1, 10); // Limite sup�rieure de la zone de spawn des checkpoints

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

    // G�n�re les checkpoints � des positions al�atoires et instancie les sph�res
    void GenerateCheckpoints()
    {
        for (int i = 0; i < numberCheckpoint; i++)
        {
            // G�n�ration d'une position al�atoire dans la zone d�finie
            Vector3 randomCheckpointPosition = new Vector3(
                Random.Range(checkpointMinArea.x, checkpointMaxArea.x),
                Random.Range(checkpointMinArea.y, checkpointMaxArea.y),
                Random.Range(checkpointMinArea.z, checkpointMaxArea.z)
            );

            // Instanciation du prefab de checkpoint (la sph�re) � cette position
            GameObject checkpoint = Instantiate(checkpointPrefab, randomCheckpointPosition, Quaternion.identity);

            // Ajoute le checkpoint � la liste
            checkpoints.Add(checkpoint);
        }
    }

    // Retourne la liste des checkpoints (les GameObjects)
    public List<GameObject> GetCheckpoints()
    {
        return checkpoints;
    }
}
