using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using mindrove;

using Unity.VisualScripting;
using System;
using System.Threading;
using System.Linq;
using UnityEngine.UIElements;

public abstract class AbstractEEGSignalSource : MonoBehaviour
{
    // Singleton
    private static AbstractEEGSignalSource instance = null;
    // Thread
    private readonly System.Object sourceLock = new System.Object();
    private Thread streamThread = null;
    private bool stopStreamProcess = false;
    private double[][] eegData = null;
    protected double[][] EEGData { get { return this.eegData; } }
    // Flags
    private bool sourceInitialized = false;
    public bool IsSourceInitialized { get { return sourceInitialized; } }
    private bool sourceStreaming = false;
    public bool IsSourceStreaming { get { return sourceStreaming; } }

    public void Awake()
    {
        if (AbstractEEGSignalSource.instance == null)
        {
            AbstractEEGSignalSource.instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // destroy duplicate
        }
    }

    private void OnDestroy()
    {
        try
        {
            this.EndSession();
            this.CustomOnDestroy();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.Log(e.Message);
        }
        AbstractEEGSignalSource.instance = null;
    }

    protected virtual void CustomOnDestroy()
    {

    }

    public static AbstractEEGSignalSource GetInstance()
    {
        if (AbstractEEGSignalSource.instance == null)
        {
            AbstractEEGSignalSource.instance = FindObjectOfType<AbstractEEGSignalSource>();
        }
        return AbstractEEGSignalSource.instance;
    }


    public bool InitEEGSource()
    {
        if (this.sourceInitialized || this.sourceStreaming)
        {
            this.sourceInitialized = true;
            return true;
        }
        this.sourceStreaming = false;
        try
        {
            // No lock needed
            this.sourceInitialized = this.CustomInitEEGSource();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.Log(e.Message);
            this.sourceInitialized = false;
        }
        return this.sourceInitialized;
    }

    // Return whether operation regarding source init was successfull or not
    protected abstract bool CustomInitEEGSource();


    public bool EndSession()
    {
        try
        {
            this.StopStreaming();
            this.sourceInitialized = !this.CustomEndSession();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.Log(e.Message);
            this.sourceInitialized = true;
        }
        return !this.sourceInitialized;
    }

    // Return whether operation regarding session finish was successfull or not
    protected abstract bool CustomEndSession();


    public bool StartStreaming()
    {
        if (!this.sourceInitialized)
        {
            this.sourceStreaming = false;
            return false;
        }
        if (this.sourceStreaming)
        {
            return true;
        }
        try
        {
            // No lock needed
            this.sourceStreaming = this.CustomStartStreaming();
            if(!this.sourceStreaming)
            {
                throw new Exception("Overridden function CustomStartStreaming() failed");
            }
            this.streamThread = new Thread(new ThreadStart(this.StreamWorker));
            this.streamThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.Log(e.Message);
            this.sourceStreaming = false;
        }
        return this.sourceStreaming;
    }
    private void StreamWorker()
    {
        while (!this.stopStreamProcess && this.sourceInitialized && this.sourceStreaming)
        {
            lock (this.sourceLock)
            {
                this.eegData = this.AssignStreamData();
            }
        }
    }

    // Return whether operation regarding stram start was successfull or not
    protected abstract bool CustomStartStreaming();

    protected abstract double[][] AssignStreamData();

