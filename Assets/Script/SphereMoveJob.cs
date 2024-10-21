using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct SphereMovementJob : IJobParallelFor
{
    public NativeArray<Vector3> position;
    public NativeArray<Vector3> velocity;
    public NativeArray<Vector3> targets;
    public float deltaTime;
    public float followSpeed;

    public void Execute(int index)
    {
        Vector3 directionTarget = (targets[index] - position[index]).normalized;
        position[index] = directionTarget * followSpeed * deltaTime ;
    }
}