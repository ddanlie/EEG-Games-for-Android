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
        this.session = session;
        if (Running) return;
    }

    // To Extend
    protected virtual void FinishEEGGame()
    {

        Running = false;
    }
}
