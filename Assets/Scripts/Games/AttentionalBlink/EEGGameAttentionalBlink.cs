using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UXF;
using static EEGGameReactionTime;

public class EEGGameAttentionalBlink : AbstractEEGGame
{
    public enum State
    {
        Idle,
        WaitUntilReady,
        Stimulus1,
        Stimulus2,
        WaitForReaction,
        Registered,
        ReactTooLong,
        RegisteredMistake,
        Finish
    }


    private readonly string p2firstName = "ERP_P2";
    private readonly string p2secondName = "ERP_P2_2";


    [Header("Settings")]
    [SerializeField] private float stim1ToStim2MaxDelayMs = 400f; // 100ms step 
    [SerializeField] private float waitForReactionDurationSec = 5f;   // W seconds
    [SerializeField] private int trialsPerIteration = 3;

    [Header("Probabilities")]
    [SerializeField] private float targetPresentFirstProbability = 0.3f;
    [SerializeField] private float targetPresentSecondProbability = 0.3f;
    [SerializeField] private float targetNotPresentProbability = 0.3f;

    [Header("Runtime Info (read-only)")]
    [SerializeField] private State currentState = State.Idle;
    [SerializeField] private StimulusType currentStimulus;
    [SerializeField] private int trialCounter = 0;


    private bool tapped = false;
    private bool yesClicked = false;
    private bool noClicked = false;
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


    public void OnYesButtonClick()
    {
        reactionTime = Time.realtimeSinceStartupAsDouble;
        yesClicked = true;
    }

    public void OnNoButtonclick()
    {
        reactionTime = Time.realtimeSinceStartupAsDouble;
        noClicked = true;
    }



    private void OnTap(InputAction.CallbackContext ctx)
    {
        if (currentState == State.WaitUntilReady)
        {
            tapped = true;
        }
    }

