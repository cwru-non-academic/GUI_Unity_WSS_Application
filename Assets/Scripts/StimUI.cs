using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StimUI : MonoBehaviour
{
    [SerializeField] private Stimulation stim;
    [SerializeField] private TMP_Text ready;
    [SerializeField] private GameObject stimOn;
    [SerializeField] private GameObject stimOff;

    private string currentMode = string.Empty;

    // Start is called before the first frame update
    void Start()
    {
        stimOff.SetActive(true);
        stimOn.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

        if (stim.Ready() || stim.Started())
        {
            if (currentMode != stim.GetModelConfigCTRL().GetCalibMode())
            {
                verifyNewMode();
            } else
            {
                ready.text = "Ready " + currentMode;
            }
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

    private void verifyNewMode()
    {
        currentMode = stim.GetModelConfigCTRL().GetCalibMode();
        if (stim.isModeValid())
        {
            ready.text = "Ready " + currentMode;
        }
        else
        {
            ready.text = "StimMode not valid";
        }
    }
}
