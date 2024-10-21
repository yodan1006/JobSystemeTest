using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public Vector3 initialPosition;

    private void Start()
    {
        initialPosition = transform.position;
    }

    public void Respawn()
    {
        transform.position = initialPosition;
    }
}
