using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class ArduinoServoWriter : MonoBehaviour {

    public Transform servoPhi;
    public Transform servoTheta;

    public SerialPort sp;
    Thread SerialThread;
    bool stopSerialCom = true;

    bool sendNow = false;
    int posTosend = 0;
    private int phiRotation=0;
    private int thetaRotation = 0;

    void Start()
    {
        StartCommuncation();
        //TestSend();
        //CloseConnection();
    }

    private void LateUpdate()
    {
        Send();
    }

    public void StartCommuncation()
    {
        SerialThread = new Thread(OnConnected);
        SerialThread.IsBackground = true;
        SerialThread.Start();
    }

    private void OnConnected()
    {
        //Open Connection
        OpenCon();
        sp.ReadTimeout = 2;

        //Run forever until stopSerialCom = true
        while (!stopSerialCom)
        {
            //Check if we should send
            if (sendNow)
            {

                Debug.Log("Rotações: Phi:" + phiRotation.ToString() + " Theta: " + thetaRotation.ToString());
                //Send
                SendValuesToSerial(phiRotation, thetaRotation);

                sendNow = false;
            }
            Thread.Sleep(1);
        }
    }

    private void OpenCon(string comPort = "COM3", int port = 9600)// se escreve assim para portas de 2 digitos:  \\\\.\\COM10
    {
        sp = new SerialPort(comPort, port);

        if (sp != null)
        {
            if (sp.IsOpen)
            {
                sp.Close();
            }
            try
            {
                sp.Open();
                stopSerialCom = false;
                Debug.Log("Opened!");
            }
            catch (System.Exception e)
            {
                Debug.Log("Error on open: " + e.Message);
            }

        }
    }

    public void CloseConnection()
    {
        stopSerialCom = true;

        //stop thread
        if (SerialThread != null && SerialThread.IsAlive)
        {
            Debug.Log("Thread Aborted!");
            SerialThread.Abort();
        }

        if (sp != null && sp.IsOpen)
        {
            sp.Close();
            Debug.Log("Closed!");
        }
    }

    void OnApplicationQuit()
    {
        CloseConnection();
    }


    public void Send()
    {
        phiRotation = (int)servoPhi.eulerAngles.z < 180? (int)servoPhi.eulerAngles.z + 90 : 90 -( 360 - (int)servoPhi.eulerAngles.z);
        thetaRotation = (int)servoTheta.eulerAngles.y;
        sendNow = true;
    }

    private void SendValuesToSerial(int phiRotation, int thetaRotation)
    {
        try
        {
            string servoValues = "<" + phiRotation.ToString() + "," + thetaRotation.ToString() + ">\n";
            Debug.Log("Mandando dados para arduino: " + phiRotation.ToString() + " e " + thetaRotation.ToString());
            sp.Write(servoValues);
        }
        catch (System.Exception e)
        {
            Debug.Log("Error: " + e.Message);
        }
    }

    private void TestSend()
    {

        //Open Connection
        OpenCon();
        sp.ReadTimeout = 2;

        SendValuesToSerial(5, 5);
    }

}