    public bool StopStreaming()
    {
        if (!this.sourceStreaming) { return true; }

        try
        {
            // First - stop reading the signal from source
            this.stopStreamProcess = true; // Stop while loop of internal private StreamWorker() thread
            this.streamThread.Join();
            this.stopStreamProcess = false; // Reset flag

            // Then ask to stop the stream
            lock (this.sourceLock)
            {
                this.sourceStreaming = !this.CustomStopStreaming();
            }
            if(!this.sourceStreaming)
            {
                throw new Exception("Overridden function CustomStopStreaming() failed");
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.Log(e.Message);

            // If some fail - fallback to reading the stream
            this.sourceStreaming = true;
            this.streamThread = new Thread(new ThreadStart(this.StreamWorker));
            this.streamThread.Start();
        }
        return !this.sourceStreaming;
    }

    // Return whether operation regarding stream stopping was successfull or not
    protected abstract bool CustomStopStreaming();
    public abstract double[][] GetCurrentData();

    public virtual string GetCurrentDataFormatted()
    {
        if (this.eegData == null)
        {
            return "NULL";
        }
        int lastIndex = this.eegData[0].Length - 1;
        double[] lastValues = Enumerable.Range(0, this.eegData.GetLength(0)).Select(rowIndex => this.eegData[rowIndex][lastIndex]).ToArray();
        return $"Channels ({this.eegData.GetLength(0)}):\n{string.Join("  ", lastValues)}";
    }

    public virtual int GetSamplingRate()
    {
        return -1;
    }

}

public class DesktopMindroveEEGSignalSource : AbstractEEGSignalSource
{
    // Board 
    //# 0 → Fp1  1
    //# 1 → Fp2  2
    //# 2 → C5   3
    //# 3 → C1   4
    //# 4 → C2   5
    //# 5 → C6   6
    //# 6 → O1   7
    //# 7 → O2   8
    private int boardId = -1;
    private BoardShim boardShim = null; // used by multiple 

    protected override bool CustomInitEEGSource()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        BoardShim.enable_dev_board_logger();
        BoardShim.set_log_file("MINDROVE.log");
#endif
        MindRoveInputParams input_params = new MindRoveInputParams();
        this.boardId = (int)BoardIds.MINDROVE_WIFI_BOARD;
        this.boardShim = new BoardShim(this.boardId, input_params);
        Debug.Log("Calling prepare_session()");
        this.boardShim.prepare_session();
        this.boardShim.config_board(BoardShim.MindroveWifiConfigMode.EEG_MODE);
        return true;
    }

    protected override bool CustomEndSession()
    {
        this.boardShim.release_session();
        return true;
    }

    protected override bool CustomStartStreaming()
    {
        this.boardShim.start_stream();
        return true;
    }

    protected override double[][] AssignStreamData()
    {
        //Debug.Log("Stream data thread started");
        int[] eegChannelsIndexes = BoardShim.get_eeg_channels(this.boardId);
        int samplingRate = this.GetSamplingRate();
        int windowSize = 2; // seconds
        int numPoints = windowSize * samplingRate;

        double[][] eegData = this.EEGData;
        if (this.boardShim.get_board_data_count() >= numPoints)
        {
            Debug.Log("Reading data buffer...");
            double[,] data = this.boardShim.get_current_board_data(numPoints); // output is: (num_channels + some_data_num, numPoints) size array
            eegData = eegChannelsIndexes.Select(index => Enumerable.Range(0, data.GetLength(1)).Select(colIndex => data[index, colIndex]).ToArray()).ToArray();
        }

        return eegData;
    }


    protected override bool CustomStopStreaming()
    {
        this.boardShim.stop_stream();
        return true;
    }
    public override double[][] GetCurrentData()
    {
        return this.EEGData;
    }

    public override int GetSamplingRate()
    {
        if (this.boardId >= 0)
        {
            return BoardShim.get_sampling_rate(this.boardId);
        }
        return -1;
    }
}

public class FileEEGSignalSource : AbstractEEGSignalSource
{

    protected override double[][] AssignStreamData()
    {
        throw new NotImplementedException();
    }

    protected override bool CustomEndSession()
    {
        throw new NotImplementedException();
    }

    protected override bool CustomInitEEGSource()
    {

        throw new NotImplementedException();
    }

    protected override bool CustomStartStreaming()
    {
        throw new NotImplementedException();
    }

    protected override bool CustomStopStreaming()
    {
        throw new NotImplementedException();
    }

    public override double[][] GetCurrentData()
    {
        throw new NotImplementedException();
    }

}

