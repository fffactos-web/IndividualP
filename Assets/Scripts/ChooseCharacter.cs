using UnityEngine;
using Cinemachine;
using System.Collections.Generic;

public class ChooseCharacter : MonoBehaviour
{
    CinemachineVirtualCamera cinemachine;
    [SerializeField] Transform chooser;

    public int ID;
    List<GameObject> characters = new List<GameObject>();
    [SerializeField]GameObject choosenCharacter;

    [SerializeField] GameObject MainPanel;
    [SerializeField] GameObject ChoosePanel;

    [SerializeField] SO_MetaReferences metaReferences;

    private void Awake()
    {
        cinemachine = GetComponent<CinemachineVirtualCamera>();
        for (int i = 0; i < chooser.childCount; i++)
        {
            characters.Add(chooser.GetChild(i).gameObject);
            chooser.GetChild(i).gameObject.SetActive(false);
        }
        chooser.GetChild(0).gameObject.SetActive(true);
    }

    public void SetChoosenCharacter(int id)
    {
        ID = id;
        metaReferences.characterID = id;
        choosenCharacter.SetActive(false);
        choosenCharacter = characters[id];
        characters[id].SetActive(true);
        characters[id].GetComponent<Animator>().Play("Dance Booty");
    }

    public void SetChooseCamera()
    {
        choosenCharacter.GetComponent<Animator>().Play("Dance Booty");
        MainPanel.SetActive(false);
        ChoosePanel.SetActive(true);
        cinemachine.Priority = 2;
    }
    public void SetDefualtCamera()
    {
        MainPanel.SetActive(true);
        ChoosePanel.SetActive(false);
        cinemachine.Priority = 0;
    }
}
