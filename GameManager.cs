using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    // Singleton
    private static GameManager instance = null;

    // EEGInfo Scene update
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
        CrossPlatformEEGSourceFactory.GetInstance().InitEEGSource();
    }

    // Update is called once per frame
    void Update()
    {
        this.StreamEEGDataToEEGInfoScene();
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
        return CrossPlatformEEGSourceFactory.GetInstance().InitEEGSource();
    }

    private void StreamEEGDataToEEGInfoScene()
    {
        updateCounter += Time.deltaTime;
        if (updateCounter > updateEvery)
        {
            updateCounter = 0;
            AbstractEEGSignalSource source = CrossPlatformEEGSourceFactory.GetInstance();
            if (source.IsSourceInitialized && source.IsSourceStreaming)
            {
                string dataText = source.GetCurrentDataFormatted();
                string sourceStatus = $"\nInitialized: {source.IsSourceInitialized}\nStreaming: {source.IsSourceStreaming}";
                UIManagerEEGInfoScene.GetInstance().SetDataText(dataText+sourceStatus);
            }
        }
    }

    public bool StreamEEGSignal()
    {
        return CrossPlatformEEGSourceFactory.GetInstance().StartStreaming();
    }

    public bool StopEEGStream()
    {
        return CrossPlatformEEGSourceFactory.GetInstance().StopStreaming();
    }
}
