using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using mindrove;

using Unity.VisualScripting;
using System;
using System.Threading;
using System.Linq;
using UnityEngine.UIElements;

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