using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManagerEEGInfoScene : MonoBehaviour
{
    [SerializeField]
    private Button startButton;
    [SerializeField]
    private TextMeshProUGUI dataText;

    // Singleton
    private static UIManagerEEGInfoScene instance = null;


    private enum UIState
    {
        Idle,
        Reading
    }

    private UIState state;


    private void Awake()
    {
        if (UIManagerEEGInfoScene.instance == null)
        {
            UIManagerEEGInfoScene.instance = this;
            DontDestroyOnLoad(gameObject);

            this.state = UIState.Idle;
        }
        else
        {
            Destroy(gameObject); // destroy duplicate
        }
    }
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static UIManagerEEGInfoScene GetInstance()
    {
        if(UIManagerEEGInfoScene.instance == null)
        {
            instance = FindObjectOfType<UIManagerEEGInfoScene>();
        }
        return instance;
    }


    public void OnStartButtonClick()
    {
        Debug.Log("Clicked");
        if (this.state == UIState.Idle)
        {
            this.startButton.interactable = false; 
            this.SetStartButtonText("Activating...");
            if(GameManager.GetInstance().StreamEEGSignal())
            {
                this.SetStartButtonText("Stop");
                this.state = UIState.Reading;
                this.dataText.SetText(this.dataText.text + "\nSTARTED");
            }
            else
            {
                this.SetStartButtonText("Start");
                this.dataText.SetText(this.dataText.text + "\nFAILED TO START");
            }
            this.startButton.interactable = true;
            return;
        }
        
        if(this.state == UIState.Reading) 
        {
            this.SetStartButtonText("Deactivating...");
            this.startButton.interactable = false;
            if(GameManager.GetInstance().StopEEGStream())
            {
                this.SetStartButtonText("Start");
                this.state = UIState.Idle;
                this.dataText.SetText(this.dataText.text + "\nSTOPPED");
            }
            else
            {
                this.SetStartButtonText("Stop");
                this.dataText.SetText(this.dataText.text + "\nFAILED TO STOP");
            }

            this.startButton.interactable = true;
            return;
        }
        
    }

    public void SetStartButtonText(string text)
    {
        this.startButton.GetComponentInChildren<TextMeshProUGUI>().SetText(text);
    }


    public void SetDataText(string text)
    {
        this.dataText.SetText(text);
    }
}
