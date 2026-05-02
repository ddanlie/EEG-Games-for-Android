using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UXF;
using static GameManager;

public class EEGGameReactionTime : AbstractEEGGame
{
    public enum State
    {
        Idle,
        Pause,
        Stimulus,
        WaitForReaction,
        Registered,
        ReactTooLong,
        PauseXSeconds,
        FrequenstStimulusShow,
        Finish
    }

    public enum StimulusType
    {
        DefaultNostim,
        FrequentStim,
        P300b
    }

    [Header("Settings")]
    [SerializeField] private float initialPauseDuration = 2f;
    [SerializeField] private float frequentStimDuration = 0.6f;
    [SerializeField] private float waitForReactionDuration = 1.5f;   // W seconds
    [SerializeField] private float pauseBetweenTrialsMin = 2f;       // X seconds (pause between trials)
    [SerializeField] private float pauseBetweenTrialsMax = 4f;       // X seconds (pause between trials)
    [SerializeField] private int maxTrials = 10;                     // X total trials (counter >= X -> finish)
    [SerializeField] private int frequentStimOnlyThreshold = 5;             // counter < 5 => always P300

    [Header("Probabilities (counter > 5) — must sum to 1")]
    [SerializeField] private float frequentStimProbability = 0.9f;
    [SerializeField] private float p300bProbability = 0.1f;

    [Header("Runtime Info (read-only)")]
    [SerializeField] private State currentState = State.Idle;
    [SerializeField] private StimulusType currentStimulus;
    [SerializeField] private int counter = 0;

    private bool tapped = false;
    private double reactionTime = 0f;

    private InputControls input;


    private void OnEnable()
    {
        input = new InputControls();
        input.AndroidMap.Tap.started += OnTap;
        input.AndroidMap.Enable();
    }

    void OnDisable()
    {
        input.AndroidMap.Tap.started -= OnTap;
        input.AndroidMap.Disable();
    }

    public override void StartEEGGame(Session session, EventLogger eventLogger)
    {
        base.StartEEGGame(session, eventLogger);
    }

    protected override IEnumerator StartEEGGame()
    {
        yield return RunStateMachine();
    }

    protected override void FinishEEGGame()
    {
        base.FinishEEGGame();
    }

    public override void SetSetting(string settingName, string value)
    {
        //TODO: add settings changes
        return;
    }

    public override Dictionary<string, object> GetCurrentGameSettings()
    {
        //TODO: return settings
        return new Dictionary<string, object> { {"nothing", "nothing" } };
    }

    private void OnTap(InputAction.CallbackContext ctx)
    {
        if(currentState == State.WaitForReaction)
        {
            tapped = true;
            reactionTime = Time.realtimeSinceStartupAsDouble;
        }
    }



