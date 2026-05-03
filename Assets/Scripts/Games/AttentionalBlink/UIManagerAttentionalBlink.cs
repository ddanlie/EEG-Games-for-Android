using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static GameManager;

public class UIManagerAttentionalBlink : MonoBehaviour
{
    [Header("General Dependencies")]
    public Canvas canvas;

    // Singleton
    private static UIManagerAttentionalBlink instance = null;

    [Header("UI")]
    [SerializeField] private GameObject targetImage; //Instantiate(prefab, new Vector3(x,y,z), Quaternion.identity, parentTransform); 
    [SerializeField] private GameObject distractorImage1;
    [SerializeField] private GameObject distractorImage2;
    [SerializeField] private GameObject distractorImage3;
    [SerializeField] private GameObject maskImage;
    [SerializeField] private GameObject x1Box;
    [SerializeField] private GameObject x2Box;
    [SerializeField] private GameObject y1Box;
    [SerializeField] private GameObject y2Box;
    [SerializeField] private TextMeshProUGUI correctFeedbackTitle;
    [SerializeField] private TextMeshProUGUI tooLongFeedbackTitle;
    [SerializeField] private TextMeshProUGUI mistakeFeedbackTitle;
    [SerializeField] private TextMeshProUGUI pressWhenReadyTitle;

    [HeaderAttribute("Input")]
    [SerializeField] private GameObject yesPanel;
    [SerializeField] private GameObject noPanel;
    [SerializeField] private EEGGameAttentionalBlink gameInstance;


    private void Awake()
    {
        if (UIManagerAttentionalBlink.instance == null)
        {
            UIManagerAttentionalBlink.instance = this;
        }
        else
        {
            Destroy(gameObject); // destroy duplicate
        }


        // yes panel
        EventTrigger yesTrigger = yesPanel.AddComponent<EventTrigger>();
        EventTrigger.Entry yesEntry = new EventTrigger.Entry();
        yesEntry.eventID = EventTriggerType.PointerClick;
        yesEntry.callback.AddListener(_ => gameInstance.OnYesButtonClick());
        yesTrigger.triggers.Add(yesEntry);

        // no panel
        EventTrigger noTrigger = noPanel.AddComponent<EventTrigger>();
        EventTrigger.Entry noEntry = new EventTrigger.Entry();
        noEntry.eventID = EventTriggerType.PointerClick;
        noEntry.callback.AddListener(_ => gameInstance.OnNoButtonclick());
        noTrigger.triggers.Add(noEntry);
    }

