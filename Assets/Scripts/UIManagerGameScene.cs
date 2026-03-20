using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


// Main ui manager:
// - reacts to menu activities, packs messages for game manager
// - loads/unloads other games scenes
public class UIManagerGameScene : MonoBehaviour
{
    private enum UIState
    {
        LoginRegisterPanel,
        AuthorizedMode,
        TestMode
    }
    //

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

    public void StartUI()
    {

    }


    // Private section

}
