using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        Finish
    }

    public enum StimulusType
    {
        DefaultNostim,
        P300a,
        P300b,
        P1,
        N1
    }

    [Header("Settings")]
    [SerializeField] private float initialPauseDuration = 2f;
    [SerializeField] private float waitForReactionDuration = 2f;   // W seconds
    [SerializeField] private float pauseBetweenTrials = 2f;        // X seconds (pause between trials)
    [SerializeField] private int maxTrials = 20;                   // X total trials (counter >= X -> finish)
    [SerializeField] private int p300aOnlyThreshold = 5;           // counter < 5 => always P300

    [Header("Probabilities (counter > 5) — must sum to 1")]
    [SerializeField] private float p1Probability = 0.20f;
    [SerializeField] private float p300Probability = 0.75f;
    [SerializeField] private float n1Probability = 0.05f;

    [SerializeField] private float p300aProbability = 0.2f;
    [SerializeField] private float p300bProbability = 0.8f;

    [Header("Runtime Info (read-only)")]
    [SerializeField] private State currentState = State.Idle;
    [SerializeField] private StimulusType currentStimulus;
    [SerializeField] private int counter = 0;

    private bool tapped = false;

    public override void StartEEGGame(Session session, EventLogger eventLogger, bool tutorial = true)
    {
        base.StartEEGGame(session, eventLogger, tutorial);
    }

    protected override IEnumerator StartEEGGame()
    {
        yield return RunStateMachine();
    }

    protected override void FinishEEGGame()
    {
        base.FinishEEGGame();
        GameManager.FinishDataRecord();
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

    private void OnTap()
    {
        tapped = true;
    }

    // Core state machine (single while loop)
    private IEnumerator RunStateMachine()
    {
        Running = true;
        counter = 0;

        if(!IsTutorial)
        {
            GameManager.StartDataRecord();
        }

        // Idle
        SetState(State.Idle);
        OnIdle();
        yield return null; // one frame in 
        // Pause
        SetState(State.Pause);
        yield return new WaitForSeconds(initialPauseDuration);

        while (currentState != State.Finish)
        {
            // Stimulus
            currentStimulus = PickStimulus();
            SetState(State.Stimulus);
            OnStimulusShow(currentStimulus);
            yield return null;

            // WaitForReaction
            SetState(State.WaitForReaction);
            tapped = false;
            float stimulusDuration = 0f;

            while (stimulusDuration < waitForReactionDuration && !tapped)
            {
                stimulusDuration += Time.deltaTime;
                yield return null;
            }


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
                    eventLogger.LogEvent(GetEventLoggerStimulusType(currentStimulus), stimulusDuration, new Dictionary<string, string>
                    {
                        { "reacted_in_time", "no" }
                    });
                }
                //ReactTooLong
                SetState(State.ReactTooLong);
                OnReactTooLong();
            }

            yield return null; // one frame to display result

            // Increment counter
            counter++;

            if (counter >= maxTrials)
            {
                SetState(State.Finish);
                break;
            }

            // PauseXSeconds (Pause between trials)
            SetState(State.PauseXSeconds);
            yield return new WaitForSeconds(pauseBetweenTrials);
        }

        // Finish
        OnFinish();
        Running = false;
    }

    private StimulusType PickStimulus()
    {
        if (counter < p300aOnlyThreshold)
            return StimulusType.P300a;

        float roll = Random.value;

        if (roll < p1Probability)
        {
            return StimulusType.P1;
        }
        else if (roll < p1Probability + p300Probability)
        {
            float p3roll = Random.value;
            if(p3roll < p300aProbability)
            {
                return StimulusType.P300a;
            }
            else
            {
                return StimulusType.P300b;
            }
        }
        else
        {
            return StimulusType.N1;
        }
    }

    private string GetEventLoggerStimulusType(StimulusType s) => s switch
    {
        StimulusType.DefaultNostim => "<NO EVENT>",
        StimulusType.P300a => "ERP_P3A",
        StimulusType.P300b => "ERP_P3B",
        StimulusType.P1 => "ERP_P1",
        StimulusType.N1 => "ERP_N1"
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

        if(!IsTutorial)
        {
            Block block = this.uxfSession.CreateBlock(1);
            this.uxfSession.BeginNextTrial();

        }
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
        UIManagerReactionTime.GetInstance().ShowStimulus(StimulusType.DefaultNostim);
    }

    private void OnFinish()
    {
        if(!IsTutorial)
        {
            GameManager.FinishDataRecord();
            this.uxfSession.CurrentTrial.End();
        }
        Debug.Log($"[StateMachine] Finished after {counter} trials.");
        UIManagerReactionTime.GetInstance().GameFinished();
    }

    private void Start()
    {

    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnTap();
        }
    }

}