public class AndroidMindroveEEGSignalSource : AbstractEEGSignalSource
{
    //# 0 → Fp1  1
    //# 1 → Fp2  2
    //# 2 → C5   3
    //# 3 → C1   4
    //# 4 → C2   5
    //# 5 → C6   6
    //# 6 → O1   7
    //# 7 → O2   8
    private AndroidJavaObject serverManager;
    private AndroidJavaObject currentActivity;
    protected override bool CustomInitEEGSource()
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            Debug.Log($"Android platform expected to init EEG source, found {Application.platform}");
            return false;
        }
        // 1. Get the current Android Activity context
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }
        return true;
    }

    public override double[][] GetCurrentData()
    {
        return this.EEGData;
    }

    protected override double[][] AssignStreamData()
    {
        if (serverManager == null)
        {
            return this.EEGData;
        }
        // Assuming there is a method to get the latest data object
        AndroidJavaObject sensorData = serverManager.Call<AndroidJavaObject>("getLatestData");
        if (sensorData == null)
        {
            return this.EEGData;
        }

        //TODO: get all channels
        double ch1 = sensorData.Get<double>("channel1");
        double ch2 = sensorData.Get<double>("channel2");
        double ch3 = sensorData.Get<double>("channel3");
        double ch4 = sensorData.Get<double>("channel4");
        double ch5 = sensorData.Get<double>("channel5");
        double ch6 = sensorData.Get<double>("channel6");
        double ch7 = sensorData.Get<double>("channel7");
        double ch8 = sensorData.Get<double>("channel8");

        return new double[][]
        {
            new double[] { ch1 },
            new double[] { ch2 },
            new double[] { ch3 },
            new double[] { ch4 },
            new double[] { ch5 },
            new double[] { ch6 },
            new double[] { ch7 },
            new double[] { ch8 }
        };
    }

    protected override bool CustomEndSession()
    {
        if (serverManager == null)
        {
            return false;
        }
        serverManager.Call("stop");
        serverManager = null;
        return true;
    }

    protected override bool CustomStartStreaming()
    {
        // 2. Initialize the MindRove ServerManager from the AAR
        serverManager = new AndroidJavaObject("mylibrary.mindrove.ServerManager", currentActivity);

        // 3. Start the server (based on the documentation logic)
        serverManager.Call("start");

        return true;
    }

    protected override bool CustomStopStreaming()
    {
        return true;// Server has no stream stop function
    }
}







// OLD EEG SOURCE CODE - works


//public class EEGSignalSource : AbstractEEGSignalSource
//{
//    // Singleton
//    private static EEGSignalSource instance = null;
//    // Board 
//    private int boardId = -1;
//    private BoardShim boardShim = null; // used by multiple 
//    // Thread
//    private readonly System.Object boardShimLock = new System.Object();
//    private Thread streamThread = null;
//    private bool stopStreamProcess = false;
//    private double[][] eegData = null;
//    // Flags
//    private bool sourceInitialized = false;
//    public bool IsSourceInitialized { get { return sourceInitialized; } }
//    private bool sourceStreaming = false;
//    public bool IsSourceStreaming { get { return sourceStreaming; } }

//    public void Awake()
//    {
//        if (EEGSignalSource.instance == null)
//        {
//            EEGSignalSource.instance = this;
//            DontDestroyOnLoad(gameObject);
//        }
//        else
//        {
//            Destroy(gameObject); // destroy duplicate
//        }
//    }

//    void OnDestroy() => this.Dispose();
//    void OnApplicationQuit() => this.Dispose();
//    public void Dispose()
//    {
//        try
//        {
//            this.StopStreaming();
//            lock (boardShimLock)
//            {
//                this.boardShim.release_session();
//            }
//        }
//        catch (Exception e)
//        {
//            Debug.LogException(e);
//            Debug.Log(e.Message);
//        }
//        EEGSignalSource.instance = null;
//    }

//    public static EEGSignalSource GetInstance()
//    {
//        if (EEGSignalSource.instance == null)
//        {
//            EEGSignalSource.instance = FindObjectOfType<EEGSignalSource>();
//        }
//        return EEGSignalSource.instance;
//    }


//    public bool InitEEGSource()
//    {
//        if (this.sourceInitialized || this.sourceStreaming) 
//        {
//            this.sourceInitialized = true;
//            return true;
//        }
//        this.sourceStreaming = false;
//        try
//        {
//            if (this.boardShim != null)
//            {
//                this.boardShim.release_session();
//            }
//        }
//        catch { }
//        finally { this.boardShim = null; }
//        try
//        {
//            #if UNITY_EDITOR || UNITY_STANDALONE
//                BoardShim.enable_dev_board_logger();
//                BoardShim.set_log_file("MINDROVE.log");
//            #endif
//            MindRoveInputParams input_params = new MindRoveInputParams();
//            this.boardId = (int)BoardIds.MINDROVE_WIFI_BOARD;
//            this.boardShim = new BoardShim(this.boardId, input_params);
//            Debug.Log("Prepare session called");
//            this.boardShim.prepare_session();
//            this.boardShim.config_board(BoardShim.MindroveWifiConfigMode.EEG_MODE);
//            this.sourceInitialized = true;
//        }
//        catch (Exception e)
//        {
//            Debug.LogException(e);
//            Debug.Log(e.Message);
//            this.sourceInitialized = false;
//        }
//        return this.sourceInitialized;
//    }

