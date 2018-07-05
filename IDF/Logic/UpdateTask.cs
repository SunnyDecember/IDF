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
    public class UpdateTask
    {
        internal UpdateTask(LocalSetting fcc)
        {
            localFolder = new LocalFolder(fcc);

            this.fcc = fcc;
        }

        /// <summary>
        /// 本地设置的一个记录
        /// </summary>
        public LocalSetting fcc;

        /// <summary>
        ///
        /// </summary>
        private LocalFolder localFolder;

        private event Action<Exception> EventError = null;

        private event Action<UpdateTask> EventDownloadSuccess = null;

        private event Action<UpdateTask, bool> EventMoveDone = null;

        public UpdateTask OnError(Action<Exception> action)
        {
            EventError += action;
            return this;
        }

        public UpdateTask OnDownloadSuccess(Action<UpdateTask> action)
        {
            EventDownloadSuccess += action;
            return this;
        }

        public UpdateTask OnMoveFileDone(Action<UpdateTask, bool> action)
        {
            EventMoveDone += action;
            return this;
        }

        #region move file

        /// <summary>
        /// 从临时文件移动到目标文件
        /// </summary>
        /// <returns></returns>
        public void MoveFile()
        {
            //清空备份列表的记录
            backupFileList.Clear();

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

                    //创建目标文件的目录。
                    FileInfo targetFile = new FileInfo(localFileItem.targetFilePath);
                    if (!targetFile.Directory.Exists)
                    {
                        Directory.CreateDirectory(targetFile.Directory.FullName);
                    }

                    //从临时文件移动到目标文件
                    if (File.Exists(localFileItem.tempFilePath))
                    {
                        Log.Info($"UpdateTask.MoveFile(): 移动临时文件到目标文件夹下 {localFileItem.fileItem.relativePath}");
                        File.Move(localFileItem.tempFilePath, localFileItem.targetFilePath);
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

                //操作目标文件发生异常，把备份文件恢复到目标文件。
                RecoverFile();
                if (null != EventMoveDone)
                    EventMoveDone(this, false);

                CompareMD5WithFrontBackTargetFile();
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

        /// <summary>
        /// 对备份前后的目标文件md5比较
        /// </summary>
        /// <returns></returns>
        private bool CompareMD5WithFrontBackTargetFile()
        {
            bool isCorrect = true;
            foreach (var item in localFolder.fileItemClientDict.Values)
            {
                if (!File.Exists(item.targetFilePath))
                {
                    Log.Warning("UpdateTask.CompareMD5WithFrontBackTargetFile(): 目标文件不存在 " + item.targetFilePath);
                    continue;
                }

                if (item.lastTargetMD5 != MD5Helper.FileMD5(item.targetFilePath))
                {
                    isCorrect = false;
                    Log.Warning("UpdateTask.CompareMD5WithFrontBackTargetFile(): 备份前目标文件和恢复后的MD5不一致 " + item.targetFilePath);
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

                if (item.fileItem.MD5 != MD5Helper.FileMD5(item.targetFilePath))
                {
                    isCorrect = false;
                    Log.Warning("UpdateTask.CheckTargetFileMD5WithXML(): 目标文件和XML不一致 " + item.targetFilePath);
                }
            }

            return isCorrect;
        }

        /// <summary>
        /// 备份的文件
        /// </summary>
        private List<LocalFileItem> backupFileList = new List<LocalFileItem>();

        /// <summary>
        /// 尝试把目标文件剪切到备份文件夹下,如果目标文件存在才会移动备份
        /// </summary>
        /// <param name="localFileItem"></param>
        private void TryBackupOneFile(LocalFileItem localFileItem)
        {
            try
            {
                //删除备份文件
                if (File.Exists(localFileItem.backupFilePath))
                    File.Delete(localFileItem.backupFilePath);

                //创建备份文件的目录
                FileInfo backupFile = new FileInfo(localFileItem.backupFilePath);
                if (!backupFile.Directory.Exists)
                    Directory.CreateDirectory(backupFile.Directory.FullName);

                //把需要备份的文件移动到备份文件夹。
                if (File.Exists(localFileItem.targetFilePath))
                {
                    File.Move(localFileItem.targetFilePath, localFileItem.backupFilePath);
                    Log.Info($"UpdateTask.TryBackupOneFile(): 移动目标文件到备份文件夹 {localFileItem.fileItem.relativePath}");
                }

                //记录备份文件
                backupFileList.Add(localFileItem);
            }
            catch (Exception e)
            {
                Log.Error($"UpdateTask.TryBackupOneFile(): 移动目标文件{localFileItem.fileItem.relativePath}到备份文件夹时异常:" + e.Message);
            }
        }

        /// <summary>
        /// 把备份文件恢复到目标文件。
        /// </summary>
        private void RecoverFile()
        {
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
        }

        #endregion move file

        #region download file

        public void DownLoad()
        {
            Log.Info("UpdateTask.DownLoad():当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
            //开始异步处理
            StartDownLoad();
            Log.Info("UpdateTask.DownLoad():当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
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

        private async void StartDownLoad()
        {
            Log.Info("UpdateTask.StartDownLoad():当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);

            int isDone = 0;
            Interlocked.Exchange(ref isDone, 0);

            //下载ConfigXML(异步的)
            Http.Get(fcc.xmlUrl).OnSuccess((WebHeaderCollection collection, Stream stream) =>
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
                }
                else
                {
                    xml.Load(stream); //从下载文件流中读xml
                }

                OriginFolder fis = new OriginFolder();
                var node = xml.DocumentElement.SelectSingleNode("./" + typeof(OriginFolder).Name);
                fis.FromXml(node);//从xml文件根节点反序列化

                //使用服务器上下载下来的来初始化客户端的
                localFolder.InitWithFolderConfig(fis.fileItemDict);

                //检查本地哪些文件需要下载
                localFolder.CheekNeedDownload();

                Interlocked.Increment(ref isDone);//下载xml完成
            }).OnFail((e) =>
            {
                EventError?.Invoke(e);
            }).Go();

            Log.Info("UpdateTask.StartDownLoad():等待xml文件下载ok，当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);

            //一直卡在这里等待上面的方法执行完成
            while (true)
            {
                Log.Info("UpdateTask.StartDownLoad():await Task.Delay(1)之前 当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                await Task.Delay(1);
                //await WaitDelay();//这里使用Task.Delay和这个函数都会导致线程改变
                Log.Info("UpdateTask.StartDownLoad():await Task.Delay(1)之后 当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                if (isDone > 0)
                    break;
            }

            Log.Info("UpdateTask.StartDownLoad():xml文件下载完成，当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);

            while (true)
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
            Log.Info("UpdateTask.DownLoadOneFile():当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);

            if (!localFileItem.IsNeedDownload)//之前的MD5比对不需要下载就不下了
            {
                return;
            }
            Log.Info("UpdateTask.DownLoadOneFile():开始下载文件项" + localFileItem.fileItem.relativePath);
            FileItem fileItem = localFileItem.fileItem;

            if (File.Exists(localFileItem.tempFilePath))//如果已经下过一个了
            {
                if (MD5Helper.FileMD5(localFileItem.tempFilePath) == localFileItem.fileItem.MD5)
                {
                    //那么这个文件就不需要下载了
                    Log.Info("UpdateTask.DownLoadOneFile():存在一致的临时文件" + localFileItem.fileItem.relativePath);
                    return;
                }
            }

            //如果这个临时文件所在的上级文件夹不存在那么就创建
            FileInfo fi = new FileInfo(localFileItem.tempFilePath);
            if (!fi.Directory.Exists)
            {
                Directory.CreateDirectory(fi.Directory.FullName);
            }

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
                fs = new FileStream(localFileItem.tempFilePath, FileMode.OpenOrCreate);
                if (fs.Length >= localFileItem.fileItem.size)//说明这个文件肯定是错误的
                {
                    fs.Close();
                    fs = new FileStream(localFileItem.tempFilePath, FileMode.Create);
                }
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
                        CopyStream(stream, fs);
                        Log.Info("UpdateTask.DownLoadOneFile():下载成功!");
                    }
                    catch (Exception e)
                    {
                        Log.Error("UpdateTask.DownLoadOneFile():下载异常:" + e.Message);
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

                    Log.Error("UpdateTask.DownLoadOneFile():下载异常:" + e.Message);
                    //EventError?.Invoke(e); //暂时不传出事件了

                    //isDone = true;
                    Interlocked.Increment(ref isDone);//下载完成
                }).Go();

                while (true)
                {
                    Log.Info("UpdateTask.DownLoadOneFile():await Task.Delay(1)之前 当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                    await Task.Delay(1);
                    Log.Info("UpdateTask.DownLoadOneFile():await Task.Delay(1)之后 当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);
                    //await WaitDelay();
                    if (isDone > 0)
                        break;
                }
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
            Log.Info("UpdateTask.CheckTempFileCorrect():当前执行线程id=" + Thread.CurrentThread.ManagedThreadId);

            bool isCorrect = true;
            foreach (var item in localFolder.fileItemClientDict.Values)
            {
                if (item.IsNeedDownload)
                {
                    if (!File.Exists(item.tempFilePath))
                    {
                        Log.Info("UpdateTask.CheckTempFileCorrect():检查文件项不存在" + item.tempFilePath);
                        isCorrect = false;
                    }
                    else
                    {
                        if (MD5Helper.FileMD5(item.tempFilePath) != item.fileItem.MD5)//计算当前的MD5，确保文件正确
                        {
                            Log.Info("UpdateTask.CheckTempFileCorrect():检查文件项不正确，删除文件" + item.tempFilePath);
                            File.Delete(item.tempFilePath);
                            isCorrect = false;
                        }
                    }
                }
            }
            if (isCorrect)
                Log.Info("UpdateTask.CheckTempFileCorrect():检查所有文件项正确！下载完成...");
            else
                Log.Info("UpdateTask.CheckTempFileCorrect():继续下载未完成的文件...");
            return isCorrect;
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