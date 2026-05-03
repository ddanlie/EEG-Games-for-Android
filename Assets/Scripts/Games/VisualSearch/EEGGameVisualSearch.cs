using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UXF;

public class EEGGameVisualSearch : AbstractEEGGame
{
    public enum State
    {
        Idle,
        InitialPause,
        ShowStimulusAbsent,
        ShowStimulusPresent,
        WaitForReaction,
        RecoverAttentionShowNoise,
        RecoverAttentionShowCentralPoint,
        Registered,
        RegisteredMistake,
        ReactTooLong,
        Finish
    }

    private readonly string gameStimulusName = "ERP_N1";

    [Header("Settings")]
    [SerializeField] private int TobjectsPerLevel = 10;
    [SerializeField] private float initialPauseDuration = 2f;
    [SerializeField] private float waitForReactionDuration = 4f;//seconds // for both - present and absent stimuli
    [SerializeField] private int maxTrials = 10;

    [Header("Probabilities")]
    [SerializeField] private float presentTargetProbability = 0.5f;

    [Header("Runtime Info (read-only)")]
    [SerializeField] private State currentState = State.Idle;
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
        return new Dictionary<string, object> { { "nothing", "nothing" } };
    }

    private void OnTap(InputAction.CallbackContext ctx)
    {
        if (currentState == State.WaitForReaction)
        {
            tapped = true;
            reactionTime = Time.realtimeSinceStartupAsDouble;
        }
    }


    private IEnumerator RunStateMachine()
    {
        counter = 0;

        if (!IsTutorial)
        {
            GameManager.GetInstance().StartDataRecord();
        }

        // Idle
        SetState(State.Idle);
        OnIdle();
        yield return new WaitForEndOfFrame();
        // Pause
        SetState(State.InitialPause);
        yield return new WaitForSeconds(initialPauseDuration);

        while (currentState != State.Finish)
        {
            tapped = false;
            bool present = DecideIfTargetIsPresent();
            int maxObjectsLevel = Random.Range(1, 5+1) * 5;//5 - 25 objects
            int objectsAmount = Random.Range(2, maxObjectsLevel+1);
            if (present)
            {
                SetState(State.ShowStimulusPresent);
                OnShowStimulus(objectsAmount, present);
                yield return new WaitForEndOfFrame();
                SetState(State.WaitForReaction);

                double startTime = Time.realtimeSinceStartupAsDouble;
                while (!tapped && (Time.realtimeSinceStartupAsDouble - startTime) < waitForReactionDuration)
                {
                    yield return null;
                }

                float stimulusDuration = (float)(reactionTime - startTime);

                if (tapped)
                {
                    if (!IsTutorial)
                    {
                        eventLogger.LogEvent(gameStimulusName, stimulusDuration, new Dictionary<string, string>
                        {
                            { "status", "correct"},//correct|error|too_slow
                            { "target_is_present", "yes" },
                            { "number_of_distractors", objectsAmount.ToString() }
                        });
                    }
                    // Registered
                    SetState(State.Registered);
                    OnRegistered();
                }
                else
                {
                    if (!IsTutorial)
                    {
                        eventLogger.LogEvent(gameStimulusName, waitForReactionDuration, new Dictionary<string, string>
                        {
                            { "status", "too_slow"},
                            { "target_is_present", "yes" },
                            { "number_of_distractors", objectsAmount.ToString() }
                        });
                    }
                    //ReactTooLong
                    SetState(State.ReactTooLong);
                    OnReactTooLong(1);
                    yield return new WaitForSeconds(1+0.1f);
                }
            }
            else
            {
                SetState(State.ShowStimulusAbsent);
                OnShowStimulus(objectsAmount, present);
                yield return new WaitForEndOfFrame();
                SetState(State.WaitForReaction);
                double startTime = Time.realtimeSinceStartupAsDouble;
                while (!tapped && (Time.realtimeSinceStartupAsDouble - startTime) < waitForReactionDuration)
                {
                    yield return null;
                }
                float stimulusDuration = (float)(reactionTime - startTime);

                if (tapped)
                {
                    if (!IsTutorial)
                    {
                        eventLogger.LogEvent(gameStimulusName, stimulusDuration, new Dictionary<string, string>
                        {
                            { "status", "error"},
                            { "target_is_present", "no" },
                            { "number_of_distractors", objectsAmount.ToString() }
                        });
                    }
                    SetState(State.RegisteredMistake);
                    OnRegisteredMistake(1);
                    yield return new WaitForSeconds(1+0.1f);
                }
                else
                {
                    if (!IsTutorial)
                    {
                        eventLogger.LogEvent(gameStimulusName, waitForReactionDuration, new Dictionary<string, string>
                        {
                            { "status", "correct" },
                            { "target_is_present", "no" },
                            { "number_of_distractors", objectsAmount.ToString() }
                        });
                    }
                    SetState(State.Registered);
                    OnRegistered();
                }

            }
            yield return new WaitForEndOfFrame();

            counter++;

            if (counter >= maxTrials)
            {
                SetState(State.Finish);
                break;
            }

            SetState(State.RecoverAttentionShowNoise);
            OnRrecoverAttentionShowNoise(0.2f, 0.2f);
            yield return new WaitForSeconds(0.2f+0.2f+0.1f);
            SetState(State.RecoverAttentionShowCentralPoint);
            OnRrecoverAttentionShowCentralPoint(0.2f);
            yield return new WaitForSeconds(0.2f+0.1f);
        }

        OnFinish();
        Running = false;
    }

    private bool DecideIfTargetIsPresent()
    {
        float roll = UnityEngine.Random.value;

        if (roll < presentTargetProbability)
        {
            return true;
        }
        else
        {
            return false;
        }

    }

    private void OnIdle()
    {
        UIManagerVisualSearch.GetInstance().StartState();
    }

    private void OnShowStimulus(int objectsAmount, bool showTarget)
    {
        UIManagerVisualSearch.GetInstance().ShowStimulus(objectsAmount, showTarget);
    }
    
    private void OnRegistered()
    {
        UIManagerVisualSearch.GetInstance().HideStimulus();
    }

    private void OnReactTooLong(float feedbackShowTimeSec)
    {
        UIManagerVisualSearch.GetInstance().FeedbackStimPresentTooLoing(feedbackShowTimeSec);//FeedbackStimNotPresentMissclick
        UIManagerVisualSearch.GetInstance().HideStimulus();
    }

    private void OnRegisteredMistake(float feedbackShowTimeSec)
    {
        UIManagerVisualSearch.GetInstance().FeedbackStimNotPresentMissclick(feedbackShowTimeSec);
        UIManagerVisualSearch.GetInstance().HideStimulus();
    }

    private void OnRrecoverAttentionShowNoise(float firstFrameTimeSec, float secondFrameTimeSec)
    {
        UIManagerVisualSearch.GetInstance().RecoverShowNoise(firstFrameTimeSec, secondFrameTimeSec);
    }

    private void OnRrecoverAttentionShowCentralPoint(float pointShowTimeSec)
    {
        UIManagerVisualSearch.GetInstance().RecoverShowCentralPoint(pointShowTimeSec);
    }

    private void OnFinish()
    {
        if (!IsTutorial)
        {
            GameManager.GetInstance().FinishDataRecord();
        }
        Debug.Log($"[StateMachine] Finished after {counter} trials.");
        FinishEEGGame();
        UIManagerVisualSearch.GetInstance().GameFinished();
    }

    private void SetState(State newState)
    {
        currentState = newState;
        Debug.Log($"[StateMachine] State: {newState}");
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        
    }
}
