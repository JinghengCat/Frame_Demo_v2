using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Logger
{
    public static void Log(string str)
    {
        if (!LoggerSwitch.m_DebugSwitch)
        {
            return;
        }
        Debug.Log(str);
    }

    public static void LogFormat(string strFormat, params object[] objs)
    {
        if (!LoggerSwitch.m_DebugSwitch)
        {
            return;
        }
        Debug.LogFormat(strFormat, objs);
    }

    public static void LogWarning(string str)
    {
        if (!LoggerSwitch.m_DebugSwitch)
        {
            return;
        }
        Debug.LogWarning(str);
    }

    public static void LogWarningFormat(string strFormat, params object[] objs)
    {
        if (!LoggerSwitch.m_DebugSwitch)
        {
            return;
        }
        Debug.LogWarningFormat(strFormat, objs);
    }

    public static void LogError(string str)
    {
        if (!LoggerSwitch.m_DebugSwitch)
        {
            return;
        }
        Debug.LogError(str);
    }

    public static void LogErrorFormat(string strFormat, params object[] objs)
    {
        if (!LoggerSwitch.m_DebugSwitch)
        {
            return;
        }
        Debug.LogErrorFormat(strFormat, objs);
    }
}
