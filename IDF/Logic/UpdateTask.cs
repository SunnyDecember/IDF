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
    /// <summary>
    /// 一个更新任务对象,IDFClient中会创建它。
    /// </summary>
    public class UpdateTask
    {
        internal UpdateTask(LocalSetting setting)
        {
            localFolder = new LocalFolder(setting);

            this.setting = setting;
        }

        /// <summary>
        /// 本地设置的一个记录，它记录了用户设置的临时文件夹位置，目标文件夹位置，备份文件夹位置等等。
        /// </summary>
        public LocalSetting setting;

        /// <summary>
        /// 远程服务器上的文件夹文件内容，暴露出来提供使用
        /// </summary>
        public OriginFolder originFolder;

        /// <summary>
        /// 本地文件夹数据，它对应一个OriginFolder数据
        /// </summary>
        public LocalFolder localFolder;

        /// <summary>
        /// 备份的文件
        /// </summary>
        private List<LocalFileItem> backupFileList = new List<LocalFileItem>();

        /// <summary>
        /// 当需要移动临时文件到目标文件夹，那么这里记录这些文件
        /// </summary>
        private List<LocalFileItem> hasMoveFileList = new List<LocalFileItem>();

        private event Action<Exception> EventError = null;

        private event Action<UpdateTask> EventDownloadSuccess = null;

        private event Action<UpdateTask, bool> EventMoveDone = null;

        /// <summary>
        /// 增量更新过程中出现了错误,参数是传出来一个Exception。
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public UpdateTask OnError(Action<Exception> action)
        {
            EventError += action;
            return this;
        }

        /// <summary>
        /// 设置下载到临时文件全部成功，这里可以开始移动文件了。
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public UpdateTask OnDownloadSuccess(Action<UpdateTask> action)
        {
            EventDownloadSuccess += action;
            return this;
        }

        /// <summary>
        /// 设置移动文件结束的回调函数，移动结束可能仍然是失败的。
        ///  其中Action的第二个参数bool，如果移动成功返回true，发生错误返回false。
        /// </summary>
        /// <param name="action">回调委托，bool表示是否发生错误</param>
        /// <returns></returns>
        public UpdateTask OnMoveFileDone(Action<UpdateTask, bool> action)
        {
            EventMoveDone += action;
            return this;
        }

        #region move file

        /// <summary>
        /// 从临时文件移动到目标文件，这是一个阻塞函数。如果在主线程中可能应该异步调用执行。但是这样最后会导致事件无法回到主线程。
        /// </summary>
        /// <returns></returns>
        public void MoveFile()
        {
            Log.Info($"UpdateTask.MoveFile(): 开始移动文件！！！");

            //清空备份列表的记录
            backupFileList.Clear();
            hasMoveFileList.Clear();

            try
            {
                foreach (var kv in localFolder.fileItemClientDict)
                {
                    LocalFileItem localFileItem = kv.Value;

                    //如果目标文件是否已经存在，目标文件和临时文件的md5是否一致。都不成立就备份目标文件到备份文件夹下。
                    if (File.Exists(localFileItem.targetFilePath) &&
                          //localFileItem.fileItem.MD5 ==  MD5Helper.FileMD5(localFileItem.targetFilePath))//<-----这里重新计算了一次MD5
                          localFileItem.fileItem.MD5 == localFileItem.lastTargetMD5)//暂时不再重新计算一次了
                    {
                        Log.Info($"UpdateTask.MoveFile(): 不需要移动文件 {localFileItem.targetFilePath}");
                        continue;
                    }
                    else
                    {
                        //尝试把目标文件剪切到备份文件夹下,如果目标文件存在才会移动备份
                        if (File.Exists(localFileItem.targetFilePath))
                        {
                            TryBackupOneFile(localFileItem);
                        }
                    }

                    //检查创建目标文件的所在目录。
                    FileHelper.CheckCreateParentDir(localFileItem.targetFilePath);

                    //从临时文件移动到目标文件
                    if (File.Exists(localFileItem.tempFilePath))
                    {
                        Log.Info($"UpdateTask.MoveFile(): 移动临时文件到目标文件夹下-> {localFileItem.fileItem.relativePath}");
                        File.Move(localFileItem.tempFilePath, localFileItem.targetFilePath);
                        hasMoveFileList.Add(localFileItem);
                    }
                    else
                    {
                        Log.Error($"UpdateTask.MoveFile(): 源路径不存在 : {localFileItem.tempFilePath}, 无法剪切。");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"UpdateTask.MoveFile(): 移动文件异常 {e}");
                OnException();
            }

            //移动结束后，比对目标文件和xml的md5是否一致
            if (CheckTargetFileMD5WithXML())
            {
                Log.Info("UpdataTask.MoveFile(): 移动文件成功");

                if (null != EventMoveDone)
                    EventMoveDone(this, true);
            }
            else
            {
                if (null != EventError)
                    EventError(new Exception());
                Log.Info("UpdataTask.MoveFile(): 移动文件出错，移动后文件和服务器的不一致");
            }
        }

        private void OnException()
        {
            //操作目标文件发生异常，把备份文件恢复到目标文件。
            RecoverFile();
            if (null != EventMoveDone)
                EventMoveDone(this, false);

            if (CheckTargetFileMD5BeforeAfter())
            {
                Log.Info("UpdataTask.OnException(): 备份前后的文件MD5一致");
            }
        }

        /// <summary>
        /// 用整个更新操作前后的目标文件md5比较,确保整个操作前后的文件一致。
        /// </summary>
        /// <returns></returns>
        public bool CheckTargetFileMD5BeforeAfter()
        {
            bool isCorrect = true;
            foreach (var item in localFolder.fileItemClientDict.Values)
            {
                if (!File.Exists(item.targetFilePath))
                {
                    continue;
                }

                if (item.lastTargetMD5 != MD5Helper.FileMD5(item.targetFilePath))
                {
                    isCorrect = false;
                    Log.Error("UpdateTask.CheckTargetFileMD5BeforeAfter(): 操作前目标文件和恢复后的MD5不一致 " + item.targetFilePath);
                }
            }

            return isCorrect;
        }

        /// <summary>
        /// 用目标文件和服务器下载下来的xml比较md5
        /// </summary>
        private bool CheckTargetFileMD5WithXML()
        {
            bool isCorrect = true;
            foreach (var item in localFolder.fileItemClientDict.Values)
            {
                if (!File.Exists(item.targetFilePath))
                {
                    Log.Warning("UpdateTask.CheckTargetFileMD5WithXML(): 目标文件不存在 " + item.targetFilePath);
                    continue;
                }

                if (!MD5Helper.Compare(item.targetFilePath, item.fileItem.MD5, item.fileItem.size))
                {
                    isCorrect = false;
                    Log.Warning("UpdateTask.CheckTargetFileMD5WithXML(): 目标文件和XML不一致 " + item.targetFilePath);
                }
            }

            return isCorrect;
        }

        /// <summary>
        /// 尝试把目标文件剪切到备份文件夹下,如果目标文件存在才会移动备份
        /// </summary>
        /// <param name="localFileItem"></param>
        private void TryBackupOneFile(LocalFileItem localFileItem)
        {
            try
            {
                //检查创建备份文件的所在目录
                FileHelper.CheckCreateParentDir(localFileItem.backupFilePath);

                if (File.Exists(localFileItem.targetFilePath))
                {
                    //尝试先删除备份文件
                    if (File.Exists(localFileItem.backupFilePath))
                        File.Delete(localFileItem.backupFilePath);

                    //把需要备份的文件移动到备份文件夹。
                    File.Move(localFileItem.targetFilePath, localFileItem.backupFilePath);
                    Log.Info($"UpdateTask.TryBackupOneFile(): 移动目标文件到备份文件夹 {localFileItem.fileItem.relativePath}");

                    //记录备份文件
                    backupFileList.Add(localFileItem);
                }
            }
            catch (Exception e)
            {
                Log.Error($"UpdateTask.TryBackupOneFile(): 移动目标文件{localFileItem.fileItem.relativePath}到备份文件夹时异常:" + e.Message);
            }
        }

        /// <summary>
        /// 把备份文件恢复到目标文件。
        /// </summary>
        public void RecoverFile()
        {
            //先把改动过的文件移动回临时文件夹
            for (int i = 0; i < hasMoveFileList.Count; i++)
            {
                LocalFileItem localFileItem = hasMoveFileList[i];

                if (!File.Exists(localFileItem.targetFilePath))
                    continue;

                if (File.Exists(localFileItem.tempFilePath))
                    File.Delete(localFileItem.tempFilePath);

                //把目标文件移动到临时文件
                File.Move(localFileItem.targetFilePath, localFileItem.tempFilePath);
                Log.Info($"UpdateTask.RecoverFile(): 把目标文件移动到临时文件 {localFileItem.fileItem.relativePath}");
            }

            //遍历备份文件列表
            for (int i = 0; i < backupFileList.Count; i++)
            {
                LocalFileItem localFileItem = backupFileList[i];

                if (!File.Exists(localFileItem.backupFilePath))
                    continue;

                if (File.Exists(localFileItem.targetFilePath))
                    File.Delete(localFileItem.targetFilePath);

                //剪切备份文件到目标文件
                File.Move(localFileItem.backupFilePath, localFileItem.targetFilePath);
                Log.Info($"UpdateTask.RecoverFile(): 把备份文件恢复到目标文件 {localFileItem.fileItem.relativePath}");
            }

            Log.Info($"UpdateTask.RecoverFile(): 备份文件恢复到目标文件结束");
            hasMoveFileList.Clear();
            backupFileList.Clear();
        }

        #endregion move file

        #region download file

        /// <summary>
        /// 这函数使用async-await实现异步的逻辑，保证了文件一个一个的下载
        /// </summary>
        public void Go()
        {
            Log.Debug("UpdateTask.Go():当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
            //开始异步处理
            StartDownLoad();
        }

        private async void StartDownLoad()
        {
            Log.Debug("UpdateTask.StartDownLoad():当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);

            int isDone = 0;
            Interlocked.Exchange(ref isDone, 0);

            Exception lastException = null;
            //下载ConfigXML(异步的)
            Http.Get(setting.xmlUrl).OnSuccess((WebHeaderCollection collection, Stream stream) =>
            {
                try
                {
                    var xml = XmlHelper.CreatXml();

                    if (setting.xmlUrl.EndsWith(".zip"))
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
                    }
                    else
                    {
                        xml.Load(stream); //从下载文件流中读xml
                    }
                    Log.Info("UpdateTask.StartDownLoad():下载xml成功!");
                    originFolder = new OriginFolder();
                    var node = xml.DocumentElement.SelectSingleNode("./" + typeof(OriginFolder).Name);
                    originFolder.FromXml(node);//从xml文件根节点反序列化

                    //使用服务器上下载下来的来初始化客户端的
                    localFolder.InitWithFolderConfig(originFolder.fileItemDict);

                    //检查本地哪些文件需要下载
                    localFolder.CheekNeedDownload();

                    Interlocked.Increment(ref isDone);//标记为1，下载xml完成
                }
                catch (Exception e)
                {
                    Log.Error($"UpdateTask.StartDownLoad():下载xml异常,{setting.xmlUrl} - " + e.Message);
                    lastException = e;//记录异常信息
                    Interlocked.Decrement(ref isDone);//也标记它非零了, -1
                }
            }).OnFail((e) =>
            {
                Log.Warning($"UpdateTask.StartDownLoad():OnFail()下载xml失败,{setting.xmlUrl} - " + e.Message);
                lastException = e;//记录异常信息
                Interlocked.Decrement(ref isDone);//也标记它非零了, -1
            }).Go();

            //一直卡在这里等待上面的方法执行完成
            while (true)
            {
                Log.Debug("UpdateTask.StartDownLoad():await Task.Delay(1)之前 当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                await Task.Delay(1);
                //await WaitDelay();//这里使用Task.Delay和这个函数都会导致线程改变
                Log.Debug("UpdateTask.StartDownLoad():await Task.Delay(1)之后 当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                if (isDone > 0)
                    break;

                if (isDone < 0)
                {
                    try { EventError?.Invoke(lastException); }
                    catch (Exception ex) { Log.Warning("UpdateTask.StartDownLoad():执行用户事件EventError异常:" + ex.Message); }
                    return;//这里下载xml失败了，那这个函数不需要往下走了
                }
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
                Log.Warning($"UpdateTask.StartDownLoad():下载文件失败,重试超过5次!");
                try { EventError?.Invoke(new Exception(setting.xmlUrl)); }
                catch (Exception ex) { Log.Warning("UpdateTask.StartDownLoad():执行用户事件EventError异常:" + ex.Message); }
                return;
            }

            //执行下载完成事件
            if (EventDownloadSuccess != null)
            {
                try { EventDownloadSuccess(this); }
                catch (Exception e)
                {
                    Log.Error("UpdateTask.StartProc():执行用户事件EventDownloadSuccess异常:" + e.Message);
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
            Log.Info("UpdateTask.DownLoadOneFile():开始下载文件项: " + localFileItem.fileItem.url);
            FileItem fileItem = localFileItem.fileItem;

            if (File.Exists(localFileItem.tempFilePath))//如果已经下过一个了
            {
                if (MD5Helper.Compare(localFileItem.tempFilePath, localFileItem.fileItem.MD5, localFileItem.fileItem.size))
                {
                    //那么这个文件就不需要下载了
                    Log.Info("UpdateTask.DownLoadOneFile():存在一致的临时文件" + localFileItem.fileItem.relativePath);
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

            //需要下载的文件是空文件，不用下载，本地直接创建一个空文件就行了。
            if (localFileItem.fileItem.size == 0)
            {
                fs = new FileStream(localFileItem.tempFilePath, FileMode.Create);
                fs.Close();
                return;
            }

            try
            {
                //下载文件的后缀加上.temp
                fs = new FileStream(localFileItem.tempFilePath + ".temp", FileMode.OpenOrCreate);

                Log.Info("UpdateTask.DownLoadOneFile():开始http连接...");
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
                        CopyStream(stream, fs);//阻塞的下载?但是进入OnSuccess会是一个异步小线程
                        Log.Info("UpdateTask.DownLoadOneFile():下载成功!");
                    }
                    catch (Exception e)
                    {
                        Log.Error($"UpdateTask.DownLoadOneFile.CopyStream():{fileItem.url}下载异常:" + e.Message);
                        //EventError?.Invoke(e); //暂时不传出事件了
                    }
                    fs.Close();

                    Interlocked.Increment(ref isDone);//下载完成
                })
                .OnFail((e) =>
                {
                    if (fs != null)
                        fs.Close();

                    Log.Error($"UpdateTask.DownLoadOneFile.OnFail():{fileItem.url}下载异常:" + e.Message);
                    //EventError?.Invoke(e); //暂时不传出事件了

                    Interlocked.Increment(ref isDone);//下载完成
                }).Go();

                while (true)
                {
                    Log.Debug("UpdateTask.DownLoadOneFile():await Task.Delay(1)之前 当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                    await Task.Delay(1);
                    Log.Debug("UpdateTask.DownLoadOneFile():await Task.Delay(1)之后 当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                    //await WaitDelay();
                    if (isDone > 0)
                        break;
                }
                //下载成功后把文件名的.temp去掉
                File.Move(localFileItem.tempFilePath + ".temp", localFileItem.tempFilePath);
            }
            catch (Exception e)
            {
                Log.Error("UpdateTask.DownLoadOneFile():下载异常:" + e.Message);
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
                    Log.Info($"UpdateTask.CheckTempFileCorrect():检查文件项{item.tempFilePath} -> 需要下载！");
                    if (!File.Exists(item.tempFilePath))
                    {
                        Log.Info("UpdateTask.CheckTempFileCorrect():错误->检查文件项文件不存在" + item.tempFilePath);
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
                            Log.Info("UpdateTask.CheckTempFileCorrect():错误->检查文件项不正确，删除文件" + item.tempFilePath);
                            File.Delete(item.tempFilePath);
                            isCorrect = false;
                        }
                    }
                }
                else
                {
                    Log.Info($"UpdateTask.CheckTempFileCorrect():检查文件项{item.tempFilePath} -> 不需要下载...");
                }
            }
            if (isCorrect)
                Log.Info("UpdateTask.CheckTempFileCorrect():检查所有文件项正确！下载完成...");
            else
                Log.Info("UpdateTask.CheckTempFileCorrect():继续下载未完成的文件...");
            return isCorrect;
        }

        /// <summary>
        /// 阻塞的拷贝两个流
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

        #endregion download file

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