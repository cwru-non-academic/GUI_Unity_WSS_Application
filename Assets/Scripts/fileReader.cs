using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;

public class fileReader : MonoBehaviour
{
    private string dataPointsPath;
    private StreamReader reader;
    private string line;
    private string[] array;
    private string[][] eventArray;
    private bool eventRunning = false;
    private bool fileLoaded = false;
    private bool eventListLoaded = false;
    private bool eventListRunning = false;
    private int eventIndex = 0;
    private int trialIndex = 0;
    private int index = 0;
    private string docName;

    [SerializeField] private MinMax mmController;
    [SerializeField] private string eventsDocName;
    [SerializeField] private TMP_Text eventNameTxt;
    [SerializeField] private TMP_Text inputTxt;
    [SerializeField] private TMP_Text eventTxt;
    [SerializeField] private TMP_Text trialTxt;
    [SerializeField] private float max;
    [SerializeField] private float min;
    [SerializeField] private int freq;

    private void OnEnable()
    {
        LoadEventList();
    }

    // Update is called once per frame
    void Update()
    {
        if (eventRunning && fileLoaded)
        {
            index = index + (int)Mathf.Round(Time.deltaTime * freq);
            if (index < array.Length)
            {
                mmController.inputFloat(normalize(float.Parse(array[index])));
                inputTxt.text = "Input: " + normalize(float.Parse(array[index])).ToString("F3");
            } else
            {
                StopEvent();
            }
            
        }
        
    }

    public void StartEvent()
    {
        if (!eventRunning)
        {
            eventRunning = true;
            index = 0;
        }
    }

    public void StopEvent()
    {
        if (eventRunning)
        {
            eventRunning = false;
        }
    }

    public void NextEvent()
    {
        if (eventIndex < eventArray[trialIndex].Length-1 && eventListLoaded)
        {
            eventIndex++;
            eventTxt.text = "Event: " + eventIndex.ToString();
            changeFile(eventArray[trialIndex][eventIndex]);
        }
        else
        {
            Debug.LogError("Event list not loaded or trial has insuficient events");
        }
    }

    public void PrevEvent()
    {
        if (eventIndex > 0 && eventListLoaded)
        {
            eventIndex--;
            eventTxt.text = "Event: " + eventIndex.ToString();
            changeFile(eventArray[trialIndex][eventIndex]);
        }
        else
        {
            Debug.LogError("Event list not loaded or event is first event");
        }
    }

    public void NextTrial()
    {
        if (trialIndex < eventArray.Length - 1 && eventListLoaded && eventListRunning)
        {
            if (cleanString(eventArray[trialIndex+1][eventIndex]) == "End")
            {
                eventListRunning = false;
            }
            else
            {
                trialIndex++;
                eventIndex = 0;
                changeFile(eventArray[trialIndex][eventIndex]);
            }
            eventTxt.text = "Event: " + eventIndex.ToString();
            trialTxt.text = "Trial: " + trialIndex.ToString();
        }
        else
        {
            Debug.LogError("Event list not loaded or event has insuficient trials");
        }
    }

    public void PrevTrial()
    {
        if (trialIndex > 0 && eventListLoaded)
        {
            trialIndex--;
            eventIndex = 0;
            eventTxt.text = "Event: " + eventIndex.ToString();
            trialTxt.text = "Trial: " + trialIndex.ToString();
            changeFile(eventArray[trialIndex][eventIndex]);
        }
        else
        {
            Debug.LogError("Event list not loaded or trial is first trial");
        }
    }

    public float normalize(float input)
    {
        float temp = (input - min) / (max - min);
        if(temp > 1.0)
        {
            return 1.0f;
        } else if (temp < 0)
        {
            return 0;
        }
        return temp;
    }
    public void changeFile(string doc)
    {
        StopEvent();
        fileLoaded = false;
        try
        {
            if(cleanString(doc)==string.Empty)
            {
                Debug.LogError("Event name in event list is empty");
                return;
            }
            docName = cleanString("/data/"+ doc +".csv");
            dataPointsPath = Path.Combine(Application.streamingAssetsPath + docName);
            reader = new StreamReader(dataPointsPath);
            eventNameTxt.text = cleanString(doc);
            string readContents = reader.ReadToEnd();
            array = readContents.Split(',');
            reader.Close();
            fileLoaded = true;
        } catch (System.Exception e)
        {
            Debug.LogError("Error importing stim event file with error code: " + e.ToString());
        }
    }

    public void LoadEventList()
    {
        eventListLoaded = false;
        eventListRunning = false;
        try
        {
            docName = "/data/" + eventsDocName;
            dataPointsPath = Path.Combine(Application.streamingAssetsPath+ docName);
            reader = new StreamReader(dataPointsPath);
            string readContents = reader.ReadToEnd();
            string[] trials = readContents.Split('\n');
            eventArray = new string[trials.Length][];
            for (int i = 0; i < trials.Length; i++)
            {
                eventArray[i] = trials[i].Split(",");
            }
            reader.Close();
            eventListLoaded = true;
            eventListRunning = true;
            eventIndex = 0;
            trialIndex = 0;
            eventTxt.text = "Event: " + eventIndex.ToString();
            trialTxt.text = "Trial: " + trialIndex.ToString();
            changeFile(eventArray[trialIndex][eventIndex]);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error importing event list file with error code: " + e.ToString());
        }
    }

    private string cleanString(string s)
    {
        s = s.Replace("\n", string.Empty);
        s = s.Replace("\r", string.Empty);
        s = s.Replace("\t", string.Empty);
        return s;
    }

    public int trialNumber()
    {
        return eventArray.Length;
    }

    public int eventNumberInTrial()
    {
        return eventArray[trialIndex].Length;
    }

    #region AandBComparisonMethods
    // for using A and B mode each trila is a different row and each event is a different column on that trial's row
    //event a is on column 0 and event b on column 1

    public void EventA()
    {
        if (eventListLoaded && eventArray[trialIndex].Length > 0)
        {
            eventTxt.text = "Event: 0";
            trialTxt.text = "Trial: " + trialIndex.ToString();
            changeFile(eventArray[trialIndex][0]);
            StartEvent();
        } else
        {
            Debug.LogError("Event list not loaded or trial has insuficient events");
        }
    }
    public void EventB()
    {
        if (eventListLoaded && eventArray[trialIndex].Length > 1)
        {
            eventTxt.text = "Event: 1";
            trialTxt.text = "Trial: " + trialIndex.ToString();
            changeFile(eventArray[trialIndex][1]);
            StartEvent();
        } else
        {
            Debug.LogError("Event list not loaded or trial has insuficient events");
        }
    }

    public string[] returnTrialData()
    {
        //[3] number is used to inform the reader that that was the last trial and to log
        string[] info = {trialIndex.ToString(),cleanString(eventArray[trialIndex][0]), cleanString(eventArray[trialIndex][1]), "0", mmController.getStimParams()[0], mmController.getStimParams()[1], mmController.getStimParams()[2] }; 
        NextTrial();
        if(!eventListRunning)
        {
            info[3] = "1";
        }
        return info;        
    }

    #endregion
}
