using Ionic.Zip;
using JumpKick.HttpLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using xuexue.file;

namespace Runing.Increment
{
    public class DownloadTask
    {
        internal DownloadTask(LocalSetting fcc, string xmlLocalURL)
        {
            localFolder = new LocalFolder(fcc);
            this.xmlLocalURL = xmlLocalURL;
            this.fcc = fcc;
        }

        /// <summary>
        /// 本地设置的一个记录，它记录了用户设置的临时文件夹位置，目标文件夹位置，备份文件夹位置等等。
        /// </summary>
        public LocalSetting fcc;

        /// <summary>
        /// 把XML保存在本地的哪条路径
        /// </summary>
        string xmlLocalURL;

        /// <summary>
        /// 本地文件夹数据，它对应一个OriginFolder数据
        /// </summary>
        private LocalFolder localFolder;

        private event Action<Exception> EventError = null;

        private event Action<DownloadTask> EventDownloadSuccess = null;

        /// <summary>
        /// 增量更新过程中出现了错误,参数是传出来一个Exception。
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DownloadTask OnError(Action<Exception> action)
        {
            EventError += action;
            return this;
        }

        /// <summary>
        /// 设置下载到临时文件全部成功，这里可以开始移动文件了。
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public DownloadTask OnDownloadSuccess(Action<DownloadTask> action)
        {
            EventDownloadSuccess += action;
            return this;
        }

