using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using System.Threading.Tasks;

// Main ui manager:
// - reacts to menu activities, packs messages for game manager
// - loads/unloads other games scenes
public class UIManagerGameScene : MonoBehaviour
{
    // General info/dependencies
    // Canvas to manipulate
    [Header("General Dependencies")]
    [SerializeField]
    public Canvas canvas;

    public struct GeneralUIInfo
    {
        public bool anonymousUser;
    }
    public struct MainMenuInfo
    {
        public GeneralGameInfo currentFocusedGameInfo;
    }
    public struct GameFinishedInfo
    {
        public string syncStatus;
    }

    public GeneralUIInfo generalUiInfo;
    public MainMenuInfo mainMenuInfo;
    public GameFinishedInfo gameFinishedInfo;

    [Header("Main Menu")]
    // individual info
    [SerializeField]
    private GameObject basicInfoScrollViewContent;
    [SerializeField]
    private GameObject basicInfoRowPrefab;
    // games list
    [SerializeField]
    private GameObject gameListScrollViewContent;
    [SerializeField]
    private GameObject gameListTitleRowPrefab;
    [SerializeField]
    private GameObject gameListRowButtonPrefab;

    private enum UIState
    {
        LoginRegisterPanel,
        AuthorizedMode,
        TestMode
    }

    // Singleton
    private static UIManagerGameScene instance = null;

    private void Awake()
    {
        if (UIManagerGameScene.instance == null)
        {
            UIManagerGameScene.instance = this;
            DontDestroyOnLoad(gameObject);

        }
        else
        {
            Destroy(gameObject); // destroy duplicate
        }
    }

    void Start()
    {
        CreatePanelsInfoContext();
    }

    void Update()
    {
        
    }

    public static UIManagerGameScene GetInstance()
    {
        if (UIManagerGameScene.instance == null)
        {
            instance = FindObjectOfType<UIManagerGameScene>();
        }
        return instance;
    }


