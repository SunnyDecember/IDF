using JumpKick.HttpLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Runing.Increment
{
    public class UpdateTask
    {
        public string xmlUrl;

        /// <summary>
        /// 所有的这次要下载文件(网上下的xml中的所有项)，key为相对路径
        /// </summary>
        public Dictionary<string, FileItemClient> dict = new Dictionary<string, FileItemClient>();

        event Action<Exception> EventError;

        event Action<UpdateTask> EventDownSuccess;

        public UpdateTask Go()
        {
            //开始异步处理
            Task.Run(StartDownLoad);//?好像是这样写，要试试
            return this;
        }

        public UpdateTask OnError(Action<Exception> action)
        {
            EventError += action;
            return this;
        }

        public UpdateTask Onuccess(Action<UpdateTask> action)
        {
            EventDownSuccess += action;
            return this;
        }

        private async Task StartDownLoad()
        {
            bool isDone = false;
            //下载ConfigXML(异步的)
            Http.Get(xmlUrl).OnSuccess((WebHeaderCollection a, Stream b) =>
            {
                var xml = XmlHelper.CreatXml();
                xml.Load(b); //从下载文件流中读xml

                FileItems fis = new FileItems();
                fis.FromXml(xml.DocumentElement);//从xml文件根节点反序列化

                foreach (var item in fis._fileItemDict)
                {
                    FileItemClient fit = new FileItemClient() { fileItem = item.Value };
                    fit.tempFilePath = "xxxxxxxxx";
                    dict.Add(item.Key, fit);
                }

                isDone = true;//下载完成
            }).OnFail((e) => {
                EventError?.Invoke(e);
            });

            //一直卡在这里等待上面的方法执行完成
            while (!isDone) { await Task.Delay(1); }
            isDone = false;

            //遍历每一项下载
            foreach (var kvp in dict)
            {
                await DownLoadOneFile(kvp.Value);
            }

            //执行下载完成事件
            if (EventDownSuccess != null)
            {
                try { EventDownSuccess(this); }
                catch (Exception e)
                {
                    Log.Error("UpdateTask.StartProc():执行用户事件EventDownSuccess异常:" + e.Message);
                }
            }
        }


        /// <summary>
        /// 由一个FileItemTask下载一项文件的方法
        /// </summary>
        /// <param name="fit"></param>
        /// <returns></returns>
        private async Task DownLoadOneFile(FileItemClient fit)
        {
            bool isDone = false;
            Http.Get(fit.fileItem.url).DownloadTo(fit.tempFilePath).OnSuccess((WebHeaderCollection a, Stream b) =>
            {
                isDone = true;//下载完成
            }).OnFail((e)=> {
                EventError?.Invoke(e);
            });

            while (!isDone)
            {
                await Task.Delay(1);
            }
        }
    }
}
