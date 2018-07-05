using System;
using System.Threading;

namespace xuexue
{
    /// <summary>
    /// U3D和windows控制台显示信息
    /// </summary>
    public static class DxDebug
    {
        static DxDebug()
        {
            if (!isInit)
            {
#if UNITY_EDITOR //已经发布成dll它们不再有效
 
#endif

#if UNITY_ANDROID
        UnityEngine.GameObject go = new UnityEngine.GameObject("AndroidLog");
        UnityEngine.GameObject.DontDestroyOnLoad(go);
        go.AddComponent<LogGameObject>();
#endif
                isInit = true;
            }
        }

        /// <summary>
        /// 一个日志的结构体(内存中保存日志队列使用它)
        /// </summary>
        public class LogItem
        {
            /// <summary>
            /// 优先级
            /// </summary>
            public int priority;

            /// <summary>
            /// 日志文本
            /// </summary>
            public string message;
        }

        /// <summary>
        /// 内存中暂时保存的日志的条数
        /// </summary>
        private static int MAX_LENGTH = 256;

        /// <summary>
        /// 内存中暂存的日志队列
        /// </summary>
        private static DQueue<LogItem> _logQueue = new DQueue<LogItem>(MAX_LENGTH, 64);

        /// <summary>
        /// 内存日志的锁
        /// </summary>
        private static object _lockMem = new object();

        /// <summary>
        /// 是否进行日志的操作,默认值是true.
        /// </summary>
        public static bool isLog = true;

        /// <summary>
        /// 是否写到一个本地的log文件，默认值是false，需要手动调用LogFile.CreatLogFile()创建一个日志文件再设置这个值.
        /// </summary>
        public static bool IsLogFile = false;

        /// <summary>
        /// 是否写到控制台,移动端的时候也应该关闭，对应System.Console.WriteLine()函数，默认值是false.
        /// </summary>
        public static bool IsConsole = false;

        /// <summary>
        /// 如果要保存到这个内存队列中，日志需要的最低优先级，低于这个优先级的日志不会被保存，默认值是1；
        /// </summary>
        public static int MemoryPriority = 10;

        /// <summary>
        /// 如果要显示到控制台需要的最低优先级，默认值是2
        /// </summary>
        public static int ConsolePriority = 20;

        /// <summary>
        /// 警告日志优先级，默认值是4
        /// </summary>
        public static int WarningPriority = 40;

        /// <summary>
        /// 错误日志优先级，默认值是8
        /// </summary>
        public static int ErrorPriority = 80;

        /// <summary>
        /// 需要写到文件中的优先级(一般可以和控制台优先级一样)
        /// </summary>
        public static int FilePriority = 20;

        /// <summary>
        /// 提供一个打印事件把日志打印到调用模块的日志里去。参数是优先级和文本
        /// </summary>
        public static event Action<LogItem> EventPrint;

        /// <summary>
        /// 标记这个日志系统是否经过了初始化。因为实际上执行的功能函数就一个log。所以暂时用这个方法。
        /// </summary>
        private static bool isInit = false;