    // Core state machine (single while loop)
    private IEnumerator RunStateMachine()
    {
        counter = 0;

        if(!IsTutorial)
        {
            GameManager.GetInstance().StartDataRecord();
        }

        // Idle
        SetState(State.Idle);
        OnIdle();
        yield return new WaitForEndOfFrame();
        // Pause
        SetState(State.Pause);
        yield return new WaitForSeconds(initialPauseDuration);

        while (currentState != State.Finish)
        {
            // Stimulus
            currentStimulus = PickStimulus();
            SetState(State.Stimulus);
            OnStimulusShow(currentStimulus);
            yield return new WaitForEndOfFrame();

            // if P3b - WaitForReaction
            tapped = false;
            if (currentStimulus == StimulusType.P300b)
            {
                SetState(State.WaitForReaction);

                double startTime = Time.realtimeSinceStartupAsDouble;
                while (!tapped && (Time.realtimeSinceStartupAsDouble - startTime) < waitForReactionDuration)
                {
                    yield return null;
                }

                float stimulusDuration = (float)(reactionTime - startTime);


                if (tapped)
                {
                    if(!IsTutorial)
                    {
                        eventLogger.LogEvent(GetEventLoggerStimulusType(currentStimulus), stimulusDuration, new Dictionary<string, string>
                        {
                            { "reacted_in_time", "yes" }
                        });
                    }
                    // Registered
                    SetState(State.Registered);
                    OnRegistered(stimulusDuration);
                }
                else
                {
                    if (!IsTutorial)
                    {
                        eventLogger.LogEvent(GetEventLoggerStimulusType(currentStimulus), waitForReactionDuration, new Dictionary<string, string>
                        {
                            { "reacted_in_time", "no" }
                        });
                    }
                    //ReactTooLong
                    SetState(State.ReactTooLong);
                    OnReactTooLong();
                }
            }
            else
            {
                SetState(State.FrequenstStimulusShow);
                OnFrequentStimulusShow();
                yield return new WaitForSeconds(frequentStimDuration);
                OnFrequentStimulusFinish();
            }
            yield return new WaitForEndOfFrame(); // one frame to display result

            // Increment frequent stimulus threshold counter
            counter++;

            if (counter >= maxTrials)
            {
                SetState(State.Finish);
                break;
            }

            // PauseXSeconds (Pause between trials)
            SetState(State.PauseXSeconds);
            yield return new WaitForSeconds(UnityEngine.Random.Range(pauseBetweenTrialsMin, pauseBetweenTrialsMax));
        }

        // Finish
        OnFinish();
        Running = false;
    }

    private StimulusType PickStimulus()
    {
        if (counter < frequentStimOnlyThreshold)
        {
            return StimulusType.FrequentStim;
        }

        float roll = UnityEngine.Random.value;
        Debug.Log($"Stimulus Roll = {roll}");

        if (roll < frequentStimProbability)
        {
            return StimulusType.FrequentStim;
        }
        else
        {
            return StimulusType.P300b;
        }
    }

    private string GetEventLoggerStimulusType(StimulusType s) => 
        s switch
        {
            StimulusType.DefaultNostim => "<NO EVENT>",
            StimulusType.P300b => "ERP_P3B"
        };
    private void SetState(State newState)
    {
        currentState = newState;
        Debug.Log($"[StateMachine] State: {newState}  (counter={counter})");
    }

    private void OnIdle()
    {
        UIManagerReactionTime.GetInstance().StartState();
        UIManagerReactionTime.GetInstance().ShowStimulus(StimulusType.DefaultNostim);
    }

    private void OnStimulusShow(StimulusType stimulus)
    {
        Debug.Log($"[StateMachine] Showing stimulus: {stimulus}");
        UIManagerReactionTime.GetInstance().ShowStimulus(stimulus);
    }

    private void OnRegistered(float reactionTime)
    {
        Debug.Log($"[StateMachine] Tapped! Reaction time: {reactionTime:F3}s");
        UIManagerReactionTime.GetInstance().ShowStimulus(StimulusType.DefaultNostim);
    }

    private void OnReactTooLong()
    {
        Debug.Log("[StateMachine] No reaction — too slow!");
        UIManagerReactionTime.GetInstance().ShowTooLongFeedback();
        UIManagerReactionTime.GetInstance().ShowStimulus(StimulusType.DefaultNostim);
    }

    private void OnFrequentStimulusShow()
    {
        UIManagerReactionTime.GetInstance().ShowStimulus(StimulusType.FrequentStim);
    }

    private void OnFrequentStimulusFinish()
    {
        UIManagerReactionTime.GetInstance().ShowStimulus(StimulusType.DefaultNostim);
    }

    private void OnFinish()
    {
        if(!IsTutorial)
        {
            GameManager.GetInstance().FinishDataRecord();
        }
        Debug.Log($"[StateMachine] Finished after {counter} trials.");
        FinishEEGGame();
        UIManagerReactionTime.GetInstance().GameFinished();
    }

    private void Start()
    {

    }

    private void Update()
    {
        //if(Input.touchCount > 0) 
        //{
        //    Touch touch = Input.GetTouch(0);

        //    if (touch.phase == TouchPhase.Began)
        //    {
        //        OnTap();
        //    }
        //}
    }

}