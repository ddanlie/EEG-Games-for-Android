using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static System.Net.Mime.MediaTypeNames;

// Main ui manager:
// - reacts to menu activities, packs messages for game manager
// - loads/unloads other games scenes
public class UIManagerGameScene : MonoBehaviour
{
    private struct GeneralUIInfo
    {
        public bool anonymousUser;
    }
    private struct MainMenuInfo
    {

    }
    private GeneralUIInfo generalUiInfo;
    private MainMenuInfo mainMenuInfo;

    private enum UIState
    {
        LoginRegisterPanel,
        AuthorizedMode,
        TestMode
    }

    // Canvas to manipulate
    [SerializeField]
    public Canvas canvas;

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
        Button loginButon = elements["LoginButton"]?.GetComponent<Button>();
        loginButon.interactable = false;
        TextMeshProUGUI loginButtonText = loginButon.GetComponentInChildren<TextMeshProUGUI>();
        loginButtonText.text = sendCodeString;
        // Add input listeners
        TMP_InputField emailInput = elements["EmailInputField"]?.GetComponent<TMP_InputField>();
        emailInput.onValueChanged.AddListener(value =>
        {
            loginButon.interactable = !string.IsNullOrEmpty(value);
        });
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

    public void MainMenu(UserIdentity currentUserIdentity)
    {
        // Check if anonymous user, show limited main menu if true
        if (currentUserIdentity.Equals(default(UserIdentity)))
        {
            this.generalUiInfo.anonymousUser = true;
            ShowPanel("MainMenuPanel");
        }
        else
        {
            this.generalUiInfo.anonymousUser = false;
            ShowPanel("AnonymousMainMenuPanel");
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

    public void OnSkipButtonPressed()
    {
        // TODO: add anonymous user functionality
    }

    // Main Menu Panel
}
