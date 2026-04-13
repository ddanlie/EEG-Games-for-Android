//#define EEG_DEBUG
#define API_DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UXF;
using static GameManager;
using static UIManagerGameScene;

// Main "driver" class - starts the gui,
// - manages EEG source and EEG games start/end/data record
// - other classes like EEGGameTutorial, EEGGame use this class public API to use EEG source and UXF library features
// - accepts messages from UI managers to change the state
// - communicates with api client: user data, games records data
public class GameManager : MonoBehaviour
{
    public enum AppState
    {
        Idle,
        TryLogin,
        Login,
        Logout,
        MainMenu,
        UserProfile,
        ProfileAnalysis,
        CoordinatorAnalysis,
        GameInfo,
        CoordinatorProfile,
        DeviceCheck,

        InGameTutorialSettings,
        InGameTutorial,
        InGameSettings,
        InGame,
        GameFinished,

        WaitChange,
    }

    // Local Data
    private const string identityFileName = "identity.txt";
    
    // Cache
    private UserIdentity currentUserIdentity;
    private AbstractEEGGame currentEEGGame = null;
    private EventLogger currentEEGGameEventLogger = null;

    // State
    private AppState appState = AppState.Idle;
    private AppState appStateChangeTo = AppState.Idle;

    // API Client
    APIClient apiclient;

    // UXF Session
    Session uxfSession = Session.instance;


    // Singleton
    private static GameManager instance = null;

#if EEG_DEBUG
    // EEGInfo Scene update
    private double updateEvery = 1; //seconds
    private double updateCounter = 0;
#endif


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
        
        appState = AppState.Idle;
        currentUserIdentity = new UserIdentity();
#endif

#if API_DEBUG
    apiclient = new APIClient(StubMode: true);
#else
    apiclient = new APIClient();
#endif

