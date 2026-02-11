using System.Collections.Generic;
using UnityEngine;


public class CharacterSwap : MonoBehaviour
{
    List<GameObject> characters = new List<GameObject>();
    [SerializeField] SO_MetaReferences metaReferences;

    private void Awake()
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);

        transform.GetChild(metaReferences.characterID).gameObject.SetActive(true);
    }

}
