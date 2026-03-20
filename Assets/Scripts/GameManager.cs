//#define EEG_DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using static GameManager;

// Main "driver" class - starts the gui,
// - manages EEG source and EEG games start/end/data record
// - other classes like EEGGameTutorial, EEGGame use this class public API to use EEG source and UXF library features
// - accepts messages from UI managers to change the state
// - communicates with api client: user data, games records data
public class GameManager : MonoBehaviour
{
    public enum AppState {
        Idle,
        InGame //see GameState
    }

    public enum GameState
    {
        Idle,
        GameProcess,
    }

    // State
    AppState appState = AppState.Idle;


    // API Client
    APIClient apiclient;

    // Local Data
    private string identityFileName = "identity.txt";

    // Singleton
    private static GameManager instance = null;

#if EEG_DEBUG
    // EEGInfo Scene update
    private double updateEvery = 1; //seconds
    private double updateCounter = 0;
#endif

    // 

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
#if EEG_DEBUG
        UIManagerGameScene.GetInstance().LoadEEGInfoSceneAdditive();
#else
        apiclient = new APIClient();
        appState = AppState.Idle;
#endif
    }

    // Update is called once per frame
    void Update()
    {
#if EEG_DEBUG
        this.StreamEEGDataToEEGInfoScene();
#else
        switch(appState)
        {
            case AppState.Idle:
                UIManagerGameScene.GetInstance().StartUI();
                break;
        }
#endif

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


    private bool InitEEGSource()//TODO: place in right place of the algorithm
    {
        return CrossPlatformEEGSourceFactory.GetInstance().InitEEGSource();
    }


#if EEG_DEBUG
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
#endif
    public bool StreamEEGSignal()
    {
        return CrossPlatformEEGSourceFactory.GetInstance().StartStreaming();
    }

    public bool StopEEGStream()
    {
        return CrossPlatformEEGSourceFactory.GetInstance().StopStreaming();
    }

    // Public API for game classes
    public int GetEEGSourceSamplingRate()
    {
        return CrossPlatformEEGSourceFactory.GetInstance().GetSamplingRate();
    }

    // Call as close as possible to a game and its actions start
    public void StartDataRecord()
    {
        this.StreamEEGSignal();
    }

    // Call as close as possible to a game and its actions finish
    public void FinishDataRecord()
    {
        this.StopEEGStream();
    }

    public void RegisterEvent(EEGEvent e)
    {

    }

    // Updates event data of given 'eventId' with data inside event 'e'
    public void UpdateEvent(string eventId, EEGEvent e)
    {

    }

    public float GetTimeElapsed()
    {
        return 0f;
    }

    // Public API for UI managers


    // Looks for local saved data: <auth token> and requests server for login
    // - if token present + login requests successful returns userId
    // otherwise returns null or ""
    public async Task<string> TryAutoLogin()
    {
        // Try to find token
        var identity = LocalStorage.Load<UserIdentity>(identityFileName);
        if (string.IsNullOrEmpty(identity.token)) { return null; }
        var userIdentity = await apiclient.Login(identity.token);
        if (string.IsNullOrEmpty(userIdentity.userId) || identity.userId != userIdentity.userId) { return null; }
        // Save data
        LocalStorage.Save<UserIdentity>(identityFileName, userIdentity);
        return userIdentity.userId;

    }

    public async Task<bool> RequestLogin(string email)
    {
        return await apiclient.RequestLogin(email);
    }

    public async Task<string> Login(string email, string code)
    {
        var userIdentity = await apiclient.Login(email, code);
        if (string.IsNullOrEmpty(userIdentity.userId) || string.IsNullOrEmpty(userIdentity.token)) { return null; }
        // save data
        LocalStorage.Save<UserIdentity>(identityFileName, userIdentity);
        return userIdentity.userId;
    }

    // Requests registration
    // if request was successfull (true new user + no network errors) - returns true
    // otherwise - returns false
    public async Task<bool> RequestRegisterUser(string email)
    {
        return await apiclient.RequestRegister(email);
    }

    // Requests actual user registration
    // Checks code from email, if wrong - returns null or ""
    // Saves <auth token>, setting up user data cache, returns userId string
    public async Task<string> RegisterUser(string email, string code)
    {
        var userIdentity = await apiclient.Register(email, code);
        if (string.IsNullOrEmpty(userIdentity.userId) || string.IsNullOrEmpty(userIdentity.token)) { return null;  }
        // save data
        LocalStorage.Save<UserIdentity>(identityFileName, userIdentity);
        return userIdentity.userId;
    }

    public void LogOut()
    {
        //delete api access token
        LocalStorage.Delete("token.txt");
        //delete user general data
        //TODO
    }
}