    public static UIManagerAttentionalBlink GetInstance()
    {
        if (UIManagerAttentionalBlink.instance == null)
        {
            instance = FindObjectOfType<UIManagerAttentionalBlink>();
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


    public void OnAttentionalBlinkPlayButtonClick()
    {
        if (GameManager.GetInstance().currentEEGGame == null)
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

    // GAME API

    public void ClearScreen()
    {
        targetImage.SetActive(false);
        distractorImage1.SetActive(false);
        distractorImage2.SetActive(false);
        distractorImage3.SetActive(false);
        maskImage.SetActive(false);
        tooLongFeedbackTitle.enabled = false;
        mistakeFeedbackTitle.enabled = false;
        pressWhenReadyTitle.enabled = false;
        correctFeedbackTitle.enabled = false;

        //destroy boxes children
        GameObject[] gameObjects = new[] { x1Box, x2Box, y1Box, y2Box }; 
        foreach (GameObject box in gameObjects)
        {
            foreach (Transform child in box.transform)
            {
                Object.Destroy(child.gameObject);
            }
        }
    }

    public void StartState()
    {
        ShowPanel("GamePanel");
        ClearScreen();
    }

    public void WaitForTap()
    {
        ClearScreen();
        pressWhenReadyTitle.enabled = true;
    }

    //s1position: 0 - y up, 1 - x right, 2 - y down, 3 - x left
    public void ShowStim1(bool isTargetPresent, int s1position, float s11LengthSec)
    {
        ClearScreen();
        GameObject stim = targetImage;
        if(!isTargetPresent) 
        {
            GameObject[] distractors = new [] { distractorImage1, distractorImage2, distractorImage3 };
            stim = distractors[Random.Range(0, distractors.Length)];
        }
        GameObject stimPosition = getPositionGameObject(s1position);


        int stimBoxSize = 100;
        int stimImgSize = 70;
        int stimMaskSize = 90;

        int startXs = -(stimBoxSize / 2) + (stimBoxSize - stimImgSize) / 2;
        int startYs = (stimBoxSize / 2) - (stimBoxSize - stimImgSize) / 2;

        int startXm = -(stimBoxSize / 2) + (stimBoxSize - stimMaskSize) / 2;
        int startYm = (stimBoxSize / 2) - (stimBoxSize - stimMaskSize) / 2;

        GameObject stimToShow = Instantiate(stim, stimPosition.transform);
        //stimToShow.GetComponent<RectTransform>().localPosition = Vector3.zero;
        //stimToShow.GetComponent<RectTransform>().position = Vector3.zero;
        stimToShow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

        GameObject maskToShow = Instantiate(maskImage, stimPosition.transform);
        //maskToShow.GetComponent<RectTransform>().localPosition = Vector3.zero;
        //maskToShow.GetComponent<RectTransform>().position = Vector3.zero;
        maskToShow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

        StartCoroutine(ShowStim1HelpCoroutine(stimToShow, maskToShow, s11LengthSec));
    }

    private IEnumerator ShowStim1HelpCoroutine(GameObject stimToShow, GameObject mask, float s11LengthSec)
    {
        mask.SetActive(false);
        stimToShow.SetActive(true);
        yield return new WaitForSeconds(s11LengthSec);
        mask.SetActive(true);
        stimToShow.SetActive(false);
    }

    public void ShowStim2(bool isTargetPresent, int s2position, float s21LengthSec)
    {
        ClearScreen();
        GameObject stim = targetImage;
        if (!isTargetPresent)
        {
            GameObject[] distractors = new[] { distractorImage1, distractorImage2, distractorImage3 };
            stim = distractors[Random.Range(0, distractors.Length)];
        }
        GameObject stimPosition = getPositionGameObject(s2position);

        int stimBoxSize = 100;
        int stimImgSize = 70;
        int stimMaskSize = 90;

        int startXs = -(stimBoxSize / 2) + (stimBoxSize - stimImgSize) / 2;
        int startYs = (stimBoxSize / 2) - (stimBoxSize - stimImgSize) / 2;

        int startXm = -(stimBoxSize / 2) + (stimBoxSize - stimMaskSize) / 2;
        int startYm = (stimBoxSize / 2) - (stimBoxSize - stimMaskSize) / 2;

        GameObject stimToShow = Instantiate(stim, stimPosition.transform);
        //stimToShow.GetComponent<RectTransform>().localPosition = Vector3.zero;
        //stimToShow.GetComponent<RectTransform>().position = Vector3.zero;
        stimToShow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

        GameObject maskToShow = Instantiate(maskImage, stimPosition.transform);
        //maskToShow.GetComponent<RectTransform>().localPosition = Vector3.zero;
        //maskToShow.GetComponent<RectTransform>().position = Vector3.zero;
        maskToShow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

        StartCoroutine(ShowStim2HelpCoroutine(stimToShow, maskToShow, s21LengthSec));
    }

    private IEnumerator ShowStim2HelpCoroutine(GameObject stimToShow, GameObject mask, float s21LengthSec)
    {
        mask.SetActive(false);
        stimToShow.SetActive(true);
        yield return new WaitForSeconds(s21LengthSec);
        mask.SetActive(true);
        stimToShow.SetActive(false);
    }

    public void RegisteredCorrect()
    {
        ClearScreen();
        correctFeedbackTitle.enabled = true;
    }

    public void RegisteredMistake()
    {
        ClearScreen();
        mistakeFeedbackTitle.enabled = true;
    }

    public void RegisterTooLong()
    {
        ClearScreen();
        tooLongFeedbackTitle.enabled = true;
    }

    private GameObject getPositionGameObject(int position)
    {
        switch (position)
        {
            case 0:
                {
                    return y1Box;
                    break;
                }
            case 1: 
                {
                    return x2Box;
                    break;
                }
            case 2:
                {
                    return y2Box;
                    break;
                }
            case 3:
                {
                    return x1Box;
                    break;
                }
        }
        return null;
    }

    public async void GameFinished()
    {
        HideAllPanels();
        GameManager.GetInstance().StateChangeRequest(AppState.GameFinished);
    }

    void Start()
    {
        InGameSettings();
    }

    void Update()
    {
        
    }
}
