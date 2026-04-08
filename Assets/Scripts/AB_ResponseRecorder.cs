using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

public class AB_ResponseRecorder : MonoBehaviour
{
    [SerializeField] private fileReader reader;
    [SerializeField] private GameObject responseABTN;
    [SerializeField] private GameObject responseBBTN;

    //logger vars
    private StringBuilder A_and_B_Logger;
    private string trialLogFile;
    public Stopwatch timer;
    private bool endOfFile = false;

    void OnEnable()
    {
        endOfFile = false;
        timer = new Stopwatch();

        // Start Unity timer
        timer.Start();

        //starts logger
        A_and_B_Logger = new StringBuilder();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            creatLog();
        }
        //record events with keys
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if(reader.getEventAStatus() && reader.getEventBStatus() && !endOfFile)
            {
                logEvent("A");
            }
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (reader.getEventAStatus() && reader.getEventBStatus() && !endOfFile)
            {
                logEvent("B");
            }
        }
    }

    private void OnDisable()
    {
        if(!endOfFile)
        {
            creatLog();
        }
    }

    public void logEvent(string AorB)
    {
        string[] readerInfo = reader.returnTrialData();
        logTrialData(timer.Elapsed.TotalSeconds.ToString(), readerInfo[0], readerInfo[1], readerInfo[2], AorB, readerInfo[4], readerInfo[5], readerInfo[6]);
        if (readerInfo[3] == "1")
        {
            responseABTN.SetActive(false);
            responseBBTN.SetActive(false);
            endOfFile = true;
            creatLog();
        }
        reader.eventRecorded();
    }

    private void logTrialData(string time, string trial, string eventA, string eventB, string response, string max, string min, string amp)
    {
        A_and_B_Logger.Append(time + ",");
        A_and_B_Logger.Append(trial + ",");
        A_and_B_Logger.Append(eventA + ",");
        A_and_B_Logger.Append(eventB + ",");
        A_and_B_Logger.Append(response + ",");
        A_and_B_Logger.Append(max+ ",");
        A_and_B_Logger.Append(min+",");
        A_and_B_Logger.Append(amp+",");

        A_and_B_Logger.AppendLine();
    }

    public void creatLog()
    {
        StartCoroutine(logTrial());
        UnityEngine.Debug.LogError("Logging");

    }

    IEnumerator logTrial()
    {
        StreamWriter file;
        try
        {
            trialLogFile = "A_and_B_Logger" + System.String.Format("{0:_yyyy_MM_dd_hh_mm_ss}", System.DateTime.Now) + ".csv";
            file = new StreamWriter(trialLogFile);
            file.WriteLine("Time,Trial,EventA,EventB,Response,Max,Min,Amp,");
            file.WriteLine(A_and_B_Logger.ToString());
            file.Close();
            timer = new Stopwatch();
            timer.Start();
            A_and_B_Logger = new StringBuilder();
            UnityEngine.Debug.Log("FinishedLogging");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Error in accessing file: " + e);
        }
        yield return new WaitForSeconds(.1f);
    }
}
