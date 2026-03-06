using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EEGEvent
{
    private string _eventId;
    public string eventId { get; private set; } = string.Empty;
    private string _eventType;
    public string eventType { get; private set; } = string.Empty;
}
