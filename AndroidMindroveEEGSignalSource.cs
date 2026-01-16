using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using mindrove;

using Unity.VisualScripting;
using System;
using System.Threading;
using System.Linq;
using UnityEngine.UIElements;

public class AndroidMindroveEEGSignalSource : AbstractEEGSignalSource
{
    //# 0 → Fp1  1
    //# 1 → Fp2  2
    //# 2 → C5   3
    //# 3 → C1   4
    //# 4 → C2   5
    //# 5 → C6   6
    //# 6 → O1   7
    //# 7 → O2   8

    // Android mindrove server
    private AndroidJavaObject serverManager;
    //private AndroidJavaObject currentActivity;
    // Cross callback eegData setup
    private readonly System.Object crossCallbackLock = new System.Object();
    private double[][] tmpEEGData = null;


    private class ServerDataProcessCallback : AndroidJavaProxy 
    { 
        private Action<object> processDataCallback = null;
        public ServerDataProcessCallback(Action<object> processDataCallback) : base("kotlin.jvm.functions.Function1") 
        {
            this.processDataCallback = processDataCallback;
        } 
        public object invoke(object sensorData) 
        {
            //Debug.Log("Server Manager callback invoked");
            this.processDataCallback(sensorData);
            return null;
        }
    }
    protected override bool CustomInitEEGSource()
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            Debug.Log($"Android platform expected to init EEG source, found {Application.platform}");
            return false;
        }
        // Get the current Android Activity context
        // No need in activity, let this be here just as example
        //using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        //{
        //    currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        //}
        // Initialize the MindRove ServerManager from the AAR
        this.serverManager = new AndroidJavaObject("mylibrary.mindrove.ServerManager", new ServerDataProcessCallback(this.PassThroughStreamData));
        if (this.serverManager == null) 
        {
            return false;
        }
        // Start the server
        this.serverManager.Call("start");
        this.serverManager.Call("pause");// do not stream yet

        return true;
    }

    private void PassThroughStreamData(object sensorData)
    {
        //TODO: get all channels
        double ch1 = ((AndroidJavaObject)sensorData).Get<double>("channel1");
        double ch2 = ((AndroidJavaObject)sensorData).Get<double>("channel2");
        double ch3 = ((AndroidJavaObject)sensorData).Get<double>("channel3");
        double ch4 = ((AndroidJavaObject)sensorData).Get<double>("channel4");
        double ch5 = ((AndroidJavaObject)sensorData).Get<double>("channel5");
        double ch6 = ((AndroidJavaObject)sensorData).Get<double>("channel6");
        double ch7 = ((AndroidJavaObject)sensorData).Get<double>("channel7");
        double ch8 = ((AndroidJavaObject)sensorData).Get<double>("channel8");

        Debug.Log($"Asking sensor data for values: {ch1} {ch2} {ch3} {ch4} {ch5} {ch6} {ch7} {ch8}");

        lock (crossCallbackLock)
        {
            this.tmpEEGData = new double[][]
            {
                new double[] { ch1 },
                new double[] { ch2 },
                new double[] { ch3 },
                new double[] { ch4 },
                new double[] { ch5 },
                new double[] { ch6 },
                new double[] { ch7 },
                new double[] { ch8 }
            };
        }
    }

    public override double[][] GetCurrentData()
    {
        return this.EEGData;
    }
    
    protected override double[][] AssignStreamData()
    {
        lock (crossCallbackLock) 
        {
            return this.tmpEEGData;
        }
    }

    protected override bool CustomEndSession()
    {
        if (this.serverManager == null)
        {
            return true;
        }
        serverManager.Call("stop");
        serverManager = null;
        return true;
    }

    protected override bool CustomStartStreaming()
    {
        serverManager.Call("resume");
        return true;
    }

    protected override bool CustomStopStreaming()
    {
        serverManager.Call("pause");
        return true;
    }
}