using Michsky.MUIP;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class testStim : MonoBehaviour
{
    [SerializeField] private Stimulation stim;
    [SerializeField] private ProgressBar pwBar;
    [SerializeField] private ProgressBar AmpBar;
    [SerializeField] private GameObject stimOn;
    [SerializeField] private GameObject stimOff;

    private int amp =3;//in mA
    private int pw = 0; //in us
    private int deltaPW = 1;
    private int deltaAmp = 1;
    private bool upOn = false;
    private bool downOn = false;
    private float timeDelay = 0.1f; //in sec

    [SerializeField] private TMP_Text ready;
    // Start is called before the first frame update
    void Start()
    {
        pwBar.currentPercent = pw;
        AmpBar.currentPercent = amp;
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
        stimOn.SetActive (false);
        stimOff.SetActive(true) ;
    }
    
    public void test()
    {
        stim.StimulateAnalog("Thumb", true, pw, amp);
        stimOn.SetActive(true);
        stimOff.SetActive(false);
    }

    private void increaseStim()
    {
        if (pw>255-deltaPW)
        {
            pw = 4;
            amp += deltaAmp;
        }else
        {
            pw += deltaPW;
        }
        pwBar.currentPercent = pw;
        AmpBar.currentPercent = amp;
    }

    private void decreaseStim()
    {
        if(pw<deltaPW)
        {
            pw = 255;
            amp -= deltaAmp;
        } else
        {
            pw -= deltaPW;
        }
        pwBar.currentPercent = pw;
        AmpBar.currentPercent = amp;
    }


    public void resetAmp()
    {
        amp = 3;
    }

}