//    public bool EndSession()
//    {
//        try
//        {
//            this.StopStreaming();
//            lock (boardShimLock)
//            {
//                this.boardShim.release_session();
//            }
//            this.sourceInitialized = false;
//        }
//        catch (Exception e)
//        {
//            Debug.LogException(e);
//            Debug.Log(e.Message);
//            this.sourceInitialized = true;
//        }
//        return this.sourceInitialized;
//    } 

//    public bool StartStreaming()
//    {
//        if(!this.sourceInitialized) 
//        { 
//            this.sourceStreaming = false;
//            return false; 
//        }
//        if(this.sourceStreaming) 
//        { 
//            return true; 
//        }
//        try
//        {
//            this.boardShim.start_stream();
//            // Continuously set new data using extra thread
//            this.streamThread = new Thread(new ThreadStart(this.ProcessStreamData));
//            this.streamThread.Start();
//            this.sourceStreaming = true;
//        }
//        catch (Exception e)
//        {
//            Debug.LogException(e);
//            Debug.Log(e.Message);
//            this.sourceStreaming = false;
//        }
//        return this.sourceStreaming;
//    }


//    private void ProcessStreamData()
//    {
//        //Debug.Log("Stream data thread started");
//        int[] eegChannelsIndexes = BoardShim.get_eeg_channels(this.boardId);
//        int samplingRate = this.GetSamplingRate();
//        int windowSize = 2; // seconds
//        int numPoints = windowSize * samplingRate;

//        //Debug.Log($"Sampling rate: {samplingRate}");

//        while (!this.stopStreamProcess && this.sourceInitialized && this.sourceStreaming)
//        {
//            Debug.Log("Line before shim lock");
//            lock (boardShimLock)
//            {
//                //Debug.Log($"Data count {this.boardShim.get_board_data_count()} and NumPoints: {numPoints}");
//                //Debug.Log($"IF: {this.boardShim.get_board_data_count() >= numPoints}");
//                if (this.boardShim.get_board_data_count() >= numPoints)
//                {
//                    Debug.Log("Reading data buffer...");
//                    double[,] data = this.boardShim.get_current_board_data(numPoints); // output is: (num_channels + some_data_num, numPoints) size array
//                    this.eegData = eegChannelsIndexes.Select(index => Enumerable.Range(0, data.GetLength(1)).Select(colIndex => data[index, colIndex]).ToArray()).ToArray();

//                    //Debug.Log(this.eegData);
//                }
//            }
//            //Debug.Log("Data was read");
//        }
//    }


//    public bool StopStreaming()
//    {
//        if (!this.sourceStreaming) { return true; }

//        try
//        {
//            this.stopStreamProcess = true;
//            this.streamThread.Join();
//            this.boardShim.stop_stream();
//            this.stopStreamProcess = false;
//            this.sourceStreaming = false;
//        }
//        catch (Exception e)
//        {
//            Debug.LogException(e);
//            Debug.Log(e.Message);
//            this.sourceStreaming = true;
//        }
//        return !this.sourceStreaming;
//    }

//    public double[][] GetCurrentData()
//    {
//        return this.eegData;
//    }

//    public string GetCurrentDataFormatted()
//    {
//        if(this.eegData == null)
//        {
//            return "NULL";
//        }
//        int lastIndex = this.eegData[0].Length - 1;
//        double[] lastValues = Enumerable.Range(0, this.eegData.GetLength(0)).Select(rowIndex => this.eegData[rowIndex][lastIndex]).ToArray();
//        return $"Channels ({this.eegData.GetLength(0)}): {string.Join(" ", lastValues)}";
//    }

//    public int GetSamplingRate()
//    {
//        if(this.boardId >= 0)
//        {
//            return BoardShim.get_sampling_rate(this.boardId);
//        }
//        return -1;
//    }
//}