    // API for game manager
    public void UnloadAllScenes()
    {
        int baseSceneIndex = SceneManager.GetActiveScene().buildIndex;

        // Loop through all loaded scenes
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.buildIndex != baseSceneIndex)
            {
                SceneManager.UnloadScene(scene);
            }
        }
    }

    public void LoadEEGInfoSceneAdditive()
    {
        this.UnloadAllScenes();
        SceneManager.LoadScene("EEGInfoScene", LoadSceneMode.Additive);
    }

    public void LoadEEGGameSceneAdditive(string sceneName)
    {
        this.UnloadAllScenes();
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }


    public void TryAutoLogin()
    {
        ShowPanel("TryLoginPanel");
    }

    public void Login()
    {
        ShowPanel("LoginPanel");

        Dictionary<string, GameObject> elements = GeneralUtilities.FindChildrenByNamesRecursive(this.canvas.transform, new List<string> 
        { 
            "LoginStatusText", "EmailInputField", "VerificationCodeInputField", "LoginButton" 
        });
        // Hide status
        TextMeshProUGUI statusText = elements["LoginStatusText"]?.GetComponent<TextMeshProUGUI>();
        statusText.text = "";
        statusText.enabled = false;
        // Blockk Login button, set text
        const string sendCodeString = "Send Code";
        const string loginString = "Log In";
        UnityEngine.UI.Button loginButon = elements["LoginButton"]?.GetComponent<UnityEngine.UI.Button>();
        loginButon.interactable = true;
        TextMeshProUGUI loginButtonText = loginButon.GetComponentInChildren<TextMeshProUGUI>();
        loginButtonText.text = sendCodeString;
        // Add input listeners
        TMP_InputField emailInput = elements["EmailInputField"]?.GetComponent<TMP_InputField>();
        TMP_InputField codeInput = elements["VerificationCodeInputField"]?.GetComponent<TMP_InputField>();
        codeInput.onEndEdit.AddListener(LoginPanelOnCodeInputValueEditvalue);
    }


    public async void MainMenu(UserIdentity currentUserIdentity)
    {
        // Check if anonymous user, show limited main menu if true
        if (currentUserIdentity.Equals(default(UserIdentity)))
        {
            this.generalUiInfo.anonymousUser = true;
            ShowPanel("LoadingDataPanel");
            GeneralGameListInfo gameList = await GameManager.GetInstance().RequestGeneralEEGGamesInfo();
            // TODO block profile, analysis and play buttons
            ShowPanel("AnonymousMainMenuPanel");
        }
        else
        {
            this.generalUiInfo.anonymousUser = false;

            ShowPanel("LoadingDataPanel");

            GeneralGameListInfo gameList;
            IndividualInfo currentIndividualInfo;
            Debug.Log("Entering while loop to request info...");
            while (true)
            {
                gameList = await GameManager.GetInstance().RequestGeneralEEGGamesInfo();
                currentIndividualInfo = await GameManager.GetInstance().RequestIndividualInfo();

                if(!gameList.Equals(default(GeneralGameListInfo)) && !currentIndividualInfo.Equals(default(IndividualInfo))) 
                {
                    break;
                }
            }
            Debug.Log("Info acquired");
            var sortedGames = APIClientUtils.GeneralGameListInfoSortBySubdomain(gameList);
            var individualInfoDict = APIClientUtils.IndividualInfoToDict(currentIndividualInfo);
            // clean content first
            foreach (Transform child in basicInfoScrollViewContent.transform) { Object.Destroy(child.gameObject); }
            // then show current individual info
            Debug.Log("Adding individual info...");
            foreach (var kvp in individualInfoDict)
            {
                var row = Instantiate(basicInfoRowPrefab, basicInfoScrollViewContent.transform);
                row.transform.SetAsLastSibling();
                Debug.Log("Row instantiated: " + row.ToString());

                TextMeshProUGUI propertyText = row.transform.Find("InfoRowProperty").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI valueText = row.transform.Find("InfoRowValue").GetComponent<TextMeshProUGUI>();

                Debug.Log("Setting row data");

                propertyText.text = kvp.Key;
                valueText.text = kvp.Value;
            }
            // clean game list
            foreach (Transform child in gameListScrollViewContent.transform) { Object.Destroy(child.gameObject); }
            // also find necessary ui elements
            Debug.Log("Looking for main menu UI elements");
            Dictionary<string, GameObject> elements = GeneralUtilities.FindChildrenByNamesRecursive(this.canvas.transform, new List<string>
            {
                "MainMenuGameDescriptionText", "MainMenuGameNameText", "MainMenuTutorialButton", "MainMenuPlayButton", "MainMenuInfoButton"
            });
            UnityEngine.UI.Button tutorialButton = elements["MainMenuTutorialButton"]?.GetComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.Button playButton = elements["MainMenuPlayButton"]?.GetComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.Button infoButton = elements["MainMenuInfoButton"]?.GetComponent<UnityEngine.UI.Button>();
            TextMeshProUGUI gameDescription = elements["MainMenuGameDescriptionText"]?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI gameName = elements["MainMenuGameNameText"]?.GetComponent<TextMeshProUGUI>();
            gameDescription.text = "";
            gameName.text = "";
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() =>
            {
                if (!mainMenuInfo.currentFocusedGameInfo.Equals(default(GeneralGameInfo)))
                    GameManager.GetInstance().StateChangeRequest(GameManager.AppState.DeviceCheck);
            });
            infoButton.onClick.RemoveAllListeners();
            infoButton.onClick.AddListener(() =>
            {
                if (!mainMenuInfo.currentFocusedGameInfo.Equals(default(GeneralGameInfo)))
                    GameManager.GetInstance().StateChangeRequest(GameManager.AppState.GameInfo);
            });
            tutorialButton.onClick.RemoveAllListeners();
            tutorialButton.onClick.AddListener(() =>
            {
                if (!mainMenuInfo.currentFocusedGameInfo.Equals(default(GeneralGameInfo)))
                    GameManager.GetInstance().StateChangeRequest(GameManager.AppState.InGameTutorialSettings);
            });
            // if some game is/was in focus - bind buttons for it
            if (!mainMenuInfo.currentFocusedGameInfo.Equals(default(GeneralGameInfo)))
            {
                // change description and name if some game is/was in focus
                gameName.text = this.mainMenuInfo.currentFocusedGameInfo.name; 
                gameDescription.text = this.mainMenuInfo.currentFocusedGameInfo.description;
            }
            // show current game list content
            Debug.Log("Adding game list data ");
            foreach (var gameListKVP in sortedGames)
            {
                var titlePrefab = Instantiate(gameListTitleRowPrefab, gameListScrollViewContent.transform);
                titlePrefab.transform.SetAsLastSibling();
                titlePrefab.transform.GetComponentInChildren<TextMeshProUGUI>().text = gameListKVP.Key; // set subdomain name
                foreach (var gameItemInfo in gameListKVP.Value.games) 
                {
                    var rowButton = Instantiate(gameListRowButtonPrefab, gameListScrollViewContent.transform).GetComponent<UnityEngine.UI.Button>();
                    rowButton.transform.SetAsLastSibling();
                    rowButton.GetComponentInChildren<TextMeshProUGUI>().text = gameItemInfo.name;
                    rowButton.onClick.RemoveAllListeners();
                    rowButton.onClick.AddListener(() => 
                    {
                        mainMenuInfo.currentFocusedGameInfo = gameItemInfo;
                        gameName.text = gameItemInfo.name;
                        gameDescription.text = gameItemInfo.description;
                    });
                }
            }
            //TODO: get profile, analysis etc buttons and bind

            ShowPanel("MainMenuPanel");

            //Hint:
            //basicInfoScrollViewContent - individual info content object
            //basicInfoRowPrefab         - individual info row with 2 texts - property name and value

            //gameListScrollViewContent  - game list content object
            //gameListTitleRowPrefab     - game list title text
            //gameListRowButtonPrefab    - game list button to get game info (several buttons under 1 title)

        }
    }
    
    public async void GameInfo()
    {
        if(this.mainMenuInfo.currentFocusedGameInfo.Equals(default(GeneralGameInfo)))
        {
            GameManager.GetInstance().StateChangeRequest(GameManager.AppState.MainMenu);
            return;
        }
        ShowPanel("GameInfoPanel");
    }

    public async void DeviceCheck()
    {
        Dictionary<string, GameObject> elements = GeneralUtilities.FindChildrenByNamesRecursive(this.canvas.transform, new List<string>
        {
            "DeviceCheckGameNameTitle", "DeviceCheckStatusStateText", "DeviceCheckTryConnectButton"
        });
        UnityEngine.UI.Button reconnectButton = elements["DeviceCheckTryConnectButton"]?.GetComponent<UnityEngine.UI.Button>();
        TextMeshProUGUI gameName = elements["DeviceCheckGameNameTitle"]?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI status = elements["DeviceCheckStatusStateText"]?.GetComponent<TextMeshProUGUI>();

        gameName.text = this.mainMenuInfo.currentFocusedGameInfo.name;
        status.text = "not connected";
        status.color = new Color(1f, 0.3f, 0.3f); // reddish


        reconnectButton.onClick.RemoveAllListeners();
        reconnectButton.onClick.AddListener(() =>
        {
            StartCoroutine(Reconnect());

            IEnumerator Reconnect()
            {
                status.text = "connecting...";
                status.color = new Color(0.3f, 0.3f, 1f);

                yield return null;

                bool deviceReady = GameManager.GetInstance().CheckEEGDevice();

                if (deviceReady)
                {
                    status.text = "connected, redirecting...";
                    status.color = new Color(0.3f, 1f, 0.3f);
                    GameManager.GetInstance().StateChangeRequest(GameManager.AppState.InGameSettings);
                }
                else
                {
                    status.text = "not connected";
                    status.color = new Color(1f, 0.3f, 0.3f);
                }
            }
        });

        ShowPanel("DeviceCheckPanel");
    }


    // Must load the scene, can't be async
    public void InGameTutorialSettings()
    {
        HideAllPanels();

        LoadEEGGameSceneAdditive(
            GameManager.GetInstance().EEGGameIdToUnitySceneGameName(
                mainMenuInfo.currentFocusedGameInfo.id
             )
        );
    }

    // Must load the scene, can't be async
    public void InGameSettings()
    {
        HideAllPanels();

        LoadEEGGameSceneAdditive(
            GameManager.GetInstance().EEGGameIdToUnitySceneGameName(
                mainMenuInfo.currentFocusedGameInfo.id
             )
        );
    }

    public void GameFinished()
    {
        UnloadAllScenes();//unload the additive game screen
        ShowPanel("GameFinishedPanel");

        Dictionary<string, GameObject> elements = 
            GeneralUtilities.FindChildrenByNamesRecursive(this.canvas.transform, new List<string>(new string[]
            {
                "GameFinishedSendingResultsPanel", "GameFinishedYesButton", "GameFinishedNoButton", "GameFinishedSendingResultsStatus"
            }));

        UnityEngine.UI.Button yesButton = elements["GameFinishedYesButton"].GetComponent<UnityEngine.UI.Button>();
        UnityEngine.UI.Button noButton = elements["GameFinishedNoButton"].GetComponent<UnityEngine.UI.Button>();
        GameObject sendingResultsPanel = elements["GameFinishedSendingResultsPanel"];
        TextMeshProUGUI sendStatus = elements["GameFinishedSendingResultsStatus"].GetComponent<TextMeshProUGUI>();
        sendingResultsPanel.SetActive( false );
        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(() =>
        {
            yesButton.interactable = false;

            StartCoroutine(SendData());

            IEnumerator SendData()
            {
                sendStatus.text = "Sending data, please wait...";
                sendStatus.color = new Color(0.3f, 0.3f, 1f);
                var task = GameManager.GetInstance().SendRecordedRunData();
                yield return new WaitUntil(() => task.IsCompleted);
                if (task.Exception != null)
                {
                    Debug.LogError(task.Exception);
                    sendStatus.color = new Color(1f, 0.3f, 0.3f);
                    sendStatus.text = "Something went wrong, try later";
                }
                else
                {
                    bool result = task.Result;
                    if (result)
                    {
                        sendStatus.text = "Success, redirecting to main menu...";
                        sendStatus.color = new Color(0.3f, 1f, 0.3f);
                        noButton.interactable = false;
                    }
                    else
                    {
                        sendStatus.color = new Color(1f, 0.3f, 0.3f);
                        sendStatus.text = "Request failed, try later";
                    }
                }
            }

        });
        noButton.onClick.RemoveAllListeners();
        noButton.onClick.AddListener(() =>
        {
            GameManager.GetInstance().StateChangeRequest(GameManager.AppState.MainMenu);
        });
    }

    public async void UserProfile()
    {
        ShowPanel("ProfileAnalysisPanel");
    }

    public async void ProfileAnalysis()
    {
        ShowPanel("UserProfilePanel");
    }


    // Private section
    private void ShowPanel(string panelName)
    {
        foreach (Transform child in this.canvas.transform)
        {
            bool isTarget = child.name == panelName;
            child.gameObject.SetActive(isTarget);
        }
    }

    private void HidePanel(string panelName)
    {
        foreach (Transform child in this.canvas.transform)
        {
            bool isTarget = child.name == panelName;
            child.gameObject.SetActive(!isTarget);
        }
    }

    private void HideAllPanels()
    {
        foreach (Transform child in this.canvas.transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    private GameObject FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child.gameObject;

            var result = FindChildByName(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    private void CreatePanelsInfoContext()
    {
        generalUiInfo = new GeneralUIInfo
        {
            anonymousUser = true
        };
        mainMenuInfo = new MainMenuInfo
        {
            currentFocusedGameInfo = default(GeneralGameInfo)
        };
        gameFinishedInfo = new GameFinishedInfo
        {
            syncStatus = ""
        };
    }

    // General
    public void OnWiFiButtonClick()
    {
        GeneralUtilities.OpenWifiPanel();
    }

    public void OnMenuButtonClick()
    {
        GameManager.GetInstance().StateChangeRequest(GameManager.AppState.MainMenu);
    }

    // Login Panel 
    public void LoginPanelOnCodeInputValueEditvalue(string value)
    {
        Dictionary<string, GameObject> elements = GeneralUtilities.FindChildrenByNamesRecursive(this.canvas.transform, new List<string>
        {
            "LoginButton"
        });
        UnityEngine.UI.Button loginButon = elements["LoginButton"]?.GetComponent<UnityEngine.UI.Button>();
        TextMeshProUGUI loginButtonText = loginButon.GetComponentInChildren<TextMeshProUGUI>();
        loginButtonText.text = string.IsNullOrEmpty(value) ? "Send Code" : "Log In";
    }
    public async void OnLoginButtonClick()
    {
        // Block and read input, wait for the login 
        Dictionary<string, GameObject> elements = GeneralUtilities.FindChildrenByNamesRecursive(this.canvas.transform, new List<string>
        {
            "EmailInputField", "VerificationCodeInputField", "LoginStatusText"
        });
        TMP_InputField emailInput = elements["EmailInputField"]?.GetComponent<TMP_InputField>();
        TMP_InputField codeInput = elements["VerificationCodeInputField"]?.GetComponent<TMP_InputField>();
        TextMeshProUGUI statusText = elements["LoginStatusText"]?.GetComponent<TextMeshProUGUI>();
        statusText.enabled = true;
        statusText.text = "";
        // Send code or login
        if (string.IsNullOrEmpty(codeInput.text))
        {
            // send code
            statusText.text = "Sending the access code...";
            if(await GameManager.GetInstance().RequestLogin(emailInput.text))
            {
                statusText.text = "Code was sent, please check your email.\nIn case you cannot find the code, try to send it again";
            }
            else
            {
                statusText.text = "Error, code wasn't sent.\nTry again";
            }
        }
        else
        {
            // login
            statusText.text = "Connecting to the server, please wait...";
            string result = await GameManager.GetInstance().Login(emailInput.text, codeInput.text);
            if(string.IsNullOrEmpty(result))
            {
                statusText.text = "Error, authentication failed\nTry again";
            }
            else 
            {
                statusText.text = "Successful authentication, redirecting to the app...";
                GameManager.GetInstance().StateChangeRequest(GameManager.AppState.MainMenu);
            }
        }
    }

    public void OnSkipButtonPressed()
    {
        // TODO: add anonymous user functionality
    }

    // Main Menu Panel
    public void OnLogoutButtonClick()
    {
        GameManager.GetInstance().Logout();
        GameManager.GetInstance().StateChangeRequest(GameManager.AppState.Login);
    }

    // Game Finished Panel
    public async void OnGameFinishedYesButtonClick()
    {

    }

    public async void OnGameFinishedNoButtonClicked()
    {

    }
}
