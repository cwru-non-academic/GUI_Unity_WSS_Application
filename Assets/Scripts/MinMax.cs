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
    [SerializeField] private GameObject fileReader;
    [SerializeField] private GameObject instructions;
    [SerializeField] private TMP_Text ready;
    [SerializeField] private PulseWidthProfileController pulseWidthProfiles;

    private int amp = 1;//in mA
    private int pwMin = 0; //in us
    private int pwMax = 0; //in us
    private int deltaAmp = 1;
    private bool upOn = false;
    private bool downOn = false;
    private float timeDelay = 0.1f; //in sec
    private float heldSeconds;
    private bool maxOn = true;
    private bool linked = false;
    private string linkType = "Serial";
    private bool uiToolkitOwnsMomentaryInput;
    private bool outputActive;

    public int MaximumPulseWidth => pwMax;
    public int MinimumPulseWidth => pwMin;
    public int Amplitude => amp;
    public bool MaximumSelected => maxOn;
    public bool Linked => linked;
    public string InputType => linkType;
    public bool OutputActive => outputActive;

    public void SetUIToolkitInputManaged(bool managed) => uiToolkitOwnsMomentaryInput = managed;

    // Start is called before the first frame update
    void Start()
    {
        if (pulseWidthProfiles == null)
        {
            Debug.LogError("MinMax: connect Pulse Width Profile Controller in the Inspector.", this);
            enabled = false;
            return;
        }
        pwMinBar.currentPercent = pwMin;
        AmpBar.currentPercent = amp;
        pwMaxBar.currentPercent = pwMax;
        stimOff.SetActive(true);
        stimOn.SetActive(false);
        ApplyPulseWidthMaximum(pulseWidthProfiles.ActiveMaximumPulseWidth);
        pulseWidthProfiles.ActiveMaximumChanged += ClampPulseWidths;
    }

    // Update is called once per frame
    void Update()
    {
        if (!uiToolkitOwnsMomentaryInput && Input.GetKeyDown(KeyCode.F8))
        {
            stim.StopStimulation();
        }
        else if (!uiToolkitOwnsMomentaryInput && Input.GetKeyDown(KeyCode.Escape))
        {
            ZeroStim();
            stim.StopStimulation();
        }
        else if (!uiToolkitOwnsMomentaryInput && Input.GetKeyDown(KeyCode.Space))
        {
            test();
        }
        else if (!uiToolkitOwnsMomentaryInput && Input.GetKeyUp(KeyCode.Space))
        {
            ZeroStim();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            stimOn.SetActive(true);
            stimOff.SetActive(false);
            outputActive = true;
            upOn = true;
            heldSeconds = 0;
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
            outputActive = true;
            downOn = true;
            heldSeconds = 0;
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
    }

    IEnumerator UpCoroutine()
    {
        while (upOn)
        {
            increaseStim();
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

    public void ZeroStim()
    {
        stim.StimulateAnalog("Thumb", 0, 0);
        stim.StimulateAnalog("Index", 0, 0);
        stim.StimulateAnalog("Middle", 0, 0);
        stimOn.SetActive(false);
        stimOff.SetActive(true);
        outputActive = false;
    }

    public void test()
    {
        if (maxOn)
        {
            stim.StimulateAnalog("Thumb", pwMax, amp);
        } else
        {
            stim.StimulateAnalog("Thumb", pwMin, amp);
        }
        stimOn.SetActive(true);
        stimOff.SetActive(false);
        outputActive = true;
    }

    private void increaseStim()
    {
        int deltaPW = CurrentPulseWidthStep();
        int pulseWidthMaximum = pulseWidthProfiles.ActiveMaximumPulseWidth;
        if (maxOn)
        {
            if (pwMax > pulseWidthMaximum - deltaPW)
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
            stim.StimulateAnalog("Thumb", pwMax, amp);
        }
        else
        {
            if (pwMin < pwMax - deltaPW)
            {
                pwMin += deltaPW;
            }
            pwMinBar.currentPercent = pwMin;
            AmpBar.currentPercent = amp;
            stim.StimulateAnalog("Thumb", pwMin, amp);
        }
        stim.UpdateChannelParams("Thumb", pwMax, pwMin, amp);
    }

    private void decreaseStim()
    {
        int deltaPW = CurrentPulseWidthStep();
        if (maxOn)
        {
            if (pwMax > pwMin+deltaPW)
            {
                pwMax -= deltaPW;
            }
            pwMaxBar.currentPercent = pwMax;
            AmpBar.currentPercent = amp;
            stim.StimulateAnalog("Thumb", pwMax, amp);
        }
        else
        {
            if (pwMin > 4+deltaPW)
            {
                pwMin -= deltaPW;
            }
            pwMinBar.currentPercent = pwMin;
            AmpBar.currentPercent = amp;
            stim.StimulateAnalog("Thumb", pwMin, amp);
        }
        stim.UpdateChannelParams("Thumb", pwMax, pwMin, amp);
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
        pwMax = Mathf.Min(pwMax, maximum);
        pwMin = Mathf.Min(pwMin, pwMax);
        pwMaxBar.SetValue(pwMax);
        pwMinBar.SetValue(pwMin);
        stim.UpdateChannelParams("Thumb", pwMax, pwMin, amp);
    }

    private void OnDestroy()
    {
        if (pulseWidthProfiles != null)
            pulseWidthProfiles.ActiveMaximumChanged -= ClampPulseWidths;
    }

    private void ApplyPulseWidthMaximum(int maximum)
    {
        SetBarMaximum(pwMinBar, maximum);
        SetBarMaximum(pwMaxBar, maximum);
    }

    private static void SetBarMaximum(ProgressBar bar, int maximum)
    {
        bar.maxValue = maximum;
        bar.valueLimit = maximum;
        bar.UpdateUI();
    }

    public void resetAmp()
    {
        amp = 1;
        pwMax = 4;
        pwMin = 4;
        pwMinBar.currentPercent = pwMin;
        AmpBar.currentPercent = amp;
        pwMaxBar.currentPercent = pwMax;
        stim.UpdateChannelParams("Thumb", pwMax, pwMin, amp);
    }

    public void link()
    {
        if (linked)
        {
            linked = false;
            linkBTN.buttonText = "link";
            input.enabled = true;
            instructions.SetActive(true);
            if (linkType == "serial")
            {
                crabSerial.SetActive(false);
            } else if (linkType == "file")
            {
                fileReader.SetActive(false);
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
            } else if (linkType == "file")
            {
                fileReader.SetActive(true);
                instructions.SetActive(false);
            }
        }
    }

    public void changeInput(string input)
    {
        if (input == "serial")
        {
            linkType = "serial";
        } else if (input == "file")
        {
            linkType = "file";
        }
    }

    public void inputFloat(float input)
    {
        if (linked)
        {
            //float pw= ((pwMax-pwMin)*input)+pwMin;
            if (input > 0.01)
            {
                stim.StimWithMode("Thumb", input);
                outputActive = true;
                //stim.StimulateAnalog("Thumb", true, (int)pw, amp);
            } else
            {
                stim.StimWithMode("Thumb", 0);
                outputActive = false;
                //stim.StimulateAnalog("Thumb", true, 0, amp);
            }
        }
    }

    public string[] getStimParams()
    {
        string[] info = {pwMax.ToString(),pwMin.ToString(), amp.ToString()};
        return info;
    }

}