        /// <summary>
        /// 输出一条日志，默认这条日志的优先级为0
        /// </summary>
        /// <param name="e"></param>
        /// <param name="priority"></param>
        public static void Log(string e, int priority = 0)
        {
            //如果优先级很低就不作任何处理了，避免拼接字符串，这能提高很多服务器的速度
            if (priority < MemoryPriority && priority < ConsolePriority && priority < FilePriority)
            {
                return;
            }

            e = string.Format(@"[{6}][{7}][{0:D2}/{1:D2}][{2:D2}:{3:D2}:{4:D2}:{5:D3}]", DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond, Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId) + e;

            //如果优先级达到了显示到控制台的优先级
            if (priority >= ConsolePriority)
            {
#if UNITY_EDITOR
                    UnityEngine.Debug.Log(e);
#else
                //是否显示在控制台（主要是控制移动端或者没有控制台界面的情况）
                // 其中这句话也会在unity的日志系统（日志文件）中写入一个日志。
                if (IsConsole)
                {
                    try
                    {
                        System.Console.WriteLine(e);
                    }
                    catch (Exception)
                    {
                    }
                }
#endif
            }

            //是否写日志文件（Android和ios平台有一个路径问题）
            if (IsLogFile && priority >= FilePriority)
            {
                LogFile.GetInst().AddLine(ref e);
            }

            LogItem log = null;

            //如果优先级达到了记录到内存中的优先级
            if (priority >= MemoryPriority)
            {
                log = AddMemLog(priority, ref e);
            }

            if (EventPrint != null)//如果有打印事件就执行事件
            {
                if (log == null)
                    log = new LogItem() { priority = priority, message = e };
                try { EventPrint(log); }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// 警告日志，以WarningPriority来记录
        /// </summary>
        /// <param name="e"></param>
        public static void LogWarning(string e)
        {
            Log("[warning]" + e, WarningPriority);
        }

        /// <summary>
        /// 错误日志，以ErrorPriority来记录
        /// </summary>
        /// <param name="e"></param>
        public static void LogError(string e)
        {
            Log("[error]" + e, ErrorPriority);
        }

        /// <summary>
        /// 显示到控制台中的日志
        /// </summary>
        /// <param name="e"></param>
        public static void LogConsole(string e)
        {
            Log(e, ConsolePriority);
        }

        /// <summary>
        /// 只显示在控制台，不影响到其他的地方(这个函数和Log函数是独立并行)
        /// </summary>
        /// <param name="e"></param>
        public static void LogConsoleOnly(string e)
        {
            e = string.Format(@"[{6}][{7}][{0:D2}/{1:D2}][{2:D2}:{3:D2}:{4:D2}:{5:D3}]", DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond, Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId) + e;
            //是否显示在控制台（主要是控制移动端或者没有控制台界面的情况）
            if (IsConsole)
            {
                try
                {
                    System.Console.WriteLine(e);
                }
                catch (Exception)
                {
                }
            }

            if (EventPrint != null)//如果有打印事件就执行事件
            {
                try { EventPrint(new LogItem() { priority = FilePriority, message = e }); }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// 写日志到文件，不影响其他的地方(这个函数和Log函数是独立并行)
        /// </summary>
        /// <param name="e"></param>
        public static void LogFileOnly(string e)
        {
            e = string.Format(@"[{6}][{7}][{0:D2}/{1:D2}][{2:D2}:{3:D2}:{4:D2}:{5:D3}]", DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond, Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId) + e;
            if (IsLogFile)
            {
                LogFile.GetInst().AddLine(ref e);
            }

            if (EventPrint != null)//如果有打印事件就执行事件
            {
                try { EventPrint(new LogItem() { priority = FilePriority, message = e }); }
                catch (Exception) { }
            }
        }

        #region Memory Log

        /// <summary>
        /// 清空当前的所有内存日志
        /// </summary>
        public static void ClearMemLog()
        {
            lock (_lockMem)
            {
                _logQueue.Clear();
            }
        }

        /// <summary>
        /// 往内存日志队列添加一行日志
        /// </summary>
        /// <param name="pri">优先级</param>
        /// <param name="msg">日志内容</param>
        private static LogItem AddMemLog(int pri, ref string msg)
        {
            lock (_lockMem)
            {
                if (_logQueue.Count < MAX_LENGTH)
                {
                    LogItem log = new LogItem() { priority = pri, message = msg };
                    _logQueue.Enqueue(log);
                    return log;
                }
                else
                {
                    LogItem log = _logQueue.Dequeue();
                    log.priority = pri;
                    log.message = msg;
                    _logQueue.Enqueue(log);
                    return log;
                }
            }
        }

        /// <summary>
        /// 得到内存日志队列所有记录的日志
        /// </summary>
        /// <returns>日志内容的拷贝</returns>
        public static LogItem[] GetAllLog()
        {
            if (_logQueue.Count > 0)
            {
                lock (_lockMem)
                {
                    LogItem[] logs = _logQueue.ToArray();
                    _logQueue.Clear();
                    return logs;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 输出当前所有内存中的日志
        /// </summary>
        public static void AllMemLogOutput()
        {
            LogItem[] logs = GetAllLog();
            if (logs == null)
            {
                return;
            }
            for (int i = 0; i < logs.Length; i++)
            {
                LogItem log = logs[i];
                if (IsConsole)
                {
                    try { System.Console.WriteLine(log.message); }
                    catch (Exception) { }
                }

                if (EventPrint != null)//如果有打印事件就执行事件
                {
                    try { EventPrint(log); } catch (Exception) { }
                }
            }
        }

        #endregion Memory Log
    }
}