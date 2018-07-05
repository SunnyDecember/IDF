using Ionic.Zip;
using JumpKick.HttpLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Runing.Increment;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using xuexue.file;

namespace UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        public UnitTest1()
        {
            //构造的时候开启文件服务器
            Process[] prcs = Process.GetProcesses();
            bool isServerRuning = false;
            for (int i = 0; i < prcs.Length; i++)
            {
                if (prcs[i].ProcessName.ToString().ToLower() == "FileServer".ToLower())
                {
                    isServerRuning = true;
                    break;
                }
            }
            if (!isServerRuning)
            {
                FileInfo fi = new FileInfo("../FileServer.exe");
                if (fi.Exists)
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = fi.FullName;
                    psi.WorkingDirectory = fi.DirectoryName;
                    Process.Start(psi);
                }
                else
                {
                    //联网去自己下一个文件服务器
                    bool isDone = false;
                    Http.Get("http://mr.xuexuesoft.com:8010/Other/FileServer.exe").DownloadTo(fi.FullName, (bytesCopied, totalBytes) =>
                    {

                    }, onSuccess: (headers) =>
                    {
                        isDone = true;
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = fi.FullName;
                        psi.WorkingDirectory = fi.DirectoryName;
                        Process.Start(psi);
                    }).OnFail((e) =>
                    {
                    }).Go();

                    while (!isDone)
                    {
                        Thread.Sleep(100);
                    }
                }
                Thread.Sleep(2000);
            }

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

        ~UnitTest1()
        {
            Runing.Increment.Log.ClearEvent();
        }

        /// <summary>
        /// 重设日志关联
        /// </summary>
        private void ResetLog()
        {
            Runing.Increment.Log.ClearEvent();
            Runing.Increment.Log.EventLogInfo += (s)=> { xuexue.DxDebug.LogFileOnly(s); };
            Runing.Increment.Log.EventLogDebug += (s) => { xuexue.DxDebug.LogFileOnly(s); };
            Runing.Increment.Log.EventLogWarning += (s) => { xuexue.DxDebug.LogFileOnly(s); };
            Runing.Increment.Log.EventLogError += (s) => { xuexue.DxDebug.LogFileOnly(s); };
            ;
        }

        /// <summary>
        /// 自带的DownloadTo方法的测试（如果路径上的文件已经被占用，那么就一直卡住）
        /// </summary>
        [TestMethod]
        public void TestMethodDownloadTo()
        {
            string url = "http://127.0.0.1:22333/Debug/Ionic.Zip.Unity.dll";
            string file = @"../test/Temp/Ionic.Zip.Unity.dll";
            FileInfo fi = new FileInfo(file);
            file = fi.FullName;
            if (!fi.Directory.Exists)
            {
                Directory.CreateDirectory(fi.Directory.FullName);
            }
            if (fi.Exists)
            {
                File.Delete(fi.FullName);
            }

            bool isDone = false;
            Http.Get(url).DownloadTo(file, (bytesCopied, totalBytes) =>
            {
                if (totalBytes.HasValue)
                {
                    Console.Write("Downloaded: " + (bytesCopied / totalBytes) * 100 + "%");
                }
                Console.Write("Downloaded: " + bytesCopied.ToString() + " bytes");
            },
            onSuccess: (headers) =>
             {
                 isDone = true;//下载完成
             }).OnFail((e) =>
             {
                 Console.WriteLine("UnitTestXML.TestMethod1():错误！");
                 isDone = true;//完成
             })
             //.OnSuccess((WebHeaderCollection a, Stream b) => //不能加这个，加了则会卡死
             //{
             //    b.ReadTimeout = 1000;
             //})
             .OnFail((e) =>
             {
                 isDone = true;//完成
             }).Go();

            while (!isDone)
            {
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// 拷贝两个流
        /// </summary>
        /// <param name="instream"></param>
        /// <param name="outstream"></param>
        private void CopyStream(Stream instream, Stream outstream)
        {
            const int bufferLen = 128;
            byte[] buffer = new byte[bufferLen];
            int count = 0;
            while ((count = instream.Read(buffer, 0, bufferLen)) > 0)
            {
                outstream.Write(buffer, 0, count);
                outstream.Flush();
            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            string url = "http://127.0.0.1:22333/Debug/Ionic.Zip.Unity.dll";
            string file = @"../test/Temp/Ionic.Zip.Unity.dll";
            FileInfo fi = new FileInfo(file);
            file = fi.FullName;
            if (!fi.Directory.Exists)
            {
                Directory.CreateDirectory(fi.Directory.FullName);
            }

            bool isDone = false;
            Http.Get(url).OnSuccess((WebHeaderCollection a, Stream s) =>
            {
                FileStream fs = File.Create(file);
                CopyStream(s, fs);
                fs.Close();
                isDone = true;
            })
            //.OnFail((e) =>
            //{
            //    Console.WriteLine("UnitTestXML.TestMethod1():错误！");
            //})
            .Go();

            while (!isDone)
            {
                Thread.Sleep(1000);
            }
        }

        [TestMethod]
        public void TestMethodCreatXML()
        {
            ResetLog();//重设日志

            for (int i = 0; i < 20; i++)
            {
                Runing.Increment.Log.Info("UnitTest.TestMethodCreatXML():生成xml文件");
                IDFHelper.CreatConfigFileWithXml("./", "http://127.0.0.1:22333/Debug/", "../test/IDFTest.zip");
            }
        }

        [TestMethod]
        public void TestMethodDownload()
        {
            ResetLog();//重设日志

            Runing.Increment.Log.Info("TestMethodDownload():当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);

            Console.WriteLine("UnitTestXML.GenerateXML():生成xml文件");
            IDFHelper.CreatConfigFileWithXml("../Debug/", "http://127.0.0.1:22333/Debug/", "../test/IDFTest.zip");

            //FileHelper.CleanDirectory("../test/Temp");

            bool isDone = false;
            IDFClient.Instance.Go("http://127.0.0.1:22333/test/IDFTest.zip", "../test/Temp", "../test/Target", "../test/Backup")
            .OnDownloadSuccess((obj) =>
            {
                Runing.Increment.Log.Info("TestMethodDownload():进入OnDownloadSuccess当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                isDone = true;
            })
            .OnError((e) =>
            {
                Runing.Increment.Log.Info("TestMethodDownload():进入OnError当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);

                isDone = true;
            });

            while (!isDone)
            {
                Thread.Sleep(1000);
            }
        }

        [TestMethod]
        public void TestMethodMoveFile()
        {
            ResetLog();//重设日志

            Runing.Increment.Log.Info("UnitTest1.TestMethodMoveFile():生成xml文件");
            IDFHelper.CreatConfigFileWithXml("../Debug/", "http://127.0.0.1:22333/Debug/", "../test/IDFTest.zip");

            FileHelper.CleanDirectory("../test/Temp");
            FileHelper.CleanDirectory("../test/Target");
            FileHelper.CleanDirectory("../test/Backup");
            Thread.Sleep(1000);

            bool isDone = false;
            IDFClient.Instance.Go("http://127.0.0.1:22333/test/IDFTest.zip", "../test/Temp", "../test/Target", "../test/Backup")
            .OnMoveFileDone((obj, success) =>
            {
                Runing.Increment.Log.Info("移动文件成功后回调");
                isDone = true;
            })
            .OnDownloadSuccess((obj) =>
            {
                //关闭那些程序
                obj.MoveFile();
            })
            .OnError((e) =>
            {
                isDone = true;
            });

            while (!isDone)
            {
                Thread.Sleep(1000);
            }

            var xml = XmlHelper.CreatXml();

            var fs = File.Open(new FileInfo("../test/IDFTest.zip").FullName, FileMode.Open, FileAccess.Read);
            ZipFile zip = ZipFile.Read(fs);
            ZipEntry ze = zip.Entries.First();//第一个实体
            MemoryStream xmlms = new MemoryStream();
            ze.Extract(xmlms);
            xmlms.Position = 0;
            xml.Load(xmlms); //从下载文件流中读xml
            OriginFolder fis = new OriginFolder();
            var node = xml.DocumentElement.SelectSingleNode("./" + typeof(OriginFolder).Name);
            fis.FromXml(node);//从xml文件根节点反序列化
            fs.Close();

            int index = 0;
            foreach (var item in fis.fileItemDict.Values)
            {
                index++;
                Runing.Increment.Log.Info($"测试{index}:测试校验文件" + item.relativePath);
                string itemTarFilePath = Path.Combine(new DirectoryInfo("../test/Target").FullName, item.relativePath);
                Assert.IsTrue(MD5Helper.FileMD5(itemTarFilePath) == item.MD5);
            }
        }

        [TestMethod]
        public void TestMethodMoveFile2()
        {
            ResetLog();//重设日志

            Runing.Increment.Log.Info("UnitTest1.TestMethodMoveFile2():生成xml文件");
            IDFHelper.CreatConfigFileWithXml("../Debug/", "http://127.0.0.1:22333/Debug/", "../test/IDFTest.zip");

            //FileHelper.CleanDirectory("../test/Temp");
            //FileHelper.CleanDirectory("../test/Target");
            //FileHelper.CleanDirectory("../test/Backup");

            FileInfo[] fis = new DirectoryInfo("../test/Target").GetFiles("*.*", SearchOption.AllDirectories);
            for (int i = 0; i < fis.Length; i++)
            {
                if (i % 2 == 0)
                {
                    File.Delete(fis[i].FullName);
                }
            }
            Thread.Sleep(1000);

            bool isDone = false;
            IDFClient.Instance.Go("http://127.0.0.1:22333/test/IDFTest.zip", "../test/Temp", "../test/Target", "../test/Backup")
            .OnMoveFileDone((obj, success) =>
            {
                Runing.Increment.Log.Info("移动文件成功后回调");
                isDone = true;
            })
            .OnDownloadSuccess((obj) =>
            {
                //关闭那些程序
                obj.MoveFile();
            })
            .OnError((e) =>
            {
                isDone = true;
            });

            while (!isDone)
            {
                Thread.Sleep(1000);
            }

            var xml = XmlHelper.CreatXml();

            var fs = File.Open(new FileInfo("../test/IDFTest.zip").FullName, FileMode.Open, FileAccess.Read);
            ZipFile zip = ZipFile.Read(fs);
            ZipEntry ze = zip.Entries.First();//第一个实体
            MemoryStream xmlms = new MemoryStream();
            ze.Extract(xmlms);
            xmlms.Position = 0;
            xml.Load(xmlms); //从下载文件流中读xml
            OriginFolder originFolder = new OriginFolder();
            var node = xml.DocumentElement.SelectSingleNode("./" + typeof(OriginFolder).Name);
            originFolder.FromXml(node);//从xml文件根节点反序列化
            fs.Close();

            int index = 0;
            foreach (var item in originFolder.fileItemDict.Values)
            {
                index++;
                Runing.Increment.Log.Info($"测试{index}:测试校验文件" + item.relativePath);
                string itemTarFilePath = Path.Combine(new DirectoryInfo("../test/Target").FullName, item.relativePath);
                Assert.IsTrue(MD5Helper.FileMD5(itemTarFilePath) == item.MD5);
            }
        }

        [TestMethod]
        public void TestMethodLog()
        {
            //Runing.Increment.Log.Warning("这是一个警告");
           // var fs = File.Create("1231.md");
            //fs.WriteByte(123);
            //输出日志到"即时窗口"，"调试->窗口->即时" 只在调试时有输出，运行没有日志
            for (int i = 0; i < 100; i++)
            {
                //fs.Close();
                //Debug.WriteLine("写一个日志！", "info");
                // Thread.Sleep(1);
            }
        }
    }
}