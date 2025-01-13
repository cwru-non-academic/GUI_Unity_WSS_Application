using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.MUIP;
using TMPro;

public class MinMax : MonoBehaviour
{
    [SerializeField] private Stimulation stim;
    [SerializeField] private ProgressBar pwMinBar;
    [SerializeField] private ProgressBar AmpBar;
    [SerializeField] private ProgressBar pwMaxBar;
    [SerializeField] private CustomDropdown input;
    [SerializeField] private ButtonManager linkBTN;
    [SerializeField] private GameObject MaxSelect;
    [SerializeField] private GameObject MinSelect;
    [SerializeField] private GameObject stimOn;
    [SerializeField] private GameObject stimOff;
    [SerializeField] private GameObject crabSerial;
    [SerializeField] private TMP_Text ready;

    private int amp = 2;//in mA
    private int pwMin = 0; //in us
    private int pwMax = 0; //in us
    private int deltaPW = 1;
    private int deltaAmp = 1;
    private bool upOn = false;
    private bool downOn = false;
    private float timeDelay = 0.1f; //in sec
    private bool maxOn = true;
    private bool linked = false;
    private string linkType = "Serial";

    // Start is called before the first frame update
    void Start()
    {
        pwMinBar.currentPercent = pwMin;
        AmpBar.currentPercent = amp;
        pwMaxBar.currentPercent = pwMax;
        stimOff.SetActive(true);
        stimOn.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            stim.StopStimulation();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            ZeroStim();
            stim.StopStimulation();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            test();
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            ZeroStim();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            stimOn.SetActive(true);
            stimOff.SetActive(false);
            upOn = true;
            StartCoroutine(UpCoroutine());
        }
        else if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            upOn = false;
            ZeroStim();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            stimOn.SetActive(true);
            stimOff.SetActive(false);
            downOn = true;
            StartCoroutine(DownCoroutine());
        }
        else if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            downOn = false;
            ZeroStim();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            maxOn = !maxOn;
            if(maxOn)
            {
                MaxSelect.SetActive(true);
                MinSelect.SetActive(false);
            }else
            {
                MaxSelect.SetActive(false);
                MinSelect.SetActive(true);
            }
        }

        if (stim.Ready())
        {
            ready.text = "Ready";
        }
        else
        {
            ready.text = "Setting Up...";
        }
    }

    IEnumerator UpCoroutine()
    {
        while (upOn)
        {
            increaseStim();
            yield return new WaitForSeconds(timeDelay);
        }
    }

    IEnumerator DownCoroutine()
    {
        while (downOn)
        {
            decreaseStim();
            yield return new WaitForSeconds(timeDelay);
        }
    }

    public void ZeroStim()
    {
        stim.StimulateAnalog("Thumb", true, 0, 0);
        stim.StimulateAnalog("Index", true, 0, 0);
        stim.StimulateAnalog("Middle", true, 0, 0);
        stimOn.SetActive(false);
        stimOff.SetActive(true);
    }

    public void test()
    {
        if (maxOn)
        {
            stim.StimulateAnalog("Thumb", true, pwMax, amp);
        } else
        {
            stim.StimulateAnalog("Thumb", true, pwMin, amp);
        }
        stimOn.SetActive(true);
        stimOff.SetActive(false);
    }

    private void increaseStim()
    {
        if (maxOn)
        {
            if (pwMax > 255 - deltaPW)
            {
                if (amp < 80)
                {
                    pwMax = 4;
                    amp += deltaAmp;
                    pwMin = 4;
                    pwMinBar.currentPercent = pwMin;
                }
            }
            else
            {
                pwMax += deltaPW;
            }
            pwMaxBar.currentPercent = pwMax;
            AmpBar.currentPercent = amp;
            stim.StimulateAnalog("Thumb", true, pwMax, amp);
        }
        else
        {
            if (pwMin < pwMax - deltaPW)
            {
                pwMin += deltaPW;
            }
            pwMinBar.currentPercent = pwMin;
            AmpBar.currentPercent = amp;
            stim.StimulateAnalog("Thumb", true, pwMin, amp);
        }
    }

    private void decreaseStim()
    {
        if (maxOn)
        {
            if (pwMax > pwMin+deltaPW)
            {
                pwMax -= deltaPW;
            }
            pwMaxBar.currentPercent = pwMax;
            AmpBar.currentPercent = amp;
            stim.StimulateAnalog("Thumb", true, pwMax, amp);
        }
        else
        {
            if (pwMin > 4+deltaPW)
            {
                pwMin -= deltaPW;
            }
            pwMinBar.currentPercent = pwMin;
            AmpBar.currentPercent = amp;
            stim.StimulateAnalog("Thumb", true, pwMin, amp);
        }
    }

    public void resetAmp()
    {
        amp = 2;
        pwMax = 4;
        pwMin = 4;
        pwMinBar.currentPercent = pwMin;
        AmpBar.currentPercent = amp;
        pwMaxBar.currentPercent = pwMax;
    }

    public void link()
    {
        if (linked)
        {
            linked = false;
            linkBTN.buttonText = "link";
            input.enabled = true;
            if (linkType == "serial")
            {
                crabSerial.SetActive(false);
            }
        }
        else
        {
            linked= true;
            linkBTN.buttonText = "unlink";
            input.enabled = false;
            if (linkType == "serial")
            {
                crabSerial.SetActive(true);
            }
        }
    }

    public void changeInput(string input)
    {
        if (input == "serial")
        {
            linkType = "serial";
        }
    }

    public void inputFloat(float input)
    {
        if (linked)
        {
            float pw= ((pwMax-pwMin)*input)+pwMin;
            if (input > 1)
            {
                stim.StimulateAnalog("Thumb", true, (int)pw, amp);
            }
            stim.StimulateAnalog("Thumb", true, 0, amp);
        }
    }

}