       UIManagerGameScene.GetInstance().UnloadAllScenes();
       RunStateMachine();
    }

    // Update is called once per frame
    void Update()
    {
#if EEG_DEBUG
        this.StreamEEGDataToEEGInfoScene();
#else

#endif

    }

    public void StateChangeRequest(AppState changeToState)
    {
        appStateChangeTo = changeToState;
    }

    private async void RunStateMachine()
    {
        while(true)
        {
            await Task.Delay(100);
            //Debug.Log("App state: " + appState.ToString());
            if (appStateChangeTo != AppState.Idle)
            {
                appState = appStateChangeTo;
                appStateChangeTo = AppState.Idle;
            }
            switch (appState)
            {
                case AppState.Idle:
                    {
                        appState = AppState.TryLogin;
                        //UIManagerGameScene.GetInstance();
                        break;
                    }
                case AppState.TryLogin:
                    {
                        UIManagerGameScene.GetInstance().TryAutoLogin();
                        UserIdentity result = await TryAutoLogin();
                        //result = default(UserIdentity);//TODO: comment
                        if (result.Equals(default(UserIdentity)))
                        {
                            appState = AppState.Login;
                        }
                        else
                        {
                            appState = AppState.MainMenu;
                        }
                        break;
                    }
                case AppState.Login:
                    {
                        UIManagerGameScene.GetInstance().Login();
                        appState = AppState.WaitChange;
                        break;
                    }
                case AppState.MainMenu:
                    {
                        this.currentEEGGame = null;
                        // TODO get indifivual info
                        try
                        {
                            UIManagerGameScene.GetInstance().MainMenu(currentUserIdentity, default(IndividualInfo));

                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            Debug.unityLogger.logEnabled = false;
                        }
                        appState = AppState.WaitChange;
                        break;
                    }
                case AppState.GameInfo:
                    {
                        UIManagerGameScene.GetInstance().GameInfo();
                        appState = AppState.WaitChange;
                        break;
                    }
                case AppState.InGameTutorialSettings:
                    {
                        UIManagerGameScene.GetInstance().InGameTutorialSettings();
                        this.currentEEGGame = UnityEngine.Object.FindFirstObjectByType<AbstractEEGGame>();
                        appState = AppState.WaitChange;
                        break;
                    }
                case AppState.InGameTutorial:
                    {
                        appState = AppState.WaitChange;
                        this.currentEEGGame.StartEEGGame(uxfSession, null);
                        break;
                    }
                case AppState.DeviceCheck:
                    {
                        UIManagerGameScene.GetInstance().DeviceCheck();
                        appState = AppState.WaitChange;
                        break;
                    }
                case AppState.InGameSettings:
                    {
                        UIManagerGameScene.GetInstance().InGameSettings();
                        this.currentEEGGame = UnityEngine.Object.FindFirstObjectByType<AbstractEEGGame>();
                        appState = AppState.WaitChange;
                        break;
                    }
                case AppState.InGame:
                    {
                        //TODO: Start recording EEG data here? 
                        // Nice place i think, then on finish - stop recording, worth a try
                        uxfSession.Begin(
                            GameManager.GetInstance().EEGGameIdToUnitySceneGameName(
                                UIManagerGameScene.GetInstance().mainMenuInfo.currentFocusedGameInfo.id
                            ),
                            currentUserIdentity.userId,
                            (int)(UnityEngine.Random.value*10e6),
                            null,
                            new Settings(currentEEGGame.GetCurrentGameSettings())
                        );
                        await Task.Yield();//wait next frame - this makes sure the session was initialized
                        this.currentEEGGameEventLogger = new EventLogger();
                        this.currentEEGGame.StartEEGGame(uxfSession, currentEEGGameEventLogger);
                        appState = AppState.WaitChange;
                        break;
                    }
                case AppState.GameFinished:
                    {
                        uxfSession.CurrentTrial.End();
                        this.currentEEGGameEventLogger.SaveToTrial(uxfSession.CurrentTrial);//save registered events
                        uxfSession.End();// noo need to wait 1 frame, flashes immidiately
                        UIManagerGameScene.GetInstance().GameFinished();
                        appState = AppState.WaitChange;
                        break;
                    }
                case AppState.WaitChange:
                    {
                        //do nothing
                        break;
                    }
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

    public string EEGGameIdToUnitySceneGameName(string id)
    {
        //TODO: make mapping
        return "ReactionTime";
    }

    private bool InitEEGSource()
    {
        return CrossPlatformEEGSourceFactory.GetInstance().InitEEGSource();
    }

    public bool StreamEEGSignal()
    {
        return CrossPlatformEEGSourceFactory.GetInstance().StartStreaming();
    }

    public bool StopEEGStream()
    {
        return CrossPlatformEEGSourceFactory.GetInstance().StopStreaming();
    }

    // Public API for game classes

    public bool CheckEEGDevice()
    {
        return InitEEGSource();
    }

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

    // Public API for UI managers


    //TODO: use this
    public void SaveRecordedEEGData()
    {
        GeneralUtilities.SaveEEGListAsTrialData(
            CrossPlatformEEGSourceFactory.GetInstance().GetPreciceRecordFilledBufferAsListAndClear(),
            this.uxfSession
        );            
    }

    // Looks for locally saved data: <auth token> etc.. and requests server for login
    // if token present + login requests successful returns UserIdentity
    // otherwise returns default structure
    public async Task<UserIdentity> TryAutoLogin()
    {
        // Try to find token
        var identity = LocalStorage.Load<UserIdentity>(identityFileName);
        if (string.IsNullOrEmpty(identity.token)) 
        {
            currentUserIdentity = default(UserIdentity);
            return default; 
        }
        var userIdentity = await apiclient.Login(identity.token);
        if (string.IsNullOrEmpty(userIdentity.userId) || identity.userId != userIdentity.userId) { return default; }
        // Save data
        LocalStorage.Save<UserIdentity>(identityFileName, userIdentity);
        currentUserIdentity = userIdentity;
        return userIdentity;

    }

    public async Task<bool> RequestLogin(string email)
    {
        return await apiclient.RequestLogin(email);
    }


    // In case of successfull login returns userId string, otherwise - null or ""
    public async Task<string> Login(string email, string code)
    {
        var userIdentity = await apiclient.Login(email, code);
        if (string.IsNullOrEmpty(userIdentity.userId) || string.IsNullOrEmpty(userIdentity.token)) 
        {
            currentUserIdentity = default(UserIdentity);
            return null; 
        }
        // save data
        LocalStorage.Save<UserIdentity>(identityFileName, userIdentity);
        currentUserIdentity = userIdentity;
        return userIdentity.userId;
    }

    // Requests registration
    // if request was successfull (true new user + no network errors) - returns true
    // otherwise - returns false
    //public async Task<bool> RequestRegisterUser(string email)
    //{
    //    return await apiclient.RequestRegister(email);
    //}

    // Requests actual user registration
    // Checks code from email, if wrong - returns null or ""
    // Saves <auth token>, setting up user data cache, returns userId string
    //public async Task<string> RegisterUser(string email, string code)
    //{
    //    var userIdentity = await apiclient.Register(email, code);
    //    if (string.IsNullOrEmpty(userIdentity.userId) || string.IsNullOrEmpty(userIdentity.token)) { return null;  }
    //    // save data
    //    LocalStorage.Save<UserIdentity>(identityFileName, userIdentity);
    //    return userIdentity.userId;
    //}

    public void Logout()
    {
        LocalStorage.Delete(identityFileName);
    }

    public async Task<IndividualInfo> RequestIndividualInfo()
    {
        return await apiclient.GetIndividualInfo(currentUserIdentity.userId);
    }

    public async Task<GeneralGameListInfo> RequestGeneralEEGGamesInfo()
    {
        return await apiclient.GetGeneralEEGGamesInfo();
    }
}
