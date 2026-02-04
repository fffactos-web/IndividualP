using UnityEngine;
using UnityEngine.AI;

public class MobSpawner : MonoBehaviour
{
    [Header("Spawn Placement")]
    [SerializeField] float spawnRadius = 2.5f;
    [SerializeField] int maxAttemptsPerMob = 8;
    [SerializeField] float raycastHeight = 50f;
    [SerializeField] float raycastDistance = 200f;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float navMeshSampleDistance = 25f;
    [SerializeField] float verticalOffset = 0.1f;

    public void SpawnWave(int difficulty, bool bigWave)
    {
        int baseMin = 3 + difficulty;
        int baseMax = 5 + difficulty * 2;

        if (bigWave)
        {
            baseMin *= 2;
            baseMax *= 2;
        }

        int count = Random.Range(baseMin, baseMax);

        for (int i = 0; i < count; i++)
        {
            if (!TryGetSpawnPoint(out Vector3 spawnPoint))
                continue;

            GameObject spawned = PoolManager.I.followerZombiePool.Spawn(
                spawnPoint + Vector3.up * verticalOffset,
                Quaternion.identity
            );

            NavMeshAgent agent =
                spawned.GetComponent<NavMeshAgent>() ??
                spawned.GetComponentInChildren<NavMeshAgent>();

            if (agent != null)
            {
                SnapAgentToNavMesh(agent, spawnPoint);
            }
        }
    }

    static void SnapAgentToNavMesh(NavMeshAgent agent, Vector3 pointOnNavMesh)
    {
        if (agent == null)
            return;

        agent.enabled = true;

        // Warp can fail after pooling — this recovers the agent state.
        if (!agent.Warp(pointOnNavMesh))
        {
            agent.enabled = false;
            agent.transform.position = pointOnNavMesh;
            agent.enabled = true;
            agent.Warp(pointOnNavMesh);
        }

        agent.isStopped = false;
        agent.ResetPath();
    }

    bool TryGetSpawnPoint(out Vector3 spawnPoint)
    {
        for (int attempt = 0; attempt < Mathf.Max(1, maxAttemptsPerMob); attempt++)
        {
            Vector2 offset2D = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = transform.position + new Vector3(offset2D.x, 0f, offset2D.y);

            Vector3 rayOrigin = candidate + Vector3.up * raycastHeight;

            if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                raycastDistance,
                groundMask,
                QueryTriggerInteraction.Ignore
            ))
            {
                candidate = hit.point;
            }

            if (NavMesh.SamplePosition(candidate, out NavMeshHit navHit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                spawnPoint = navHit.position;
                return true;
            }
        }

        spawnPoint = default;
        return false;
    }
}
