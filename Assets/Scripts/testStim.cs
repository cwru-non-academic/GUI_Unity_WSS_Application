using Michsky.MUIP;
using System.Collections;
using TMPro;
using UnityEngine;

public class testStim : MonoBehaviour
{
    [SerializeField] private Stimulation stim;
    [SerializeField] private StimUI stimUI;
    [SerializeField] private ProgressBar pwBar;
    [SerializeField] private ProgressBar AmpBar;
    [SerializeField] private ProgressBar pwBar2;
    [SerializeField] private ProgressBar AmpBar2;
    [SerializeField] private ProgressBar pwBar3;
    [SerializeField] private ProgressBar AmpBar3;
    [SerializeField] private GameObject ch1Select;
    [SerializeField] private GameObject ch2Select;
    [SerializeField] private GameObject ch3Select;
    [SerializeField] private CustomDropdown shapes;
    [SerializeField] private PulseWidthProfileController pulseWidthProfiles;

    private int amp =2;//in mA
    private int pw = 0; //in us
    private int amp2 = 2;//in mA
    private int pw2 = 0; //in us
    private int amp3 = 2;//in mA
    private int pw3 = 0; //in us
    private int deltaAmp = 1;
    private bool upOn = false;
    private bool downOn = false;
    private float timeDelay = 0.1f; //in sec
    private float heldSeconds;
    private int currentCH;
    private bool uiToolkitOwnsMomentaryInput;
    private bool outputActive;

    public int CurrentChannel => currentCH;
    public int PulseWidth1 => pw;
    public int PulseWidth2 => pw2;
    public int PulseWidth3 => pw3;
    public int Amplitude1 => amp;
    public int Amplitude2 => amp2;
    public int Amplitude3 => amp3;
    public bool OutputActive => outputActive;

    public void SetUIToolkitInputManaged(bool managed) => uiToolkitOwnsMomentaryInput = managed;

    // Start is called before the first frame update
    void Start()
    {
        if (pulseWidthProfiles == null)
        {
            Debug.LogError("testStim: connect Pulse Width Profile Controller in the Inspector.", this);
            enabled = false;
            return;
        }
        pwBar.currentPercent = pw;
        AmpBar.currentPercent = amp;
        pwBar2.currentPercent = pw2;
        AmpBar2.currentPercent = amp2;
        pwBar3.currentPercent = pw3;
        AmpBar3.currentPercent = amp3;
        currentCH = 1;
        ApplyPulseWidthMaximum(pulseWidthProfiles.ActiveMaximumPulseWidth);
        pulseWidthProfiles.ActiveMaximumChanged += ClampPulseWidths;
    }

