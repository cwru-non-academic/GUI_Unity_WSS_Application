using Michsky.MUIP;
using System.Collections;
using TMPro;
using UnityEngine;

public class testStim : MonoBehaviour
{
    [SerializeField] private Stimulation stim;
    [SerializeField] private ProgressBar pwBar;
    [SerializeField] private ProgressBar AmpBar;
    [SerializeField] private ProgressBar pwBar2;
    [SerializeField] private ProgressBar AmpBar2;
    [SerializeField] private ProgressBar pwBar3;
    [SerializeField] private ProgressBar AmpBar3;
    [SerializeField] private GameObject ch1Select;
    [SerializeField] private GameObject ch2Select;
    [SerializeField] private GameObject ch3Select;
    [SerializeField] private GameObject stimOn;
    [SerializeField] private GameObject stimOff;
    [SerializeField] private CustomDropdown shapes;

    private int amp =2;//in mA
    private int pw = 0; //in us
    private int amp2 = 2;//in mA
    private int pw2 = 0; //in us
    private int amp3 = 2;//in mA
    private int pw3 = 0; //in us
    private int deltaPW = 1;
    private int deltaAmp = 1;
    private bool upOn = false;
    private bool downOn = false;
    private float timeDelay = 0.1f; //in sec
    private int currentCH;

    [SerializeField] private TMP_Text ready;
    // Start is called before the first frame update
    void Start()
    {
        pwBar.currentPercent = pw;
        AmpBar.currentPercent = amp;
        pwBar2.currentPercent = pw2;
        AmpBar2.currentPercent = amp2;
        pwBar3.currentPercent = pw3;
        AmpBar3.currentPercent = amp3;
        currentCH = 1;
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
        } else if(Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentCH > 1)
            {
                currentCH--;
            } else
            {
                currentCH = 3;
            }
            updateChanelSelect();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (currentCH <3)
            {
                currentCH++;
            }
            else
            {
                currentCH = 1;
            }
            updateChanelSelect();
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

    private void updateChanelSelect()
    {
        switch(currentCH)
        {
            case 1:
                ch1Select.SetActive(true);
                ch2Select.SetActive(false);
                ch3Select.SetActive(false);
                break;
            case 2:
                ch2Select.SetActive(true);
                ch1Select.SetActive(false);
                ch3Select.SetActive(false);
                break;
            case 3:
                ch3Select.SetActive(true);
                ch2Select.SetActive(false);
                ch1Select.SetActive(false);
                break;
            default:
                ch1Select.SetActive(true);
                ch2Select.SetActive(false);
                ch3Select.SetActive(false);
                break;
        }
    }

    public void updateWaveform()
    {
        int shape = shapes.index;
        if(shape==10)//trapezoid
        {
            Debug.Log("waveform currently not suported");
        }else if(shape==11)//custom waveform
        {
            stim.updateWaveform(1, 11, 12, currentCH);
        }else if(shape<10)//other presets
        {
            stim.updateWaveform(1, shape, shape, currentCH);
        }
    }

    public void ZeroStim()
    {
        stim.StimulateAnalog("Thumb", true, 0,0);
        stim.StimulateAnalog("Index", true, 0, 0);
        stim.StimulateAnalog("Middle", true, 0, 0);
        stimOn.SetActive (false);
        stimOff.SetActive(true) ;
    }
    
    public void test()
    {
        stim.StimulateAnalog("Thumb", true, pw, amp);
        stim.StimulateAnalog("Index", true, pw2, amp2);
        stim.StimulateAnalog("Middle", true, pw3, amp3);
        stimOn.SetActive(true);
        stimOff.SetActive(false);
    }

    private void increaseStim()
    {
        if (currentCH==1)
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
        } else if(currentCH==2)
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
        } else if (currentCH == 3)
        {
            if (pw3 > 255 - deltaPW)
            {
                if (amp3 < 80)
                {
                    pw3 = 4;
                    amp3 += deltaAmp;
                }
            }
            else
            {
                pw3 += deltaPW;
            }
            pwBar3.currentPercent = pw3;
            AmpBar3.currentPercent = amp3;
            stim.StimulateAnalog("Middle", true, pw3, amp3);
        }
    }

    private void decreaseStim()
    {
        if (currentCH==1)
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
        } else if(currentCH==2) {
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
        } else if (currentCH == 3)
        {
            if (pw3 < deltaPW)
            {
                if (amp3 > 1)
                {
                    pw3 = 255;
                    amp3 -= deltaAmp;
                }
            }
            else
            {
                pw3 -= deltaPW;
            }
            pwBar3.currentPercent = pw3;
            AmpBar3.currentPercent = amp3;
            stim.StimulateAnalog("Middle", true, pw3, amp3);
        }
    }


    public void resetAmp()
    {
        amp = 2;
        amp2 = 2;
        amp3 = 2;
        pw = 0;
        pw2 = 0;
        pw3 = 0;
        pwBar2.currentPercent = pw2;
        AmpBar2.currentPercent = amp2;
        pwBar.currentPercent = pw;
        AmpBar.currentPercent = amp;
        pwBar3.currentPercent = pw3;
        AmpBar3.currentPercent = amp3;
    }


}
