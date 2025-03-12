using Michsky.MUIP;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;

public class portScanner : MonoBehaviour
{
    enum SerialType
    {
        stim,
        crab
    }

    [SerializeField] private Stimulation stim;
    [SerializeField] private CrabSerialCNTR crab;
    [SerializeField] private CustomDropdown serialList;
    [SerializeField] private SerialType serialType;

    private string selectedPort = "";
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void OnEnable()
    {
        if (serialType == SerialType.stim)
        {
            stim.gameObject.SetActive(false);
            stim.forcePort = true;
        } else if (serialType == SerialType.crab)
        {
            crab.gameObject.SetActive(false);
            crab.forcePort = true;
        }
        generatePortList();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void generatePortList()
    {
        serialList.items.Clear();
        string[] ports = SerialPort.GetPortNames();
        if(ports.Length > 0)
        {
            foreach (string port in ports)
            {
                serialList.CreateNewItem(port, false);
            }
        }else
        {
            serialList.CreateNewItem("empty", false);
        }
        serialList.SetupDropdown();
        serialList.SetDropdownIndex(0);
    }

    void OnDisable()
    {
        if (serialType == SerialType.stim)
        {
            stim.gameObject.SetActive(false);
            stim.forcePort = true;
            stim.gameObject.SetActive(true);
        }
        else if (serialType == SerialType.crab)
        {
            crab.gameObject.SetActive(false);
            crab.forcePort = true;
            crab.gameObject.SetActive(true);
        }
    }

    public void selectPort()
    {
        selectedPort = serialList.items[serialList.index].itemName;
        if(selectedPort != "empty")
        {
            if (serialType == SerialType.stim)
            {
                stim.gameObject.SetActive(false);
                stim.comPort = selectedPort;
                stim.gameObject.SetActive(true);
            }
            else if (serialType == SerialType.crab)
            {
                crab.gameObject.SetActive(false);
                crab.comPort = selectedPort;
                crab.gameObject.SetActive(true);
            }
        }
        
    }
}
