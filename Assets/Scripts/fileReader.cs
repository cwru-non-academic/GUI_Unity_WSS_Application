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
    private string[] eventArray;
    private bool eventRunning = false;
    private bool fileLoaded = false;
    private bool eventListLoaded = false;
    private int eventIndex = 0;
    private int index = 0;
    private string docName;

    [SerializeField] private MinMax mmController;
    [SerializeField] private string eventsDocName;
    [SerializeField] private TMP_Text eventTxt;
    [SerializeField] private TMP_Text inputTxt;
    [SerializeField] private float max;
    [SerializeField] private float min;
    [SerializeField] private int freq;


    // Start is called before the first frame update
    void Start()
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
        if (eventIndex < eventArray.Length)
        {
            eventIndex++;
            changeFile(eventArray[eventIndex]);
        }
    }

    public float normalize(float input)
    {
        return (input - min) / (max - min);
    }
    public void changeFile(string doc)
    {
        StopEvent();
        fileLoaded = false;
        try
        {
            docName = cleanString("/data/"+ doc +".csv");
            dataPointsPath = Path.Combine(Application.streamingAssetsPath + docName);
            reader = new StreamReader(dataPointsPath);
            eventTxt.text = cleanString(doc);
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
        try
        {
            docName = "/data/" + eventsDocName;
            dataPointsPath = Path.Combine(Application.streamingAssetsPath+ docName);
            reader = new StreamReader(dataPointsPath);
            string readContents = reader.ReadToEnd();
            eventArray = readContents.Split(',');
            reader.Close();
            eventListLoaded = true;
            changeFile(eventArray[eventIndex]);
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
}
