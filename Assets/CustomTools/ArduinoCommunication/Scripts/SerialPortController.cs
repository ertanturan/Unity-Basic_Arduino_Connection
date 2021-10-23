using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class SerialPortController
{
    private Queue<byte> receivedData = new Queue<byte>(); 
    private Queue<string> receivedStringData = new Queue<string>();
    Queue<byte> sendingData = new Queue<byte>();

    SerialPort port;
    Thread listenerThread;
    Thread writerThread;
    object writerMutex = new object();
    public readonly bool WorksInBinaryMode;
    private int launchDelayMs = 0;

    private int isInitialized = 0;
    public bool IsInitialized
    { get { return isInitialized != 0; } }
    private int connectionClosed = 0;
    public bool ConnectionClosed
    { get { return connectionClosed != 0; } }

    public readonly string portName;
    public readonly int baudRate;

    public SerialPortController(string portname, int baudrate, bool worksInBinaryMode = false)
    {
        this.portName = portname;
        this.baudRate = baudrate;
        this.WorksInBinaryMode = worksInBinaryMode;
    }

    public SerialPortController(string portname, int baudrate, int launchDelayMs, bool worksInBinaryMode = false)
    {
        this.portName = portname;
        this.baudRate = baudrate;
        this.WorksInBinaryMode = worksInBinaryMode;
        this.launchDelayMs = launchDelayMs;
    }

    public void Launch()
    {
        if (listenerThread != null)
            throw new System.Exception("Was already launched before.");

        if(WorksInBinaryMode)
            listenerThread = new Thread(PortListenerBinary_T);
        else
            listenerThread = new Thread(PortListenerText_T);
        listenerThread.Start();
    }

    public void SendData(byte data)
    {
        Monitor.Enter(writerMutex);
        sendingData.Enqueue(data);
        Monitor.Pulse(writerMutex);
        Monitor.Exit(writerMutex);
    }

    public void SendData(string data)
    {
        if (data == null)
            return;

        Monitor.Enter(writerMutex);
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(data+port.NewLine);
        foreach(var b in bytes)
            sendingData.Enqueue(b);        
        Monitor.Pulse(writerMutex);
        Monitor.Exit(writerMutex);
    }

    public int ReceivedByteCount
    {
        get
        {
            int count;
            lock(receivedData)
            {
                count = receivedData.Count;
            }
            return count;
        }
    }

    public bool GetReceivedByte(out byte result)
    {
        lock (receivedData)
        {
            if (receivedData.Count == 0)
            {
                result = 0;
                return false;
            }
            result = receivedData.Dequeue();
            return true;
        }
    }

    public int ReceivedStringCount
    {
        get
        {
            lock (receivedStringData)
                return receivedStringData.Count;
        }
    }

    public bool GetReceivedString(out string result)
    {
        lock(receivedStringData)
        {
            if(receivedStringData.Count == 0)
            {
                result = null;
                return false;
            }
            result = receivedStringData.Dequeue();
            return true;
        }
    }

    public void Stop()
    {
        if(port != null && IsInitialized && !ConnectionClosed)
            port.Close();
    }

    private void InitializePort()
    {
        port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.None);

        try
        {
            port.Open();
        }
        catch(System.Exception e)
        {
            e = e;
        }

        if (port.IsOpen)
        {
            writerThread = new Thread(PortWriter_T);
            writerThread.Start();
        }
        else
            Interlocked.Exchange(ref connectionClosed, 1);

        Interlocked.Exchange(ref isInitialized, 1);
    }
    
    void PortListenerBinary_T()
    {
        if (launchDelayMs != 0)
            Thread.Sleep(launchDelayMs);

        InitializePort();
        
        try
        {
            while (port.IsOpen)
            {
                try
                { 
                    int data = port.ReadChar();
                    if (data == -1)
                        break; // connection closed

                    lock(receivedData)
                        receivedData.Enqueue((byte)data);
                }
                catch (System.TimeoutException e)
                {
                    // timeout is OK. keep reading.
                }
            }
        }
        catch(System.Exception e)
        {
            // serial port error (probably!)
            e = e;
        }
        Interlocked.Exchange(ref connectionClosed, 1);
    }

    void PortListenerText_T()
    {
        if (launchDelayMs != 0)
            Thread.Sleep(launchDelayMs);

        InitializePort();
        
        byte[] buffer = new byte[1];
        System.Text.StringBuilder str = new System.Text.StringBuilder(128);
        try
        {
            while (port.IsOpen)
            {
                try
                {
                    int data = port.Read(buffer, 0, 1);
                    if (data == -1 || data == 0)
                        break; // connection closed

                    data = buffer[0];
                    if (data == '\r')
                        ;// ignore
                    else if (data == '\n')
                    {
                        lock (receivedStringData)
                            receivedStringData.Enqueue(str.ToString());
                        str.Length = 0;
                    }
                    else
                        str.Append((char)data);                    
                }
                catch (System.TimeoutException e)
                {
                    // timeout is OK. keep reading.
                    e = e;
                }
            }
        }
        catch (System.Exception e)
        {
            // serial port error (probably!)
            e = e;
        }
        Interlocked.Exchange(ref connectionClosed, 1);
    }

    void PortWriter_T()
    {
        try
        { 
            byte[] data = new byte[1];
            while (true)
            {
                Monitor.Enter(writerMutex);
                while (sendingData.Count == 0)
                {
                    Monitor.Wait(writerMutex, 1000);
                    if (connectionClosed == 1)
                        return; // stop working
                }

                data[0] = sendingData.Dequeue();
                Monitor.Exit(writerMutex);

                if (connectionClosed == 1)
                    return; // stop working
                port.Write(data, 0, 1);
            }
        }
        catch(System.Exception e)
        {
            // serial port error
            e = e;
        }
        Interlocked.Exchange(ref connectionClosed, 1);
    }
}
