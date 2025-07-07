using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectionDetection.Enums
{
    public class EnumTypes
    {

    }

    public enum CaptureMode
    {
        Continuous,
        Trigger
    }

    public enum TriggerMode
    {
        SoftTware,
        IO
    }

    public enum StateStep
    {
        WaitPLC,
        TakeImage,
        StoreImage,
        ShowResult
    }
}
