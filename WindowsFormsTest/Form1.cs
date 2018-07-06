using Runing.Increment;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            //重设线程数
            ThreadPool.SetMinThreads(32, 32);

            //挂个日志文件输出
            xuexue.LogFile.GetInst().CreatLogFile("../test/log");
            xuexue.LogFile.GetInst().isImmediatelyFlush = true;
            xuexue.DxDebug.IsLogFile = true;
            xuexue.DxDebug.IsConsole = true;

            //清除日志文件
            xuexue.LogFile.GetInst().ClearLogFileInFolder("../test/log", 0.1f);
        }

        private void ResetLog()
        {
            Runing.Increment.Log.ClearEvent();
            Runing.Increment.Log.EventLogInfo += (s) => { xuexue.DxDebug.LogFileOnly(s); };
            Runing.Increment.Log.EventLogDebug += (s) => { xuexue.DxDebug.LogFileOnly(s); };
            Runing.Increment.Log.EventLogWarning += (s) => { xuexue.DxDebug.LogFileOnly(s); };
            Runing.Increment.Log.EventLogError += (s) => { xuexue.DxDebug.LogFileOnly(s); };
            ;
        }

        private void button1_Click(object sender, EventArgs ea)
        {
            ResetLog();//重设日志

            Runing.Increment.Log.Info("TestMethodDownload():当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);

            Console.WriteLine("UnitTestXML.GenerateXML():生成xml文件");
            IDFHelper.CreatConfigFileWithXml("../Debug/", "http://127.0.0.1:22333/Debug/", "../test/IDFTest.zip");

            //FileHelper.CleanDirectory("../test/Temp");

            //bool isDone = false;
            IDF.Update("http://127.0.0.1:22333/test/IDFTest.zip", "../test/Temp", "../test/Target", "../test/Backup")
            .OnDownloadSuccess((obj) =>
            {
                Runing.Increment.Log.Info("TestMethodDownload():进入OnDownloadSuccess当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                //isDone = true;
            })
            .OnError((e) =>
            {
                Runing.Increment.Log.Info("TestMethodDownload():进入OnError当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);

                //isDone = true;
            }).Go();

 
        }
    }
}
