using UnityEngine;
using System.Collections.Generic;

public class UtilitySpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnOption
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float chance;
    }

    [Header("Random Offset")]
    [SerializeField] float randomRadius = 5f;

    [Header("Raycast")]
    [SerializeField] float rayHeight = 100f;
    [SerializeField] LayerMask groundMask;

    [Header("Spawn Options")]
    [SerializeField] List<SpawnOption> spawnOptions;

    void Start()
    {
        MoveRandomly();
        TrySpawn();
        Destroy(gameObject); // удаляем себя после работы
    }

    void MoveRandomly()
    {
        Vector2 randomCircle = Random.insideUnitCircle * randomRadius;

        transform.position += new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    void TrySpawn()
    {
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, rayHeight * 200000f, groundMask))
        {
            GameObject prefab = ChoosePrefab();

            if (prefab != null)
            {
                Quaternion prefabRot = Quaternion.LookRotation(transform.forward, hit.normal);
                Instantiate(prefab, hit.point, prefabRot);
            }
        }
    }

    GameObject ChoosePrefab()
    {
        float total = 0f;

        foreach (var option in spawnOptions)
            total += option.chance;

        float randomValue = Random.value * total;

        float cumulative = 0f;

        foreach (var option in spawnOptions)
        {
            cumulative += option.chance;

            if (randomValue <= cumulative)
                return option.prefab;
        }

        return null;
    }
}
