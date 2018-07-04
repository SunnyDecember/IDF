using System;

namespace Runing.Increment
{
    /// <summary>
    /// 这个模块的日志对外接口,添加对应log事件即可对外输出日志。
    /// </summary>
    public static class Log
    {
        public static event Action<string> EventLogDebug = null;

        public static event Action<string> EventLogInfo = null;

        public static event Action<string> EventLogWarning = null;

        public static event Action<string> EventLogError = null;

        public static void Debug(string msg)
        {
            if (EventLogDebug != null)
            {
                EventLogDebug(msg);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(msg, "Debug");//如果没有绑定事件，那么就随便输出一下到控制台算了
            }
        }

        public static void Info(string msg)
        {
            if (EventLogInfo != null)
            {
                EventLogInfo(msg);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(msg, "Info");//如果没有绑定事件，那么就随便输出一下到控制台算了
            }
        }

        public static void Warning(string msg)
        {
            if (EventLogWarning != null)
            {
                EventLogWarning(msg);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(msg, "Warning");//如果没有绑定事件，那么就随便输出一下到控制台算了
            }
        }

        public static void Error(string msg)
        {
            if (EventLogError != null)
            {
                EventLogError(msg);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(msg, "Error");//如果没有绑定事件，那么就随便输出一下到控制台算了
            }
        }
    }
}