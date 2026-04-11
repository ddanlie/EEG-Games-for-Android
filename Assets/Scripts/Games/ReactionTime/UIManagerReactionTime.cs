using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManagerReactionTime : MonoBehaviour
{
    [Header("General Dependencies")]
    [SerializeField]
    public Canvas canvas;

    // Singleton
    private static UIManagerReactionTime instance = null;

    private void Awake()
    {
        if (UIManagerReactionTime.instance == null)
        {
            UIManagerReactionTime.instance = this;
            DontDestroyOnLoad(gameObject);

        }
        else
        {
            Destroy(gameObject); // destroy duplicate
        }
    }
    public static UIManagerReactionTime GetInstance()
    {
        if (UIManagerReactionTime.instance == null)
        {
            instance = FindObjectOfType<UIManagerReactionTime>();
        }
        return instance;
    }

    public void ShowPanel(string panelName)
    {
        foreach (Transform child in this.canvas.transform)
        {
            bool isTarget = child.name == panelName;
            child.gameObject.SetActive(isTarget);
        }
    }

    public void HidePanel(string panelName)
    {
        foreach (Transform child in this.canvas.transform)
        {
            bool isTarget = child.name == panelName;
            child.gameObject.SetActive(!isTarget);
        }
    }

    public void HideAllPanels()
    {
        foreach (Transform child in this.canvas.transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    private async void InGameSettings()
    {
        ShowPanel("GameSettingsPanel");
    }

    void Start()
    {
        InGameSettings();
    }

    void Update()
    {
        
    }

    public void OnReactionTimePlayButtonClick()
    {
        GameManager.GetInstance().StateChangeRequest(GameManager.AppState.InGame);
    }
}
