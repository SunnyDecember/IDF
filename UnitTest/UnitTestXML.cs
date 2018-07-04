using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Xml;
using JumpKick.HttpLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Runing.Increment;
using xuexue.flie;

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


        [TestMethod]
        public void TestMethod1()
        {
            string url = "http://127.0.0.1:22333/Debug/Ionic.Zip.Unity.dll";
            string file = @"E:\work_projects\IDF\UnitTest\bin\test\Temp\Ionic.Zip.Unity.dll";

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
            string file = @"E:\work_projects\IDF\UnitTest\bin\test\Temp\Ionic.Zip.Unity.dll";

            bool isDone = false;
            Http.Get(url).OnSuccess((WebHeaderCollection a, Stream s) =>
            {
                FileStream fs = File.Create(file);
                CopyStream(s, fs);
                //fs.Close();
                isDone = true;

            }).OnFail((e) =>
            {
                Console.WriteLine("UnitTestXML.TestMethod1():错误！");
            }).Go();

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

            FileHelper.CleanDirectory("../test/Temp");

            bool isDone = false;
            IDFClient.Instance.Go("http://127.0.0.1:22333/test/IDFTest.zip", "../test/Temp", "../test/Target").OnDownloadSuccess((obj) =>
            {
                //关闭那些程序
                obj.MoveFile();

                isDone = true;
            })
            .OnError((e) => { isDone = true; });

            while (!isDone)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
