using System;
using System.IO;
using System.Text;
using System.Threading;

namespace xuexue
{
    /// <summary>
    /// 将日志记录到本地文件，在移动端应该要设置一个正确的路径
    /// </summary>
    public class LogFile
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public LogFile()
        {
        }

        /// <summary>
        /// 析构
        /// </summary>
        ~LogFile()
        {
            Close();
        }

        private static LogFile _instance;

        /// <summary>
        /// 获得实例
        /// </summary>
        /// <returns></returns>
        public static LogFile GetInst()
        {
            if (_instance == null)
            {
                _instance = new LogFile();
            }
            return _instance;
        }

        /// <summary>
        /// 日志文件的FileStream
        /// </summary>
        private FileStream _fileStream = null;

        /// <summary>
        /// 日志文件的streamWriter
        /// </summary>
        private StreamWriter _streamWriter = null;

        /// <summary>
        /// 当日志文件还没有创建的时候，临时的记录到这个字符串队列中。。
        /// </summary>
        private DQueue<string> _tempString = new DQueue<string>(1024, 4);

        /// <summary>
        /// 用来隔一段时间自动刷新
        /// </summary>
        private Timer _timer;

        /// <summary>
        /// 程序名，会用在日志文件中
        /// </summary>
        public string programName = "dnetlog";

        /// <summary>
        /// 日志文件的扩展名(由于.net库中的FileInfo是使用带.号的，所以这里也认为带.号)
        /// </summary>
        public string fileExtension = ".txt";

        /// <summary>
        /// 日志文件夹路径
        /// </summary>
        public string folderPath = null;

        /// <summary>
        /// 日志文件最大字节数
        /// </summary>
        public long maxLogFileSize = 50 * 1024 * 1024;

        /// <summary>
        /// 是否立即刷新日志,默认为false
        /// </summary>
        public bool isImmediatelyFlush = false;

        /// <summary>
        /// 文件日志的锁
        /// </summary>
        private Object _lockFile = new Object();