    private IEnumerator RunStateMachine()
    {
        trialCounter = 0;
        float s11LengthSec = 0.1f; //time of a part 1 stimulus
        float s21LengthSec = 0.1f;

        if (!IsTutorial)
        {
            GameManager.GetInstance().StartDataRecord();
        }
        SetState(State.Idle);
        OnIdle();
        for (float stimDelay = stim1ToStim2MaxDelayMs; stimDelay >= 100; stimDelay -= 100)
        {
            for(int trialNum = 0; trialNum < trialsPerIteration; trialNum++) 
            {
                trialCounter++;
                int targetVariant = ChooseTargetVariant();// 0 - not present, 1 - present first, 2 - present second
                int s1position = -1;
                int s2position = s1position;
                while(s1position == s2position)//acceptable delay
                {
                    s1position = Random.Range(0,4);// 0 - y up, 1 - x right, 2 - y down, 3 - x left
                    s2position = Random.Range(0,4);
                }
                tapped = false;
                SetState(State.WaitUntilReady);
                OnWaitUntilReady();
                yield return new WaitUntil(() => tapped);
                OnWaitUntilReadyTapped();
                yield return new WaitForSeconds(1);
                SetState(State.Stimulus1);
                OnStimulus1(targetVariant == 1, s1position, s11LengthSec);
                yield return new WaitForSeconds(stimDelay/1000+s11LengthSec);
                SetState(State.Stimulus2);
                OnStimulus2(targetVariant == 2, s2position, s21LengthSec);
                double startTime = Time.realtimeSinceStartupAsDouble;//order is important!
                yield return new WaitForSeconds(s21LengthSec);
                SetState(State.WaitForReaction);
                yesClicked = false;
                noClicked = false;
                while (!yesClicked && !noClicked && (Time.realtimeSinceStartupAsDouble - startTime) < waitForReactionDurationSec)
                {
                    Debug.Log($"times: {Time.realtimeSinceStartupAsDouble - startTime} < {waitForReactionDurationSec}");
                    yield return null;
                }
                float stimulus2Duration = waitForReactionDurationSec;
                if (yesClicked || noClicked) 
                {
                    stimulus2Duration = (float)(reactionTime - startTime);//seconds
                }
                float stimulus1Duration = stimDelay/1000 ;//seconds //not used in analysis, so actual value doesn't matter
                bool targetPresent = (targetVariant == 1 || targetVariant == 2);
                int timeCoef = targetVariant == 1 ? -1 : 1;// if target is first - time should be negative
                if (yesClicked)
                {
                    if(targetPresent)
                    {
                        if(!IsTutorial)
                        {
                            //1st P2
                            eventLogger.LogEvent(p2firstName, stimulus1Duration, new Dictionary<string, string>
                            {
                                { "stim1_stim2_time_ms", (stimDelay*timeCoef).ToString()},
                                { "stim1_pos",  s1position.ToString() },
                                { "stim2_pos", s2position.ToString() },
                                { "target_present", "yes" },
                                { "status", "correct"},//correct|error|too_slow

                            });
                            //2nd P2 (true reaction time)
                            eventLogger.LogEvent(p2secondName, stimulus2Duration, new Dictionary<string, string>
                            {
                                { "stim1_stim2_time_ms", (stimDelay*timeCoef).ToString()},
                                { "stim1_pos",  s1position.ToString() },
                                { "stim2_pos", s2position.ToString() },
                                { "target_present", "yes" },
                                { "status", "correct"},//correct|error|too_slow
                            });
                        }
                        SetState(State.Registered);
                        OnRegistered();
                    }
                    else
                    {
                        if(!IsTutorial)
                        {
                            //1st P2
                            eventLogger.LogEvent(p2firstName, stimulus1Duration, new Dictionary<string, string>
                            {
                                { "stim1_stim2_time_ms", (stimDelay).ToString()},
                                { "stim1_pos",  s1position.ToString() },
                                { "stim2_pos", s2position.ToString() },
                                { "target_present", "no" },
                                { "status", "error"},//correct|error|too_slow
                            });
                            //2nd P2 (true reaction time)
                            eventLogger.LogEvent(p2secondName, stimulus2Duration, new Dictionary<string, string>
                            {
                                { "stim1_stim2_time_ms", (stimDelay).ToString()},
                                { "stim1_pos",  s1position.ToString() },
                                { "stim2_pos", s2position.ToString() },
                                { "target_present", "no" },
                                { "status", "error"},//correct|error|too_slow
                            });
                        }
                        SetState(State.RegisteredMistake);
                        OnRegisteredMistake();
                    }
                }
                else if(noClicked)
                {
                    if (targetPresent)
                    {
                        if(!IsTutorial)
                        {
                            //1st P2
                            eventLogger.LogEvent(p2firstName, stimulus1Duration, new Dictionary<string, string>
                            {
                                { "stim1_stim2_time_ms", (stimDelay).ToString()},
                                { "stim1_pos",  s1position.ToString() },
                                { "stim2_pos", s2position.ToString() },
                                { "target_present", "yes" },
                                { "status", "error"},//correct|error|too_slow

                            });
                            //2nd P2 (true reaction time)
                            eventLogger.LogEvent(p2secondName, stimulus2Duration, new Dictionary<string, string>
                            {
                                { "stim1_stim2_time_ms", (stimDelay).ToString()},
                                { "stim1_pos",  s1position.ToString() },
                                { "stim2_pos", s2position.ToString() },
                                { "target_present", "yes" },
                                { "status", "error"},//correct|error|too_slow
                            });
                        }

                        SetState(State.RegisteredMistake);
                        OnRegisteredMistake();
                    }
                    else
                    {
                        if(!IsTutorial)
                        {
                            //1st P2
                            eventLogger.LogEvent(p2firstName, stimulus1Duration, new Dictionary<string, string>
                            {
                                { "stim1_stim2_time_ms", (stimDelay*timeCoef).ToString()},
                                { "stim1_pos",  s1position.ToString() },
                                { "stim2_pos", s2position.ToString() },
                                { "target_present", "no" },
                                { "status", "correct"},//correct|error|too_slow

                            });
                            //2nd P2 (true reaction time)
                            eventLogger.LogEvent(p2secondName, stimulus2Duration, new Dictionary<string, string>
                            {
                                { "stim1_stim2_time_ms", (stimDelay*timeCoef).ToString()},
                                { "stim1_pos",  s1position.ToString() },
                                { "stim2_pos", s2position.ToString() },
                                { "target_present", "no" },
                                { "status", "correct"},//correct|error|too_slow
                            });
                        }

                        SetState(State.Registered);
                        OnRegistered();
                    }
                }
                else//too long 
                {
                    if (!IsTutorial)
                    {
                        //1st P2
                        eventLogger.LogEvent(p2firstName, stimulus1Duration, new Dictionary<string, string>
                        {
                            { "stim1_stim2_time_ms", (stimDelay*(targetPresent ? timeCoef : 1)).ToString()},
                            { "stim1_pos",  s1position.ToString() },
                            { "stim2_pos", s2position.ToString() },
                            { "target_present", targetPresent ? "yes" : "no"},
                            { "status", "too_slow"},//correct|error|too_slow

                        });
                        //2nd P2 (true reaction time)
                        eventLogger.LogEvent(p2secondName, waitForReactionDurationSec, new Dictionary<string, string>
                        {
                            { "stim1_stim2_time_ms", (stimDelay*(targetPresent ? timeCoef : 1)).ToString()},
                            { "stim1_pos",  s1position.ToString() },
                            { "stim2_pos", s2position.ToString() },
                            { "target_present", targetPresent ? "yes" : "no" },
                            { "status", "too_slow" +
                            ""},//correct|error|too_slow
                        });
                    }

                    SetState(State.ReactTooLong);
                    OnReactTooLong();
                }
                yield return new WaitForSeconds(0.5f);


            }
        }


        // Idle
        SetState(State.Finish);
        OnFinish();
        Running = false;
    }

