using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UXF;

public abstract class AbstractEEGGame : MonoBehaviour
{
    private Session session = null;
    public bool Running { get; protected set; } = false;

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
    public virtual void StartEEGGame(Session session)
    {
        if (Running) return;
        this.session = session;
    }

    // To Extend
    protected virtual void FinishEEGGame()
    {
        Running = false;
    }

    public abstract void SetSetting(string settingName, string value);
}
