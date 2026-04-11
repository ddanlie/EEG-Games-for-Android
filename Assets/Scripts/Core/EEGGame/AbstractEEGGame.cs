using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UXF;

public abstract class AbstractEEGGame : MonoBehaviour
{
    private Session session = null;
    public bool Running { get; protected set; } = false;

    public bool IsTutorial { get; protected set; }

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
    public virtual void StartEEGGame(Session session, bool tutorial = true)
    {
        if (Running) return;
        IsTutorial = tutorial;
        this.session = session;
        StartCoroutine(StartEEGGame());
    }

    protected abstract IEnumerator StartEEGGame();

    // To Extend
    protected virtual void FinishEEGGame()
    {
        Running = false;
    }

    public abstract void SetSetting(string settingName, string value);
}
