using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UXF;

public abstract class AbstractEEGGame : MonoBehaviour
{
    protected Session uxfSession = null;
    protected EventLogger eventLogger = null;
    public bool Running { get; protected set; } = false;

    public bool IsTutorial { get; set; } = true;

    // Game Manager instance shorter name
    protected GameManager GameManager { get; private set; } = null;


    void Start()
    {
        GameManager = GameManager.GetInstance();
    }

    
    void Update()
    {
        
    }

    // Override/Extend API

    // To Extend
    public virtual void StartEEGGame(Session startedSession, EventLogger eventLogger)
    {
        if(!startedSession.hasInitialised && !IsTutorial)
        {
            throw new InvalidOperationException("UXF Session must be initialized before game start");
        }
        if (Running) return;
        Running = true;
        this.uxfSession = startedSession;
        this.eventLogger = eventLogger;
        StartCoroutine(StartEEGGame());
    }

    protected abstract IEnumerator StartEEGGame();

    // To Extend
    protected virtual void FinishEEGGame()
    {
        Running = false;
    }

    public abstract void SetSetting(string settingName, string value);

    public abstract Dictionary<string, object> GetCurrentGameSettings();
}
