using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Xml;
using JumpKick.HttpLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Runing.Increment;
using xuexue.file;

namespace UnitTest
{
    [TestClass]
    public class UnitTestXML
    {
        public UnitTestXML()
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
                Thread.Sleep(2000);
            }
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
        public void GenerateXML()
        {
            Console.WriteLine("UnitTestXML.GenerateXML():生成xml文件");
            IDFHelper.CreatConfigFileWithXml("./", "http://127.0.0.1:22333/Debug/", "../test/IDFTest.zip");

            //FileHelper.CleanDirectory("../test/Temp");

            bool isDone = false;
            IDFClient.Instance.Go("http://127.0.0.1:22333/test/IDFTest.zip", "../test/Temp", "../test/Target").OnDownloadSuccess((obj) =>
            {
                //关闭那些程序
                obj.MoveFile();

                isDone = true;
            })
            .OnError((e) => {
                isDone = true;
            });

            while (!isDone)
            {
                Thread.Sleep(1000);
            }
        }

        [TestMethod]
        public void TestMethodLog()
        {
            //输出日志到"即时窗口"，"调试->窗口->即时" 只在调试时有输出，运行没有日志
            for (int i = 0; i < 100; i++)
            {
                Debug.WriteLine("写一个日志！", "info");
                Thread.Sleep(1);
            }

        }
    }
}
