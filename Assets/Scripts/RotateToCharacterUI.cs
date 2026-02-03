using UnityEngine;

public class RotateToCharacterUI : MonoBehaviour
{
    RectTransform rect;
    Transform player;

    [SerializeField]
    float chchchc;

    [SerializeField]
    bool modifyScale;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }
    private void Update()
    {
        rect.LookAt(Camera.main.transform.position);

        if (modifyScale)
        {
            Vector3 scale = new Vector3((Camera.main.transform.position - rect.position).magnitude, (Camera.main.transform.position - rect.position).magnitude, (Camera.main.transform.position - rect.position).magnitude);
            rect.localScale = scale / chchchc;
        }
    }
}
