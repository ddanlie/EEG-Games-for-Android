using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singleton
    private static GameManager instance = null;

    //  EEG data
    private double updateEvery = 1; //seconds
    private double updateCounter = 0;

    private void Awake()
    {
        if (GameManager.instance == null)
        {
            GameManager.instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // destroy duplicate
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        UIManagerGameScene.GetInstance().LoadEEGInfoSceneAdditive();
        EEGSignalSource.GetInstance().InitEEGSource();
    }

    // Update is called once per frame
    void Update()
    {
        // Stream EEG data
        updateCounter += Time.deltaTime;
        if(updateCounter > updateEvery)
        {
            updateCounter = 0;
            EEGSignalSource source = EEGSignalSource.GetInstance();
            if (source.IsSourceInitialized && source.IsSourceStreaming)
            {
                string dataText = source.GetCurrentDataFormatted();
                UIManagerEEGInfoScene.GetInstance().SetDataText(dataText);
            }
        }
    }

    public static GameManager GetInstance()
    {
        if (GameManager.instance == null)
        {
            instance = FindObjectOfType<GameManager>();
        }
        return instance;
    }

    ~GameManager() 
    {
        GameManager.instance = null;
    }

    private bool InitEEGSource()
    {
        EEGSignalSource source = EEGSignalSource.GetInstance();
        Debug.Log("Initializing EEG Source");
        if(source.InitEEGSource())
        {
            Debug.Log("EEG Initialized");
            return true;
        }
        else
        {
            Debug.LogError("EEG Init Failed");
            return false;
        }
    }

    public bool StreamEEGSignal()
    {
        EEGSignalSource source = EEGSignalSource.GetInstance();
        if (!this.InitEEGSource())
        {
            Debug.LogError("Streaming attempt failed");
            return false;
        }
        Debug.Log("Starting data stream");
        if (source.StartStreaming())
        {
            Debug.Log("Streaming started");
            return true;
        }
        else
        {
            Debug.LogError("Streaming attempt failed");
            return false;
        }
    }

    public bool StopEEGStream()
    {
        Debug.Log("Stop stream attempt");
        if (EEGSignalSource.GetInstance().StopStreaming())
        {
            Debug.Log("EEG stream stopped");
            return true;
        }
        else
        {
            Debug.LogError("Failed to stop stream");
            return false;
        }
    }
}
