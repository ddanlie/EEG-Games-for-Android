using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using mindrove;

using Unity.VisualScripting;
using System;
using System.Threading;
using System.Linq;
using UnityEngine.UIElements;

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