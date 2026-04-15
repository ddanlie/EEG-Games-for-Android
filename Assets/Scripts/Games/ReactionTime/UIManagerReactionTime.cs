using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;

public class UIManagerReactionTime : MonoBehaviour
{
    [Header("General Dependencies")]
    [SerializeField]
    public Canvas canvas;

    // Singleton
    private static UIManagerReactionTime instance = null;

    // Game elements
    [SerializeField] private GameObject defaultStimulusPanel;
    [SerializeField] private GameObject defaultStimulusP3aImage;
    [SerializeField] private GameObject defaultStimulusP3bImage;
    [SerializeField] private GameObject p1StimulusPanel;
    [SerializeField] private GameObject n1StimulusPanel;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip N1Sound;


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


    // Reaction Time UI API

    // shows empty square in the middle
    public void StartState()
    {
        ShowPanel("GamePanel");
        defaultStimulusPanel.SetActive(true);
        defaultStimulusP3aImage.SetActive(false);
        defaultStimulusP3bImage.SetActive(false);

        p1StimulusPanel.SetActive(false);
        n1StimulusPanel.SetActive(false);
    }

    // Stimuli names: "p3a", "p3b", "default_nostim", "p1", "n1"
    public void ShowStimulus(EEGGameReactionTime.StimulusType stimulus)
    {
        bool p3a = stimulus == EEGGameReactionTime.StimulusType.P300a;
        bool p3b = stimulus == EEGGameReactionTime.StimulusType.P300b;
        bool defaultNostim = stimulus == EEGGameReactionTime.StimulusType.DefaultNostim;
        bool p1 = stimulus == EEGGameReactionTime.StimulusType.P1;
        bool n1 = stimulus == EEGGameReactionTime.StimulusType.N1;

        defaultStimulusPanel.SetActive(defaultNostim || p3a || p3b);
        defaultStimulusP3aImage.SetActive(p3a);
        defaultStimulusP3bImage.SetActive(p3b);

        p1StimulusPanel.SetActive(p1);
        n1StimulusPanel.SetActive(n1);
    }

    public async void GameFinished()
    {
        HideAllPanels();
        GameManager.GetInstance().StateChangeRequest(AppState.GameFinished);
    }


    void OnApplicationPause(bool pause)
    {
        GameManager.GetInstance().StateChangeRequest(AppState.GameFinished);
    }
    void OnApplicationQuit()
    {
        GameManager.GetInstance().StateChangeRequest(AppState.GameFinished);
    }
}
