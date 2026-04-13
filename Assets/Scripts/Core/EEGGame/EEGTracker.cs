using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UXF;


//TODO: do not use this, EEG source has all data.
// modify it or add utilities function to just save source data
public class EEGTracker : Tracker
{
    // Define your column headers
    public override string MeasurementDescriptor => "eeg";

    public override IEnumerable<string> CustomHeader =>
        new string[] { 
            "channel_1", "channel_2", "channel_3", 
            "channel_4", "channel_5", "channel_6",
            "channel_7", "channel_8"
        };

    protected override UXFDataRow GetCurrentValues()
    {
        // Read from your singleton
        var data = EEGSingleton.Instance.LatestSample;

        return new UXFDataRow()
        {
            ("channel_1", data.ch1.ToString()),
            ("channel_2", data.ch2.ToString()),
            ("channel_3", data.ch3.ToString()),
            ("timestamp", data.timestamp.ToString())
        };
    }
}