        /// <summary>
        /// 定时器回调：每隔5秒钟刷新一次文件
        /// </summary>
        /// <param name="state"></param>
        private void OnTimerTick(object state)
        {
            try
            {
                Flush();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 清除文件夹中的较早的日志文件
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <param name="day">清除天数</param>
        public void ClearLogFileInFolder(string folderPath, float day = 3.0f)
        {
            //清除日志文件夹中的日志文件
            DirectoryInfo dirifp = new DirectoryInfo(folderPath);
            if (!dirifp.Exists)
            {
                return;
            }
            FileInfo[] fis = dirifp.GetFiles();
            for (int i = 0; i < fis.Length; i++)
            {
                try
                {
                    //如果文件名包含程序名，而且扩展名符合，那么就删除日志文件
                    if (fis[i].FullName.Contains(this.programName) && fis[i].Extension == this.fileExtension)
                    {
                        TimeSpan ts = DateTime.Now - fis[i].CreationTime;
                        if (ts.TotalDays > day)//超过3天的就删除
                        {
                            File.Delete(fis[i].FullName);
                        }
                    }
                    else
                    {
                        //如果不是日志文件那么也删除（不要删除了，防止误操作）
                        //File.Delete(fis[i].FullName);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// 指定目录创建日志文件(输入文件夹路径)
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <param name="isClearFolder">是否清除较早的日志文件</param>
        /// <param name="day">清除天数</param>
        public void CreatLogFile(string folderPath, bool isClearFolder = true, float day = 3.0f)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }
            lock (_lockFile)
            {
                //如果有资源就释放
                Close();

                try
                {
                    //检查路径是文件还是目录
                    FileInfo fileInfo = new FileInfo(folderPath);
                    if (fileInfo.Exists)//如果这玩意已经是个文件
                    {
                        this.folderPath = fileInfo.DirectoryName;//把路径设置为该文件的所属文件夹
                    }
                    else
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
                        if (!dirInfo.Exists)
                        {
                            Directory.CreateDirectory(dirInfo.FullName);
                        }
                        this.folderPath = dirInfo.FullName;
                    }

                    //清理日志文件夹
                    if (isClearFolder)
                    {
                        ClearLogFileInFolder(this.folderPath, day);
                    }

                    string time = string.Format(@"[{0:D2}.{1:D2}][{2:D2}-{3:D2}-{4:D2}]", DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

                    _fileStream = new FileStream(Path.Combine(this.folderPath, time + programName + fileExtension), FileMode.Create);
                    _streamWriter = new StreamWriter(_fileStream, Encoding.UTF8, 1024);
                    _streamWriter.AutoFlush = false;

                    //创建定时器
                    _timer = new Timer(new TimerCallback(OnTimerTick));
                    _timer.Change(250, 5000);

                    while (_tempString != null && _tempString.Count > 0)//如果已经记录有内容
                    {
                        string item = _tempString.Dequeue();
                        _streamWriter.WriteLine(item);
                        _streamWriter.Flush();
                    }
                    _tempString.TrimExcess();//不再使用了
                }
                catch (Exception)
                {
                    Close();
                }
            }
        }

        /// <summary>
        /// 不指定目录的话，在根目录的log文件夹创建日志文件（失败后放到AppData）
        /// </summary>
        public void CreatLogFile()
        {
            //尝试使用模块根目录
            string domainPath = AppDomain.CurrentDomain.BaseDirectory;
            //unity中调用这几个文件夹容易返回null
            if (string.IsNullOrEmpty(domainPath))
            {
                domainPath = System.Environment.CurrentDirectory;
            }
            if (string.IsNullOrEmpty(domainPath))//如果还为空
            {
                domainPath = "";
            }

            CreatLogFile(Path.Combine(domainPath, "log"));

            //尝试使用AppData目录,应该在AppData/xuexue/log文件夹
            if (_fileStream == null)
            {
                string path = "log";
                string appdata = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                if (!string.IsNullOrEmpty(appdata))
                    path = Path.Combine(Path.Combine(appdata, "xuexue"), "log");
                CreatLogFile(path);
            }
        }

        /// <summary>
        /// 直接设置一个FileStream
        /// </summary>
        public void SetFileStream(FileStream fileStream)
        {
            //如果有资源就释放
            Close();
            try
            {
                _fileStream = fileStream;
                _streamWriter = new StreamWriter(_fileStream, Encoding.UTF8, 1024);
                _streamWriter.AutoFlush = false;

                //创建定时器
                _timer = new Timer(new TimerCallback(OnTimerTick));
                _timer.Change(250, 5000);
            }
            catch (Exception e)
            {
                Close();
            }
        }

        /// <summary>
        /// 向文件里写日志
        /// </summary>
        /// <param name="e"></param>
        public void Add(ref string e)
        {
            if (_fileStream == null && _tempString != null)
            {
                _tempString.EnqueueMaxLimit(e);
                return;
            }

            lock (_lockFile)
            {
                try
                {
                    _streamWriter.Write(e);
                    if (isImmediatelyFlush)
                    {
                        Flush();
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// 向文件里写一行日志
        /// </summary>
        /// <param name="e"></param>
        public void AddLine(ref string e)
        {
            if (_fileStream == null && _tempString != null)
            {
                _tempString.EnqueueMaxLimit(e);
                return;
            }

            lock (_lockFile)
            {
                try
                {
                    _streamWriter.WriteLine(e);
                    if (isImmediatelyFlush)
                    {
                        Flush();
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// 刷新日志文件流
        /// </summary>
        public void Flush()
        {
            lock (_lockFile)
            {
                try
                {
                    if (_streamWriter != null)
                    {
                        _streamWriter.Flush();
                        if (_fileStream.Length > maxLogFileSize)
                        {
                            _streamWriter.WriteLine("too big, new log File!");
                            _streamWriter.Flush();
                            CreatLogFile(this.folderPath);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// 关闭日志文件，在程序退出的时候应该调用
        /// </summary>
        public void Close()
        {
            lock (_lockFile)
            {
                try
                {
                    if (_timer != null)
                    {
                        _timer.Dispose();
                    }

                    if (_fileStream != null)
                    {
                        _fileStream.Flush(true);
                        _fileStream.Close();
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    _fileStream = null;
                    _streamWriter = null;
                    _timer = null;
                }
            }
        }
    }
}