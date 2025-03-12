using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEditor;
using UnityEngine;

public class CrabSerialCNTR : MonoBehaviour
{
    [SerializeField] public bool forcePort = false;
    [SerializeField] private bool testMode = true;
    [SerializeField] private MinMax mmController;

    #region COM_Port_vars
    [SerializeField] public string comPort = "COM7";
    private const int BAUD_RATE = 9600;                   // bps that the UART is setup for
    private const StopBits STOP_BIT_COUNT = StopBits.One;   // 1 stop bit
    private const int BITS_PER_DATA_CHAR = 8;               // 8-bit data character length (i.e., we're using bytes as characters)
    private const Parity PARITY = Parity.None;              // parity disabled
    private const int timeOut = 10;
    private SerialPort stream;
    #endregion

    private bool running = false;

    void OnEnable()
    {
        if (!testMode) 
        {
            if (!forcePort) //overide useful when multiple ports
            {
                comPort = getCOMPort();
            }
            stream = new SerialPort(comPort, BAUD_RATE, PARITY, BITS_PER_DATA_CHAR, STOP_BIT_COUNT);
            stream.ReadTimeout = timeOut;
            stream.Open();
            running = true;
        }
        else
        {
            running = false;
        }
    }

    void OnDisable()
    {
        running = false;
        if (!testMode)
        {
            releaseCOM_port();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(running)
        {
            while (stream.BytesToRead > 1)
            {
                byte[] incoming = new byte[1];
                stream.Read(incoming, 0, incoming.Length);
                //Debug.Log("In " + ByteToString(incoming, incoming.Length)+ " "+ ((int)incoming[0]).ToString()); //debug to see byte input
                mmController.inputFloat(((int)incoming[0]) / 255.0f);
            }
        }
        
    }

    private string ByteToString(byte[] data, int lenght)
    {
        string str = "";
        for (int i = 0; i < lenght; i++)
        {
            str += data[i].ToString("x") + " ";
        }
        return str;
    }

    #region com_port_methods

    public void releaseCOM_port()
    {
        if (stream != null)
        {
            if (stream.IsOpen)
            {
                stream.Close();
            }
        }
    }

    //pick first com port in list so if multiple com ports use overide (set desired port in contructor in the form "COMX")
    private string getCOMPort()
    {
        string[] ports = SerialPort.GetPortNames();


        if (ports.Length > 1)
        {
            Debug.Log("Multiple Ports Beware");
            // Display each port name to the console.
            Debug.Log("The following serial ports were found:");
            foreach (string port in ports)
            {
                Debug.Log("Port: " + port.ToString());
            }
            Debug.Log("Using port: " + ports[1].ToString());
        }
        return ports[0].ToString();
    }
    #endregion
}
