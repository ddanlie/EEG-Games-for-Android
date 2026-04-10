using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static System.Net.Mime.MediaTypeNames;
using Unity.VisualScripting;
using UnityEngine.UIElements;

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
    private struct GeneralUIInfo
    {
        public bool anonymousUser;
    }

    // Main menu info/dependencies
    private struct MainMenuInfo
    {

    }
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

    private GeneralUIInfo generalUiInfo;
    private MainMenuInfo mainMenuInfo;

    private enum UIState
    {
        LoginRegisterPanel,
        AuthorizedMode,
        TestMode
    }

    // Singleton
    private static UIManagerGameScene instance = null;

    //private GameManager gameManager;

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
        SceneManager.LoadScene("gameName", LoadSceneMode.Additive);
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
        // I just made button to be interactable all the time
        //emailInput.onValueChanged.AddListener(value =>
        //{
        //    loginButon.interactable = !string.IsNullOrEmpty(value);
        //});
        TMP_InputField codeInput = elements["VerificationCodeInputField"]?.GetComponent<TMP_InputField>();
        codeInput.onEndEdit.AddListener(value =>
        {
            loginButtonText.text = string.IsNullOrEmpty(value) ? sendCodeString : loginString;
        });


        //TextMeshProUGUI statusText = GeneralUtilities.FindChildByName(this.canvas.transform, "LoginStatusText")?.GetComponent<TextMeshProUGUI>();
        //statusText.enabled = false;
        //Button loginButon = GeneralUtilities.FindChildByName(this.canvas.transform, "LoginButton")?.GetComponent<Button>();
        //TMP_InputField emailInput = GeneralUtilities.FindChildByName(this.canvas.transform, "EmailInputField")?.GetComponent<TMP_InputField>();
        //TMP_InputField codeInput = GeneralUtilities.FindChildByName(this.canvas.transform, "EmailInputField")?.GetComponent<TMP_InputField>();
    }

    public async void MainMenu(UserIdentity currentUserIdentity, IndividualInfo individualInfo)
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
            // Get game list
            GeneralGameListInfo gameList = await GameManager.GetInstance().RequestGeneralEEGGamesInfo();
            // Get individual info
            IndividualInfo currentIndividualInfo = await GameManager.GetInstance().RequestIndividualInfo();
            // Show loaded data
            // individual info data
            var individualInfoDict = APIClientUtils.IndividualInfoToDict(currentIndividualInfo);
            // clean content first
            foreach (Transform child in basicInfoScrollViewContent.transform) { Object.Destroy(child.gameObject); }
            // then add current info
            foreach (var kvp in individualInfoDict)
            {
                var row = Instantiate(basicInfoRowPrefab, basicInfoScrollViewContent.transform);
                row.transform.SetAsLastSibling();

                TextMeshProUGUI propertyText = row.transform.Find("InfoRowProperty").GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI valueText = row.transform.Find("InfoRowValue").GetComponent<TextMeshProUGUI>();

                propertyText.text = kvp.Key;
                valueText.text = kvp.Value;
            }
            // game list data
            var sortedGames = APIClientUtils.GeneralGameListInfoSortBySubdomain(gameList);
            // clean game list
            foreach (Transform child in gameListScrollViewContent.transform) { Object.Destroy(child.gameObject); }
            // also find necessary ui elements
            Dictionary<string, GameObject> elements = GeneralUtilities.FindChildrenByNamesRecursive(this.canvas.transform, new List<string>
            {
                "GameDescriptionText", "TutorialButton", "PlayButton", "InfoButton"
            });
            TextMeshProUGUI gameDescription = elements["GameDescriptionText"]?.GetComponent<TextMeshProUGUI>();
            gameDescription.text = "";
            UnityEngine.UI.Button tutorialButton = elements["TutorialButton"]?.GetComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.Button playButton = elements["PlayButton"]?.GetComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.Button infoButton = elements["InfoButton"]?.GetComponent<UnityEngine.UI.Button>();
            // add current game list content
            foreach (var kvp in sortedGames)
            {
                var title = Instantiate(gameListTitleRowPrefab, gameListScrollViewContent.transform).GetComponent<TextMeshProUGUI>();
                title.transform.SetAsLastSibling();
                foreach (var gameItemInfo in kvp.Value.games) 
                {
                    var rowButton = Instantiate(gameListRowButtonPrefab, gameListScrollViewContent.transform).GetComponent<UnityEngine.UI.Button>();
                    rowButton.transform.SetAsLastSibling();
                    rowButton.onClick.RemoveAllListeners();
                    rowButton.onClick.AddListener(() => 
                    { 
                        //show game description    
                        gameDescription.text = gameItemInfo.description;
                        //rebind play, tutorial and info buttons
                        playButton.onClick.RemoveAllListeners();
                        playButton.onClick.AddListener(() =>
                        {

                        });
                        infoButton.onClick.RemoveAllListeners();
                        infoButton.onClick.AddListener(() =>
                        {

                        });
                        tutorialButton.onClick.RemoveAllListeners();
                        tutorialButton.onClick.AddListener(() =>
                        {

                        });
                    });
                }
            }
            //Hint:
            //basicInfoScrollViewContent - individual info content object
            //basicInfoRowPrefab         - individual info row with 2 texts - property name and value

            //gameListScrollViewContent  - game list content object
            //gameListTitleRowPrefab     - game list title text
            //gameListRowButtonPrefab    - game list button to get game info (several buttons under 1 title)

            ShowPanel("MainMenuPanel");
        }
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

        };
    }


    // Login Panel 

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

    public void OnLogoutButtonClick()
    {
        GameManager.GetInstance().Logout();
        GameManager.GetInstance().StateChangeRequest(GameManager.AppState.Login);
    }

    public void OnWiFiButtonClick()
    {
        GeneralUtilities.OpenWifiPanel();
    }

    public void OnSkipButtonPressed()
    {
        // TODO: add anonymous user functionality
    }

    // Main Menu Panel
}
