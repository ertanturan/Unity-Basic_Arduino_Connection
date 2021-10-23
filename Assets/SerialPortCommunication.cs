using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class SerialPortCommunication : MonoBehaviour {

    private SerialPortController c;

    private const string I_ANIMATION_STARTED = "100";
    private const string I_ANIMATION_ENDED = "99";

    private const string O_ANIMATION_STOP = "99";

    public string portName = "COM3";
    public int baudRate = 9600;

    public System.Action<string> OnDataReceived;
    public System.Action<string> OnDataSent;

    public string PortName => c?.portName;
    public bool IsInitialized => c?.IsInitialized ?? false;
    public bool ConnectionClosed => c?.ConnectionClosed ?? true;



    void Awake ()
    {
        portName = PlayerPrefs.GetString("SPort", portName);
        c = new SerialPortController(portName, baudRate);
        c.Launch();
    }
		
	void Update ()
    {
        string receivedData;
        
        while (c.GetReceivedString(out receivedData))
        {
                Debug.Log("Received Data : " + receivedData);
        }
    }

    private void OnDestroy()
    {
        c.Stop();
    }
         
    public void SendCustomData(string data)
    {
        c.SendData(data);
    }

    public void Reconnect(string portName)
    {
        c.Stop();
        PlayerPrefs.SetString("SPort", portName??"");
        c = new SerialPortController(portName, baudRate, 3000);
        c.Launch();
    }
}
