using Michsky.MUIP;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.PackageManager;
using UnityEngine;

public class testStim : MonoBehaviour
{
    [SerializeField] private Stimulation stim;
    [SerializeField] private ProgressBar pwBar;
    [SerializeField] private ProgressBar AmpBar;
    [SerializeField] private ProgressBar pwBar2;
    [SerializeField] private ProgressBar AmpBar2;
    [SerializeField] private GameObject stimOn;
    [SerializeField] private GameObject stimOff;

    private int amp =2;//in mA
    private int pw = 0; //in us
    private int amp2 = 2;//in mA
    private int pw2 = 0; //in us
    private int deltaPW = 1;
    private int deltaAmp = 1;
    private bool upOn = false;
    private bool downOn = false;
    private float timeDelay = 0.1f; //in sec
    private bool ch1;

    [SerializeField] private TMP_Text ready;
    // Start is called before the first frame update
    void Start()
    {
        pwBar.currentPercent = pw;
        AmpBar.currentPercent = amp;
        pwBar2.currentPercent = pw2;
        AmpBar2.currentPercent = amp2;
        ch1 = true;
        stimOff.SetActive(true);
        stimOn.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            stim.StopStimulation();
        } else if (Input.GetKeyDown(KeyCode.Escape))
        {
            ZeroStim();
            stim.StopStimulation();
        } else if(Input.GetKeyDown(KeyCode.Space))
        {
            test();
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            ZeroStim();
        }else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            stimOn.SetActive(true);
            stimOff.SetActive(false);
            upOn = true;
            StartCoroutine(UpCoroutine());
        } else if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            upOn = false;
            ZeroStim() ;
        } else if( Input.GetKeyDown(KeyCode.DownArrow)) 
        {
            stimOn.SetActive(true);
            stimOff.SetActive(false);
            downOn = true;
            StartCoroutine(DownCoroutine());
        } else if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            downOn = false;
            ZeroStim();
        } else if(Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            ch1 = !ch1;
        }

        if (stim.Ready())
        {
            ready.text = "Ready";
        } else
        {
            ready.text = "Setting Up...";
        }

    }

    IEnumerator UpCoroutine()
    {
        while (upOn)
        {
            increaseStim() ;
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
        stim.StimulateAnalog("Thumb", true, 0,0);
        stim.StimulateAnalog("Index", true, 0, 0);
        stimOn.SetActive (false);
        stimOff.SetActive(true) ;
    }
    
    public void test()
    {
        stim.StimulateAnalog("Thumb", true, pw, amp);
        stim.StimulateAnalog("Index", true, pw2, amp2);
        stimOn.SetActive(true);
        stimOff.SetActive(false);
    }

    private void increaseStim()
    {
        if (ch1)
        {
            if (pw > 255 - deltaPW)
            {
                if(amp < 80)
                {
                    pw = 4;
                    amp += deltaAmp;
                }
            }
            else
            {
                pw += deltaPW;
            }
            pwBar.currentPercent = pw;
            AmpBar.currentPercent = amp;
            stim.StimulateAnalog("Thumb", true, pw, amp);
        } else
        {
            if (pw2 > 255 - deltaPW)
            { 
                if (amp2 < 80)
                {
                    pw2 = 4;
                    amp2 += deltaAmp;
                }
            }
            else
            {
                pw2 += deltaPW;
            }
            pwBar2.currentPercent = pw2;
            AmpBar2.currentPercent = amp2;
            stim.StimulateAnalog("Index", true, pw2, amp2);
        }
    }

    private void decreaseStim()
    {
        if (ch1)
        {
            if (pw < deltaPW)
            { 
                if (amp > 1)
                {
                    pw = 255;
                    amp -= deltaAmp;
                }
            }
            else
            {
                pw -= deltaPW;
            }
            pwBar.currentPercent = pw;
            AmpBar.currentPercent = amp;
            stim.StimulateAnalog("Thumb", true, pw, amp);
        } else {
            if (pw2 < deltaPW)
            {
                if (amp2 > 1)
                {
                    pw2 = 255;
                    amp2 -= deltaAmp;
                }
            }
            else
            {
                pw2 -= deltaPW;
            }
            pwBar2.currentPercent = pw2;
            AmpBar2.currentPercent = amp2;
            stim.StimulateAnalog("Index", true, pw2, amp2);
        }
    }


    public void resetAmp()
    {
        amp = 2;
        amp2 = 2;
        pw = 0;
        pw2 = 0;
        pwBar2.currentPercent = pw2;
        AmpBar2.currentPercent = amp2;
        pwBar.currentPercent = pw;
        AmpBar.currentPercent = amp;
    }


}
