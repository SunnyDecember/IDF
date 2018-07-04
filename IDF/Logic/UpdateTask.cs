using Ionic.Zip;
using JumpKick.HttpLib;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using xuexue.flie;

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

        public void DownLoad()
        {
            //开始异步处理
            StartDownLoad();
        }

        public void MoveFile()
        {
            //开始异步处理
        }

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

        private async Task StartDownLoad()
        {
            bool isDone = false;
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
                localFolder.InitWithFolderConfig(fis);

                //检查本地哪些文件需要下载
                localFolder.CheekNeedDownload();

                //EventDownSuccess(this, collection, stream);
                isDone = true;//下载完成
            }).OnFail((e) =>
            {
                EventError?.Invoke(e);
            }).Go();

            //一直卡在这里等待上面的方法执行完成
            while (!isDone) { await Task.Delay(1); }
            isDone = false;

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
            if (!localFileItem.IsNeedDownload)//之前的MD5比对不需要下载就不下了
            {
                return;
            }

            FileItem fileItem = localFileItem.fileItem;

            if (File.Exists(localFileItem.tempFilePath))//如果已经下过一个了
            {
                if (MD5Helper.FileMD5(localFileItem.tempFilePath) == localFileItem.fileItem.MD5)
                {
                    //那么这个文件就不需要下载了
                    return;
                }
            }

            //如果这个临时文件所在的上级文件夹不存在那么就创建
            FileInfo fi = new FileInfo(localFileItem.tempFilePath);
            if (!fi.Directory.Exists)
            {
                Directory.CreateDirectory(fi.Directory.FullName);
            }

            bool isDone = false;
            FileStream fs = null;
            try
            {
                fs = new FileStream(localFileItem.tempFilePath, FileMode.OpenOrCreate);
                if (fs.Length >= localFileItem.fileItem.size)//说明这个文件肯定是错误的
                {
                    fs.Close();
                    fs = new FileStream(localFileItem.tempFilePath, FileMode.Create);
                }

                Http.Get(fileItem.url).OnMake((req) =>
                {
                    req.AddRange(fs.Length);
                }).OnSuccess((WebHeaderCollection collection, Stream stream) =>
                {
                    try
                    {
                        fs.Seek(fs.Length, SeekOrigin.Begin);
                        CopyStream(stream, fs);
                        fs.Close();
                    }
                    catch (Exception e)
                    {
                        Log.Error("UpdateTask.DownLoadOneFile():下载异常:" + e.Message);
                        //EventError?.Invoke(e); //暂时不传出事件了
                    }
                    isDone = true;
                }).OnFail((e) =>
                {
                    Log.Error("UpdateTask.DownLoadOneFile():下载异常:" + e.Message);
                    //EventError?.Invoke(e); //暂时不传出事件了
                    isDone = true;
                }).Go();

                while (!isDone)
                {
                    await Task.Delay(1);
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
            bool isCorrect = true;
            foreach (var item in localFolder.fileItemClientDict.Values)
            {
                if (MD5Helper.FileMD5(item.tempFilePath) != item.fileItem.MD5)
                {
                    isCorrect = false;
                    File.Delete(item.tempFilePath);
                }
            }
            return isCorrect;
        }
    }
}