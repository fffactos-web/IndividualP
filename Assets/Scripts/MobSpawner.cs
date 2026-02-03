using UnityEngine;
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
            Physics.Raycast(ray, out hit, 100000000f);
            PoolManager.I.followerZombiePool.Spawn(
                hit.point + new Vector3(0, 5f, 0),
                Quaternion.identity
            );
        }
    }
}
