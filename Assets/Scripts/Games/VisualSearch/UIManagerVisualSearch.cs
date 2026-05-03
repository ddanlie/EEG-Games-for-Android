using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static GameManager;

public class UIManagerVisualSearch : MonoBehaviour
{
    [Header("General Dependencies")]
    public Canvas canvas;

    // Singleton
    private static UIManagerVisualSearch instance = null;


    // Game elements

    [SerializeField] private GameObject targetImage;
    [SerializeField] private GameObject distractorImage1;
    [SerializeField] private GameObject distractorImage2;
    [SerializeField] private GameObject recoverAttentionImage1;
    [SerializeField] private GameObject recoverAttentionImage2;
    [SerializeField] private GameObject focusPointImage;
    [SerializeField] private GameObject stimuliGridPanel;
    [SerializeField] private TextMeshProUGUI presentTooLongFeedbackTitle;
    [SerializeField] private TextMeshProUGUI notPresentMissclickFeedbackTitle;

    private readonly int gridSideSize = 5;

    private void Awake()
    {
        if (UIManagerVisualSearch.instance == null)
        {
            UIManagerVisualSearch.instance = this;
        }
        else
        {
            Destroy(gameObject); // destroy duplicate
        }
    }

    public static UIManagerVisualSearch GetInstance()
    {
        if (UIManagerVisualSearch.instance == null)
        {
            instance = FindObjectOfType<UIManagerVisualSearch>();
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


    public void OnVisualSearchPlayButtonClick()
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
    public void StartState()
    {
        ShowPanel("GamePanel");
        targetImage.SetActive(false);
        distractorImage1.SetActive(false);
        distractorImage2.SetActive(false);
        recoverAttentionImage1.SetActive(false);
        recoverAttentionImage2.SetActive(false);
        focusPointImage.SetActive(false);
        stimuliGridPanel.SetActive(false);
        presentTooLongFeedbackTitle.enabled = false;
        notPresentMissclickFeedbackTitle.enabled = false;
    }

    public void ShowStimulus(int objectsAmount, bool showTarget)
    {
        int gridSize = gridSideSize * gridSideSize;
        // Choose 

        GameObject[] imgs = new []{ distractorImage1, distractorImage2 };

        int[] indiciesX = new int[gridSize];
        int[] indiciesY = new int[gridSize];
        // fill
        int idxCounter = 0;
        for (int i = 0; i < gridSideSize; i++)
        {
            for (int j = 0; j < gridSideSize; j++)
            {
                indiciesX[idxCounter] = j;
                indiciesY[idxCounter] = i;
                idxCounter++;
            }
        }
        // shuffle
        for (int i = gridSize - 1; i > 0; i--)
        {
            int jX = UnityEngine.Random.Range(0, i+1);
            (indiciesX[i], indiciesX[jX]) = (indiciesX[jX], indiciesX[i]);

            int jY = UnityEngine.Random.Range(0, i+1);
            (indiciesY[i], indiciesY[jY]) = (indiciesY[jY], indiciesY[i]);
        }
        // choose target index
        int idx = UnityEngine.Random.Range(0, objectsAmount);
        int targetX = -1;
        int targetY = -1;
        if (showTarget)
        {
            targetX = indiciesX[idx];
            targetY = indiciesY[idx];
        }

        int x, y;
        int stimuliTileSize = 60;
        int panelSize = 350;
        int startX = -(panelSize / 2) + (stimuliTileSize / 2);
        int startY = (panelSize / 2) - (stimuliTileSize / 2);
        GameObject imgToPlace;
        for(int i = 0; i < objectsAmount; i++)
        {
            x = indiciesX[i];
            y = indiciesY[i];
            Debug.Log($"Stim position {i}: ({x}, {y}) -> ({startX + x * stimuliTileSize}, {startY - y * stimuliTileSize})");
            if(x == targetX && y == targetY)
            {
                imgToPlace = Instantiate(targetImage, stimuliGridPanel.transform);
            }
            else
            {
                imgToPlace = Instantiate(imgs[Random.Range(0, imgs.Length)], stimuliGridPanel.transform);
            }
            imgToPlace.SetActive(true);
            imgToPlace.GetComponent<RectTransform>().localPosition = Vector3.zero;
            imgToPlace.GetComponent<RectTransform>().position = Vector3.zero;
            imgToPlace.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(startX + x * stimuliTileSize, startY - y * stimuliTileSize);
        }
        stimuliGridPanel.SetActive(true);
    }

    public void HideStimulus()
    {
        stimuliGridPanel.SetActive(false);
        foreach (Transform child in stimuliGridPanel.transform)
        {
            Object.Destroy(child.gameObject);
        }
    }

    public void FeedbackStimPresentTooLoing(float feedbackShowTimeSec)
    {
        presentTooLongFeedbackTitle.enabled = true;
        StartCoroutine(HideTextAfterDelay(presentTooLongFeedbackTitle, feedbackShowTimeSec));
    }

    public void FeedbackStimNotPresentMissclick(float feedbackShowTimeSec)
    {
        notPresentMissclickFeedbackTitle.enabled = true;
        StartCoroutine(HideTextAfterDelay(notPresentMissclickFeedbackTitle, feedbackShowTimeSec));
    }
    IEnumerator HideTextAfterDelay(TextMeshProUGUI text, float delaySec)
    {
        yield return new WaitForSeconds(delaySec);
        text.enabled = false;
    }

    public void RecoverShowNoise(float frame1TimeSec, float frame2TimeSec)
    {
        StartCoroutine(RecoverShowNoiseCoroutine(frame1TimeSec, frame2TimeSec));
    }

    private IEnumerator RecoverShowNoiseCoroutine(float t1, float t2)
    {
        recoverAttentionImage1.SetActive(true);
        yield return new WaitForSeconds(t1);
        recoverAttentionImage1.SetActive(false);
        recoverAttentionImage2.SetActive(true);
        yield return new WaitForSeconds(t2);
        recoverAttentionImage2.SetActive(false);
    }

    public void RecoverShowCentralPoint(float showTime)
    {
        StartCoroutine(RecoverShowCentralPointCoroutine(showTime));
    }

    private IEnumerator RecoverShowCentralPointCoroutine(float t)
    {
        focusPointImage.SetActive(true);
        yield return new WaitForSeconds(t);
        focusPointImage.SetActive(false);
    }


    public async void GameFinished()
    {
        HideAllPanels();
        GameManager.GetInstance().StateChangeRequest(AppState.GameFinished);
    }

}
