using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DetectionRejouer : MonoBehaviour
{
    public GameObject panelGagnant;
    public GameObject panelAttente;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            panelAttente.SetActive(true);
            panelGagnant.SetActive(false);
        }
    }
}
