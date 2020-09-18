using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProfilingUtilities
{
    public static bool LogTimes = true;

    public static void LogAction(System.Action action, string text)
    {
        System.DateTime start = System.DateTime.Now;
        action();
        if (LogTimes)
        {
            System.DateTime end = System.DateTime.Now;
            Debug.Log($"perf {text} : {end - start}");
        }
    }

    public static void LogActionAggrigate(System.Action action, ref System.TimeSpan aggrigateTime)
    {
        System.DateTime start = System.DateTime.Now;
        action();
        if (LogTimes)
        {
            System.DateTime end = System.DateTime.Now;
            aggrigateTime += (end - start);
        }
    }
}
