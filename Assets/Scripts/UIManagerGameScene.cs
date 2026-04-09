using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        // Hide status
        TextMeshProUGUI statusText = GeneralUtilities.FindChildByName(this.canvas.transform, "LoginStatusText")?.GetComponent<TextMeshProUGUI>(); ;
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

    public void OnGetAccessButtonClick()
    {

    }

    // Main Menu Panel
}
