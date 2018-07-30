using Ionic.Zip;
using JumpKick.HttpLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Runing.Increment;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
        private void ResetLog(bool isDebug = true)
        {
            Runing.Increment.Log.ClearEvent();
            Runing.Increment.Log.EventLogInfo += (s) => { xuexue.DxDebug.LogFileOnly(s); };
            Runing.Increment.Log.EventLogWarning += (s) => { xuexue.DxDebug.LogFileOnly(s); };
            Runing.Increment.Log.EventLogError += (s) => { xuexue.DxDebug.LogFileOnly(s); };

            if (isDebug)
                Runing.Increment.Log.EventLogDebug += (s) => { xuexue.DxDebug.LogFileOnly(s); };
        }

        /// <summary>
        /// 自带的DownloadTo方法的测试（如果路径上的文件已经被占用，那么就一直卡住）
        /// </summary>
        [TestMethod]
        public void TestMethodHttpLib1()
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
                Thread.Sleep(50);
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

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        /// (Unit Test Method) tests method HTTP library 2.
        /// 这个函数的目前执行日志如下：
        /// [][12][07/31][06:05:37:448]UnitTest.TestMethodHttpLib2():启动当前执行线程id=12
        /// [] [12] [07/31] [06:05:37:697] UnitTest.TestMethodHttpLib2():Go之后执行线程id=12
        /// [] [15] [07/31] [06:05:37:775] UnitTest.TestMethodHttpLib2():OnSuccess里执行线程id=15
        /// </summary>
        ///
        /// <remarks> Surface, 2018/7/31. </remarks>
        ///-------------------------------------------------------------------------------------------------
        [TestMethod]
        public void TestMethodHttpLib2()
        {
            ResetLog();//重设日志

            string url = "http://127.0.0.1:22333/Debug/Ionic.Zip.Unity.dll";
            string file = @"../test/Temp/Ionic.Zip.Unity.dll";
            FileInfo fi = new FileInfo(file);
            file = fi.FullName;
            if (!fi.Directory.Exists)
            {
                Directory.CreateDirectory(fi.Directory.FullName);
            }
            Runing.Increment.Log.Debug("UnitTest.TestMethodHttpLib2():启动当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
            bool isDone = false;
            Http.Get(url).OnSuccess((WebHeaderCollection a, Stream s) =>
            {
                Runing.Increment.Log.Debug("UnitTest.TestMethodHttpLib2():OnSuccess里执行线程id=" + Thread.CurrentThread.ManagedThreadId);
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
            Runing.Increment.Log.Debug("UnitTest.TestMethodHttpLib2():Go之后执行线程id=" + Thread.CurrentThread.ManagedThreadId);
            while (!isDone)
            {
                Thread.Sleep(50);
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
            //SynchronizationContext contex = new SynchronizationContext();
            //SynchronizationContext.SetSynchronizationContext(contex);

            //这样调用它没有跳回原来的线程
            Runing.Increment.Log.Info("TestMethodDownload():当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);

            Console.WriteLine("UnitTestXML.GenerateXML():生成xml文件");
            IDFHelper.CreatConfigFileWithXml("../Debug/", "http://127.0.0.1:22333/Debug/", "../test/IDFTest.zip");

            //FileHelper.CleanDirectory("../test/Temp");

            bool isDone = false;
            IDF.Update("http://127.0.0.1:22333/test/IDFTest.zip", "../test/Temp", "../test/Target", "../test/Backup")
               .OnDownloadSuccess((obj) =>
               {
                   Runing.Increment.Log.Info("TestMethodDownload():进入OnDownloadSuccess当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                   isDone = true;
               })
               .OnError((e) =>
               {
                   Runing.Increment.Log.Info("TestMethodDownload():进入OnError当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);

                   Assert.Fail();//下载错误，认为不通过
                   isDone = true;
               }).Go();

            while (!isDone)
            {
                Thread.Sleep(15);
            }
        }

        [TestMethod]
        public void TestMethodDownloadAsync()
        {
            ResetLog();//重设日志
            bool isDone = false;
            bool isFail = false;

            //使用这个线程它也不会重新跳回原来的线程
            Task.Run(() =>
            {
                Runing.Increment.Log.Info("TestMethodDownload():当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);

                Console.WriteLine("UnitTestXML.GenerateXML():生成xml文件");
                IDFHelper.CreatConfigFileWithXml("../Debug/", "http://127.0.0.1:22333/Debug/", "../test/IDFTest.zip");

                //FileHelper.CleanDirectory("../test/Temp");

                IDF.Update("http://127.0.0.1:22333/test/IDFTest.zip", "../test/Temp", "../test/Target", "../test/Backup")
                .OnDownloadSuccess((obj) =>
                {
                    Runing.Increment.Log.Info("TestMethodDownload():进入OnDownloadSuccess当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                    isDone = true;
                })
                .OnError((e) =>
                {
                    Runing.Increment.Log.Info("TestMethodDownload():进入OnError当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                    isDone = true;
                    isFail = true;
                }).Go();
            });
            while (!isDone)
            {
                Thread.Sleep(15);
            }
            Assert.IsFalse(isFail);
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
            IDF.Update("http://127.0.0.1:22333/test/IDFTest.zip", "../test/Temp", "../test/Target", "../test/Backup")
            .OnMoveFileDone((obj, success) =>
            {
                Runing.Increment.Log.Info("移动文件成功后回调");
                isDone = true;
            })
            .OnDownloadSuccess((obj) =>
            {
                //关闭那些程序
                //异步的移动文件
               Task.Run(()=> { obj.MoveFile(); });
            })
            .OnError((e) =>
            {
                isDone = true;
            }).Go();

            while (!isDone)
            {
                Thread.Sleep(500);
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

            if (Directory.Exists("../test/Target"))
            {
                FileInfo[] fis = new DirectoryInfo("../test/Target").GetFiles("*.*", SearchOption.AllDirectories);
                for (int i = 0; i < fis.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        File.Delete(fis[i].FullName);
                    }
                }
            }

            Thread.Sleep(500);

            bool isDone = false;
            IDF.Update("http://127.0.0.1:22333/test/IDFTest.zip", "../test/Temp", "../test/Target", "../test/Backup")
            .OnMoveFileDone((obj, success) =>
            {
                Runing.Increment.Log.Info("移动文件成功后回调");
                isDone = true;
            })
            .OnDownloadSuccess((obj) =>
            {
                //关闭那些程序
                //异步的移动文件
                Task.Run(() => { obj.MoveFile(); });
            })
            .OnError((e) =>
            {
                isDone = true;
            }).Go();

            while (!isDone)
            {
                Thread.Sleep(50);
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
        public void TestMethodRecoverFile()
        {
            ResetLog();//重设日志

            if (Directory.Exists("../test/Target"))
            {
                FileInfo[] fis = new DirectoryInfo("../test/Target").GetFiles("*", SearchOption.AllDirectories);
                for (int i = 0; i < fis.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        File.Delete(fis[i].FullName);
                    }
                }
            }
            Thread.Sleep(500);

            //干扰文件项
            var ws = File.CreateText(new FileInfo("../Debug/TestFile.txt").FullName);
            ws.Write("123456789123456789");
            ws.Close();

            ws = File.CreateText(new FileInfo("../test/Target/TestFile.txt").FullName);
            ws.Write("12345");
            ws.Close();

            Runing.Increment.Log.Info("UnitTest1.TestMethodRecoverFile():生成xml文件");
            IDFHelper.CreatConfigFileWithXml("../Debug/", "http://127.0.0.1:22333/Debug/", "../test/IDFTest.zip");

            //记录备份前目标文件的md5值
            DirectoryInfo backupBeforeDir = new DirectoryInfo("../test/Target");
            FileInfo[] backupBeforeFiles = backupBeforeDir.GetFiles("*", SearchOption.AllDirectories);
            Dictionary<string, string> backupBeforeMD5 = new Dictionary<string, string>();
            for (int i = 0; i < backupBeforeFiles.Length; i++)
            {
                FileInfo file = backupBeforeFiles[i];
                backupBeforeMD5.Add(file.FullName, MD5Helper.FileMD5(file.FullName));
            }

            bool isDone = false;
            IDF.Update("http://127.0.0.1:22333/test/IDFTest.zip", "../test/Temp", "../test/Target", "../test/Backup")
            .OnMoveFileDone((obj, success) =>
            {
                string str = File.ReadAllText(new FileInfo("../test/Target/TestFile.txt").FullName);
                Assert.IsTrue(str == "123456789123456789");

                obj.RecoverFile();
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
            }).Go();

            while (!isDone)
            {
                Thread.Sleep(50);
            }

            //记录备份后目标文件的md5值
            Runing.Increment.Log.Info($"UnitTest1.TestMethodRecoverFile(): 开始检查备份前后的文件...");
            DirectoryInfo backupAfterDir = new DirectoryInfo("../test/Target");
            FileInfo[] backupAfterFiles = backupAfterDir.GetFiles("*", SearchOption.AllDirectories);
            Assert.IsTrue(backupBeforeMD5.Count == backupAfterFiles.Length);

            for (int i = 0; i < backupAfterFiles.Length; i++)
            {
                FileInfo file = backupAfterFiles[i];
                Assert.IsTrue(backupBeforeMD5[file.FullName] == MD5Helper.FileMD5(file.FullName));
            }

            string str2 = File.ReadAllText(new FileInfo("../test/Target/TestFile.txt").FullName);
            Assert.IsTrue(str2 == "12345");
        }

        [TestMethod]
        public void TestMethodNoServer()
        {
            ResetLog(false);//重设日志

            bool isDone = false;
            Runing.Increment.Log.Info("UnitTest1.TestMethodNoServer(): http://127.0.0.1:11122");

            IDF.Update("http://127.0.0.1:11122/test/IDFTest.zip", "../test/Temp", "../test/Target", "../test/Backup")
            .OnMoveFileDone((obj, success) => { isDone = true; })
            .OnDownloadSuccess((obj) => { obj.MoveFile(); })
            .OnError((e) => { isDone = true; })
            .Go();

            while (!isDone) { Thread.Sleep(50); }
            isDone = false;

            Runing.Increment.Log.Info("UnitTest1.TestMethodNoServer(): http://baidu.com/IDFTest.zip");
            IDF.Update("http://baidu.com/IDFTest.zip", "../test/Temp", "../test/Target", "../test/Backup")
            .OnMoveFileDone((obj, success) => { isDone = true; })
            .OnDownloadSuccess((obj) => { obj.MoveFile(); })
            .OnError((e) => { isDone = true; })
            .Go();

            while (!isDone) { Thread.Sleep(50); }
            isDone = false;

            Runing.Increment.Log.Info("UnitTest1.TestMethodNoServer(): http://www.google.com");
            IDF.Update("http://www.google.com/IDFTest.zip", "../test/Temp", "../test/Target", "../test/Backup")
            .OnMoveFileDone((obj, success) => { isDone = true; })
            .OnDownloadSuccess((obj) => { obj.MoveFile(); })
            .OnError((e) => { isDone = true; })
            .Go();

            while (!isDone) { Thread.Sleep(50); }
            isDone = false;
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