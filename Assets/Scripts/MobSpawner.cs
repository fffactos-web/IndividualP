using UnityEngine;
using UnityEngine.AI;
<<<<<<< Updated upstream
=======
using UnityEngine.UIElements;
>>>>>>> Stashed changes

public class MobSpawner : MonoBehaviour
{
    [Header("Spawn Placement")]
    [SerializeField] float spawnRadius = 2.5f;
    [SerializeField] int maxAttemptsPerMob = 8;
    [SerializeField] float raycastHeight = 50f;
    [SerializeField] float raycastDistance = 200f;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float navMeshSampleDistance = 25f;

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
<<<<<<< Updated upstream
            if (!TryGetSpawnPoint(out Vector3 spawnPoint))
                continue;

            GameObject spawned = PoolManager.I.followerZombiePool.Spawn(
                spawnPoint,
=======
            RaycastHit hit;
            Ray ray = new Ray(transform.position, -transform.up);
            Vector3 spawnPoint = transform.position;
            if (Physics.Raycast(ray, out hit, 100000000f))
            {
                spawnPoint = hit.point;
            }

            if (NavMesh.SamplePosition(spawnPoint, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
            {
                spawnPoint = navHit.position;
            }

            PoolManager.I.followerZombiePool.Spawn(
                spawnPoint + new Vector3(0, 0.1f, 0),
>>>>>>> Stashed changes
                Quaternion.identity
            );

            if (spawned.TryGetComponent(out NavMeshAgent agent))
            {
                SnapAgentToNavMesh(agent, spawnPoint);
            }
            else
            {
                agent = spawned.GetComponentInChildren<NavMeshAgent>();
                if (agent != null)
                {
                    SnapAgentToNavMesh(agent, spawnPoint);
                }
            }
        }
    }
<<<<<<< Updated upstream

    static void SnapAgentToNavMesh(NavMeshAgent agent, Vector3 pointOnNavMesh)
    {
        if (agent == null)
            return;

        agent.enabled = true;

        // Warp can silently fail when the agent internal state is stale after pooling.
        // In that case, toggling enabled after setting transform usually recovers it.
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
        // We try multiple times to ensure we land on the NavMesh even if spawner is above ground,
        // rotated, or slightly outside the baked navmesh.
        for (int attempt = 0; attempt < Mathf.Max(1, maxAttemptsPerMob); attempt++)
        {
            Vector2 offset2D = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = transform.position + new Vector3(offset2D.x, 0f, offset2D.y);

            // Always raycast using world down, not transform.up (spawner might be rotated).
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
=======
}
>>>>>>> Stashed changes
