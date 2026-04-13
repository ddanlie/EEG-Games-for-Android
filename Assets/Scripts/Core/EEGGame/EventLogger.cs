using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UXF;

public class EventLogger
{
    private List<UXFDataRow> events = new List<UXFDataRow>();
    private long sessionStartTime;

    public EventLogger()
    {
        sessionStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }


    public void LogEvent(string eventType, float duration = 0f,
                         Dictionary<string, string> otherInfo = null)
    {
        float onset = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sessionStartTime;

        // Rows are set according to the BIDS requirements
        // see https://bids-specification.readthedocs.io/en/stable/modality-agnostic-files/events.html
        var row = new UXFDataRow()
        {
            ("onset", onset.ToString("F4")),
            ("duration", duration.ToString("F4")),
            ("trial_type", eventType),
            ("response_time", duration.ToString("F4"))
        };

        // Add any extra BIDS columns
        if (otherInfo != null)
        {
            foreach (var kv in otherInfo)
            {
                row.Add((kv.Key, kv.Value));
            }
        }

        events.Add(row);
    }

    public void SaveToTrial(Trial trial)
    {
        var header = new string[] { "onset", "duration", "trial_type" };
        var table = new UXFDataTable(header);
        foreach (var row in events) table.AddCompleteRow(row);

        trial.SaveDataTable(table, "events", UXFDataType.OtherTrialData);
    }
}