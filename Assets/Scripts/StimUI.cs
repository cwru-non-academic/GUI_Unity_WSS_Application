using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StimUI : MonoBehaviour
{
    [SerializeField] private Stimulation stim;
    [SerializeField] private StimConfigController stimCTRL;
    [SerializeField] private TMP_Text ready;
    [SerializeField] private GameObject stimOn;
    [SerializeField] private GameObject stimOff;

    // Start is called before the first frame update
    void Start()
    {
        stimOff.SetActive(true);
        stimOn.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (stim.Ready())
        {
            ready.text = "Ready "+ stimCTRL._config.sensationController;
        }
        else
        {
            ready.text = "Setting Up...";
        }
    }

    public void StimActive()
    {
        stimOn.SetActive(true);
        stimOff.SetActive(false);
    }

    public void StimNotActive()
    {
        stimOn.SetActive(false);
        stimOff.SetActive(true);
    }
}
