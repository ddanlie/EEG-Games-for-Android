using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using static GameManager;

public class UIManagerReactionTime : MonoBehaviour
{
    [Header("General Dependencies")]
    public Canvas canvas;

    // Singleton
    private static UIManagerReactionTime instance = null;

    // Game elements
    [SerializeField] private GameObject  noStimulusPanel;
    [SerializeField] private GameObject  defaultStimulusPanel;
    [SerializeField] private GameObject  P3bStimulusImage;
    [SerializeField] private TextMeshProUGUI tooLongFeedbackTitle;

    private void Awake()
    {
        if (UIManagerReactionTime.instance == null)
        {
            UIManagerReactionTime.instance = this;
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
        if(GameManager.GetInstance().currentEEGGame == null)
        {
            return;
        }

        if (GameManager.GetInstance().currentEEGGame.IsTutorial)
        {
            GameManager.GetInstance().StateChangeRequest(GameManager.AppState.InGameTutorial);
        }
        else
        {
            GameManager.GetInstance().StateChangeRequest(GameManager.AppState.InGame);
        }
    }


    // Reaction Time UI API

    // shows empty square in the middle
    public void StartState()
    {
        ShowPanel("GamePanel");
        noStimulusPanel.SetActive(true);
        defaultStimulusPanel.SetActive(false);
        P3bStimulusImage.SetActive(false);
        tooLongFeedbackTitle.enabled = false;
    }

    // Stimuli names: "p3a", "p3b", "default_nostim", "p1", "n1"
    public void ShowStimulus(EEGGameReactionTime.StimulusType stimulus)
    {
        bool frequentStim = stimulus == EEGGameReactionTime.StimulusType.FrequentStim;
        bool p3b = stimulus == EEGGameReactionTime.StimulusType.P300b;
        //bool defaultNoStim = stimulus == EEGGameReactionTime.StimulusType.DefaultNostim;

        defaultStimulusPanel.SetActive(frequentStim);
        P3bStimulusImage.SetActive(p3b);
    }

    public void ShowTooLongFeedback()
    {
        tooLongFeedbackTitle.enabled = true;
        StartCoroutine(HideFeedbackAfterDelay());
    }
    private IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(0.4f);
        tooLongFeedbackTitle.enabled = false;
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