        public void Go()
        {
            Log.Debug("DownloadTask.Go():当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
            //开始异步处理
            StartDownLoad();
        }

        /// <summary>
        /// 保存XML到本地
        /// </summary>
        /// <param name="stream"></param>
        void SaveXML(Stream stream)
        {
            stream.Position = 0;
            FileInfo xmlFile = new FileInfo(xmlLocalURL);
            
            if (!Directory.Exists(xmlFile.Directory.FullName))
            {
                Directory.CreateDirectory(xmlFile.Directory.FullName);
            }

            using (FileStream fileStream = xmlFile.OpenWrite())
            {
                CopyStream(stream, fileStream);
            }
        }

        private async void StartDownLoad()
        {
            Log.Debug("DownloadTask.StartDownLoad():当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);

            int isDone = 0;
            Interlocked.Exchange(ref isDone, 0);

            //下载ConfigXML(异步的)
            Http.Get(fcc.xmlUrl).OnSuccess((WebHeaderCollection collection, Stream stream) =>
            {
                try
                {
                    var xml = XmlHelper.CreatXml();

                    if (fcc.xmlUrl.EndsWith(".zip"))
                    {
                        MemoryStream ms = new MemoryStream();
                        CopyStream(stream, ms);

                        MemoryStream xmlms = new MemoryStream();
                        ms.Position = 0;
                        ZipFile zip = ZipFile.Read(ms);
                        ZipEntry ze = zip.Entries.First();//第一个实体
                        ze.Extract(xmlms);
                        xmlms.Position = 0;
                        xml.Load(xmlms); //从下载文件流中读xml
                        SaveXML(xmlms);
                    }
                    else
                    {
                        xml.Load(stream); //从下载文件流中读xml
                        SaveXML(stream);
                    }
                    Log.Info("DownloadTask.StartDownLoad():下载xml成功!");
                    OriginFolder fis = new OriginFolder();
                    var node = xml.DocumentElement.SelectSingleNode("./" + typeof(OriginFolder).Name);
                    fis.FromXml(node);//从xml文件根节点反序列化

                    //使用服务器上下载下来的来初始化客户端的
                    localFolder.InitWithFolderConfig(fis.fileItemDict);

                    //检查本地哪些文件需要下载
                    localFolder.CheekNeedDownload();

                    Interlocked.Increment(ref isDone);//标记为1，下载xml完成
                }
                catch (Exception e)
                {
                    Log.Error($"DownloadTask.StartDownLoad():下载xml异常,{fcc.xmlUrl} - " + e.Message);
                    try
                    {
                        EventError?.Invoke(e);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("DownloadTask.StartDownLoad():执行用户事件EventError异常:" + ex.Message);
                    }
                    Interlocked.Decrement(ref isDone);//也标记它非零了, -1
                }
            }).OnFail((e) =>
            {
                Log.Warning($"DownloadTask.StartDownLoad():OnFail()下载xml失败,{fcc.xmlUrl} - " + e.Message);
                try { EventError?.Invoke(e); }
                catch (Exception ex) { Log.Warning("DownloadTask.StartDownLoad():执行用户事件EventError异常:" + ex.Message); }
                Interlocked.Decrement(ref isDone);//也标记它非零了, -1
            }).Go();

            //一直卡在这里等待上面的方法执行完成
            while (true)
            {
                Log.Debug("DownloadTask.StartDownLoad():await Task.Delay(1)之前 当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                await Task.Delay(1);
                //await WaitDelay();//这里使用Task.Delay和这个函数都会导致线程改变
                Log.Debug("DownloadTask.StartDownLoad():await Task.Delay(1)之后 当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                if (isDone > 0)
                    break;

                if (isDone < 0)
                    return;//这里下载xml失败了，那这个函数不需要往下走了
            }

            int errorCount = 0;
            while (errorCount < 5)//只重试5次
            {
                //遍历每一项下载
                foreach (var kvp in localFolder.fileItemClientDict)
                {
                    await DownLoadOneFile(kvp.Value);
                }

                if (CheckTempFileCorrect())//如果确定已经下载ok了
                {
                    break;
                }
                errorCount++;
            }

            //重试超过五次是下载失败
            if (errorCount >= 5)
            {
                Log.Warning($"DownloadTask.StartDownLoad():下载文件失败,重试超过5次!");
                try { EventError?.Invoke(new Exception(fcc.xmlUrl)); }
                catch (Exception ex) { Log.Warning("DownloadTask.StartDownLoad():执行用户事件EventError异常:" + ex.Message); }
                return;
            }

            //执行下载完成事件
            if (EventDownloadSuccess != null)
            {
                try { EventDownloadSuccess(this); }
                catch (Exception e)
                {
                    Log.Error("DownloadTask.StartProc():执行用户事件EventDownloadSuccess异常:" + e.Message);
                }
            }
        }

        /// <summary>
        /// 由一个FileItemTask下载一项文件的方法
        /// </summary>
        /// <param name="localFileItem"></param>
        /// <returns></returns>
        private async Task DownLoadOneFile(LocalFileItem localFileItem)
        {
            if (!localFileItem.IsNeedDownload)//之前的MD5比对不需要下载就不下了
            {
                return;
            }
            Log.Info("DownloadTask.DownLoadOneFile():开始下载文件项" + localFileItem.fileItem.relativePath);
            FileItem fileItem = localFileItem.fileItem;

            if (File.Exists(localFileItem.tempFilePath))//如果已经下过一个了
            {
                if (MD5Helper.Compare(localFileItem.tempFilePath, localFileItem.fileItem.MD5, localFileItem.fileItem.size))
                {
                    //那么这个文件就不需要下载了
                    Log.Info("DownloadTask.DownLoadOneFile():存在一致的临时文件" + localFileItem.fileItem.relativePath);
                    localFileItem.IsNeedDownload = false;//标记它不用下载了
                    return;
                }
                else
                {
                    File.Delete(localFileItem.tempFilePath);//如果下好的临时文件MD5不一致，那么就删除这个以前的临时文件
                }
            }

            //如果这个临时文件所在的上级文件夹不存在那么就创建
            FileHelper.CheckCreateParentDir(localFileItem.tempFilePath);

            int isDone = 0;
            Interlocked.Exchange(ref isDone, 0);

            FileStream fs = null;

            //需要下载的文件是空文件，不下载，本地直接创建一个就行了。
            if (0 == localFileItem.fileItem.size)
            {
                fs = new FileStream(localFileItem.tempFilePath, FileMode.OpenOrCreate);
                fs.Close();
                return;
            }

            try
            {
                //下载文件的后缀加上.temp
                fs = new FileStream(localFileItem.tempFilePath + ".temp", FileMode.OpenOrCreate);

                Log.Info("DownloadTask.DownLoadOneFile():开始http连接...");
                Http.Get(fileItem.url)
                .OnMake((req) =>
                {
                    req.AddRange(fs.Length);//设置断点续传
                })
                .OnSuccess((WebHeaderCollection collection, Stream stream) =>
                {
                    try
                    {
                        fs.Seek(fs.Length, SeekOrigin.Begin);
                        CopyStream(stream, fs);
                        Log.Info("DownloadTask.DownLoadOneFile():下载成功!");
                    }
                    catch (Exception e)
                    {
                        Log.Error("DownloadTask.DownLoadOneFile():下载异常:" + e.Message);
                        //EventError?.Invoke(e); //暂时不传出事件了
                    }
                    fs.Close();
                    //isDone = true;
                    Interlocked.Increment(ref isDone);//下载完成
                })
                .OnFail((e) =>
                {
                    if (fs != null)
                        fs.Close();

                    Log.Error("DownloadTask.DownLoadOneFile():下载异常:" + e.Message);
                    //EventError?.Invoke(e); //暂时不传出事件了

                    //isDone = true;
                    Interlocked.Increment(ref isDone);//下载完成
                }).Go();

                while (true)
                {
                    Log.Debug("DownloadTask.DownLoadOneFile():await Task.Delay(1)之前 当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                    await Task.Delay(1);
                    Log.Debug("DownloadTask.DownLoadOneFile():await Task.Delay(1)之后 当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                    //await WaitDelay();
                    if (isDone > 0)
                        break;
                }
                //下载成功后把文件名的.temp去掉
                File.Move(localFileItem.tempFilePath + ".temp", localFileItem.tempFilePath);
            }
            catch (Exception e)
            {
                Log.Error("DownloadTask.DownLoadOneFile():下载异常:" + e.Message);
                if (fs != null)
                { fs.Close(); }
            }
        }

        /// <summary>
        /// 比对当前临时文件夹的文件是不是真的正确了
        /// </summary>
        /// <returns></returns>
        private bool CheckTempFileCorrect()
        {
            bool isCorrect = true;
            foreach (var item in localFolder.fileItemClientDict.Values)
            {
                if (item.IsNeedDownload)
                {
                    Log.Info($"DownloadTask.CheckTempFileCorrect():检查文件项{item.tempFilePath} -> 需要下载！");
                    if (!File.Exists(item.tempFilePath))
                    {
                        Log.Info("DownloadTask.CheckTempFileCorrect():错误->检查文件项文件不存在" + item.tempFilePath);
                        isCorrect = false;
                    }
                    else
                    {
                        //计算当前的MD5，确保文件正确
                        if (MD5Helper.Compare(item.tempFilePath, item.fileItem.MD5, item.fileItem.size))
                        {
                            item.IsNeedDownload = false;//如果文件一致了那么认为它不再需要下载了
                        }
                        else
                        {
                            Log.Info("DownloadTask.CheckTempFileCorrect():错误->检查文件项不正确，删除文件" + item.tempFilePath);
                            File.Delete(item.tempFilePath);
                            isCorrect = false;
                        }
                    }
                }
                else
                {
                    Log.Info($"DownloadTask.CheckTempFileCorrect():检查文件项{item.tempFilePath} -> 不需要下载...");
                }
            }
            if (isCorrect)
                Log.Info("DownloadTask.CheckTempFileCorrect():检查所有文件项正确！下载完成...");
            else
                Log.Info("DownloadTask.CheckTempFileCorrect():继续下载未完成的文件...");
            return isCorrect;
        }

        /// <summary>
        /// 拷贝两个流
        /// </summary>
        /// <param name="instream"></param>
        /// <param name="outstream"></param>
        private void CopyStream(Stream instream, Stream outstream)
        {
            const int bufferLen = 1024;
            byte[] buffer = new byte[bufferLen];
            int count = 0;
            while ((count = instream.Read(buffer, 0, bufferLen)) > 0)
            {
                outstream.Write(buffer, 0, count);
                outstream.Flush();
            }
        }

        /// <summary>
        /// 等待一下子
        /// </summary>
        /// <returns></returns>
        private Task WaitDelay()
        {
            return Task.Run(() =>
            {
                Thread.Sleep(1);
            });
        }
    }
}