    // Update is called once per frame
    void Update()
    {
        if (!uiToolkitOwnsMomentaryInput && Input.GetKeyDown(KeyCode.F1))
        {
            stim.StopStimulation();
        } else if (!uiToolkitOwnsMomentaryInput && Input.GetKeyDown(KeyCode.Escape))
        {
            ZeroStim();
            stim.StopStimulation();
        } else if(!uiToolkitOwnsMomentaryInput && Input.GetKeyDown(KeyCode.Space))
        {
            test();
        }
        else if (!uiToolkitOwnsMomentaryInput && Input.GetKeyUp(KeyCode.Space))
        {
            ZeroStim();
        }else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            stimUI.StimActive();
            outputActive = true;
            upOn = true;
            heldSeconds = 0;
            StartCoroutine(UpCoroutine());
        } else if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            upOn = false;
            ZeroStim() ;
        } else if( Input.GetKeyDown(KeyCode.DownArrow)) 
        {
            stimUI.StimActive();
            outputActive = true;
            downOn = true;
            heldSeconds = 0;
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

    }

    IEnumerator UpCoroutine()
    {
        while (upOn)
        {
            increaseStim() ;
            heldSeconds += timeDelay;
            yield return new WaitForSeconds(timeDelay);
        }
    }

    IEnumerator DownCoroutine()
    {
        while (downOn)
        {
            decreaseStim();
            heldSeconds += timeDelay;
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
        stim.StimulateAnalog("Thumb", 0,0);
        stim.StimulateAnalog("Index", 0, 0);
        stim.StimulateAnalog("Middle", 0, 0);
        stimUI.StimNotActive();
        outputActive = false;
    }
    
    public void test()
    {
        stim.StimulateAnalog("Thumb", pw, amp);
        stim.StimulateAnalog("Index", pw2, amp2);
        stim.StimulateAnalog("Middle", pw3, amp3);
        stimUI.StimActive();
        outputActive = true;
    }

    private void increaseStim()
    {
        int deltaPW = CurrentPulseWidthStep();
        int pulseWidthMaximum = pulseWidthProfiles.ActiveMaximumPulseWidth;
        if (currentCH==1)
        {
            if (pw > pulseWidthMaximum - deltaPW)
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
            stim.StimulateAnalog("Thumb", pw, amp);
        } else if(currentCH==2)
        {
            if (pw2 > pulseWidthMaximum - deltaPW)
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
            stim.StimulateAnalog("Index", pw2, amp2);
        } else if (currentCH == 3)
        {
            if (pw3 > pulseWidthMaximum - deltaPW)
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
            stim.StimulateAnalog("Middle", pw3, amp3);
        }
    }

    private void decreaseStim()
    {
        int deltaPW = CurrentPulseWidthStep();
        int pulseWidthMaximum = pulseWidthProfiles.ActiveMaximumPulseWidth;
        if (currentCH==1)
        {
            if (pw < deltaPW)
            { 
                if (amp > 1)
                {
                    pw = pulseWidthMaximum;
                    amp -= deltaAmp;
                }
            }
            else
            {
                pw -= deltaPW;
            }
            pwBar.currentPercent = pw;
            AmpBar.currentPercent = amp;
            stim.StimulateAnalog("Thumb", pw, amp);
        } else if(currentCH==2) {
            if (pw2 < deltaPW)
            {
                if (amp2 > 1)
                {
                    pw2 = pulseWidthMaximum;
                    amp2 -= deltaAmp;
                }
            }
            else
            {
                pw2 -= deltaPW;
            }
            pwBar2.currentPercent = pw2;
            AmpBar2.currentPercent = amp2;
            stim.StimulateAnalog("Index", pw2, amp2);
        } else if (currentCH == 3)
        {
            if (pw3 < deltaPW)
            {
                if (amp3 > 1)
                {
                    pw3 = pulseWidthMaximum;
                    amp3 -= deltaAmp;
                }
            }
            else
            {
                pw3 -= deltaPW;
            }
            pwBar3.currentPercent = pw3;
            AmpBar3.currentPercent = amp3;
            stim.StimulateAnalog("Middle", pw3, amp3);
        }
    }

    public void SelectWaveform(int index)
    {
        if (shapes == null || index < 0 || index >= shapes.items.Count) return;
        shapes.index = index;
        shapes.SetDropdownIndex(index);
    }

    private int CurrentPulseWidthStep()
    {
        if (heldSeconds >= 3f) return 25;
        if (heldSeconds >= 1.5f) return 10;
        if (heldSeconds >= 0.5f) return 5;
        return 1;
    }

    private void ClampPulseWidths(int maximum)
    {
        ApplyPulseWidthMaximum(maximum);
        pw = Mathf.Min(pw, maximum);
        pw2 = Mathf.Min(pw2, maximum);
        pw3 = Mathf.Min(pw3, maximum);
        pwBar.SetValue(pw);
        pwBar2.SetValue(pw2);
        pwBar3.SetValue(pw3);
    }

    private void OnDestroy()
    {
        if (pulseWidthProfiles != null)
            pulseWidthProfiles.ActiveMaximumChanged -= ClampPulseWidths;
    }

    private void ApplyPulseWidthMaximum(int maximum)
    {
        SetBarMaximum(pwBar, maximum);
        SetBarMaximum(pwBar2, maximum);
        SetBarMaximum(pwBar3, maximum);
    }

    private static void SetBarMaximum(ProgressBar bar, int maximum)
    {
        bar.maxValue = maximum;
        bar.valueLimit = maximum;
        bar.UpdateUI();
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
