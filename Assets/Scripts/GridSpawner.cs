using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    [SerializeField] GameObject utilitySpawnerPrefab;

    [Header("Grid Settings")]
    [SerializeField] int gridX = 10;
    [SerializeField] int gridY = 10;
    [SerializeField] float step = 10f;

    [Header("Grid Settings")]
    [SerializeField] GameObject player;
    [SerializeField] LayerMask ground;
    [SerializeField] Transform spawnPoint;

    void Start()
    {
        for (int x = 0; x < gridX; x++)
        {
            for (int y = 0; y < gridY; y++)
            {
                Vector3 pos = new Vector3(x * step, transform.position.y, y * step);

                Instantiate(utilitySpawnerPrefab, pos, Quaternion.identity);
            }
        }

        TransformPlayer();

    }

    public void TransformPlayer()
    {
        RaycastHit hit;
        Physics.Raycast(spawnPoint.position, -transform.up, out hit, 10000000f, ground, QueryTriggerInteraction.Ignore);
        player.transform.position = hit.point + new Vector3(0, 200f, 0);
    }
}