    int ChooseTargetVariant()
    {
        float roll = Random.value;
        if(roll < targetPresentFirstProbability)
        {
            return 1;
        }
        else if (roll < targetPresentSecondProbability + targetPresentFirstProbability)
        {
            return 2;
        }
        else//target not present
        {
            return 0;
        }
    }


    private void OnIdle()
    {
        UIManagerAttentionalBlink.GetInstance().StartState();
    }
    private void OnWaitUntilReady()
    {
        UIManagerAttentionalBlink.GetInstance().WaitForTap();
    }

    private void OnWaitUntilReadyTapped()
    {
        UIManagerAttentionalBlink.GetInstance().ClearScreen();
    }

    //s11LengthSec - first part of the first stimulus to last [seconds]
    private void OnStimulus1(bool isTargetPresent, int s1position, float s11LengthSec)
    {
        UIManagerAttentionalBlink.GetInstance().ShowStim1(isTargetPresent, s1position, s11LengthSec);
    }

    //s12LengthSec - first part of the second stimulus to last [seconds]
    private void OnStimulus2(bool isTargetPresent, int s2position, float s21LengthSec)
    {
        UIManagerAttentionalBlink.GetInstance().ShowStim2(isTargetPresent, s2position, s21LengthSec);
    }

    private void OnRegistered()
    {
        UIManagerAttentionalBlink.GetInstance().RegisteredCorrect();
    }

    private void OnRegisteredMistake()
    {
        UIManagerAttentionalBlink.GetInstance().RegisteredMistake();
    }

    private void OnReactTooLong()
    {
        UIManagerAttentionalBlink.GetInstance().RegisterTooLong();
    }

    private void OnFinish()
    {
        if (!IsTutorial)
        {
            GameManager.GetInstance().FinishDataRecord();
        }
        Debug.Log($"[StateMachine] Finished after {trialCounter} trials.");
        FinishEEGGame();
        UIManagerAttentionalBlink.GetInstance().GameFinished();
    }

    private void SetState(State newState)
    {
        currentState = newState;
        Debug.Log($"[StateMachine] State: {newState}");
    }


    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
