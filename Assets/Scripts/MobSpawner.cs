using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class MobSpawner : MonoBehaviour
{
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
            RaycastHit hit;
            Ray ray = new Ray(transform.position, -transform.up);
            Vector3 spawnPoint = transform.position;
            if (Physics.Raycast(ray, out hit, 100000000f))
            {
                spawnPoint = hit.point;
            }

            if (NavMesh.SamplePosition(spawnPoint, out NavMeshHit navHit, 10f, NavMesh.AllAreas))
            {
                spawnPoint = navHit.position;
            }

            GameObject spawned = PoolManager.I.followerZombiePool.Spawn(
                spawnPoint,
                Quaternion.identity
            );

            if (spawned.TryGetComponent(out NavMeshAgent agent))
            {
                agent.Warp(spawnPoint + Vector3.up * agent.baseOffset);
                agent.isStopped = false;
                agent.ResetPath();
            }
            else if (spawned.TryGetComponentInChildren(out agent))
            {
                agent.Warp(spawnPoint + Vector3.up * agent.baseOffset);
                agent.isStopped = false;
                agent.ResetPath();
            }
        }
    }
}
