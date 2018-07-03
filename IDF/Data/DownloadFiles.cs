using System.Collections.Generic;
using System.Xml;

namespace Runing.Increment
{
    /// <summary>
    /// 下载一批文件的下载过程记录数据
    /// </summary>
    public class DownloadFiles : IXMLSerialize
    {
        /// <summary>
        /// 以本地文件相对路径为key的字典，每一个本地文件路径应该不同，否则就覆盖了。
        /// </summary>
        public Dictionary<string, DownloadFile> dict = new Dictionary<string, DownloadFile>();

        /// <summary>
        /// 文件的总计数
        /// </summary>
        public int fileCount
        {
            get
            {
                return dict.Count;
            }
        }

        /// <summary>
        /// 文件完成的计数
        /// </summary>
        public int fileDoneCount
        {
            get
            {
                int count = 0;
                lock (this)//就是拿来锁字典
                {
                    foreach (var item in dict)
                    {
                        if (item.Value.isDone)
                        {
                            count++;
                        }
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// 所有文件的总大小
        /// </summary>
        public long filesSize
        {
            get
            {
                long size = 0;
                lock (this)//就是拿来锁字典
                {
                    foreach (var item in dict)
                    {
                        size += item.Value.fileSize;
                    }
                }
                return size;
            }
        }

        /// <summary>
        /// 需要下载的文件大小，用来统计下载进度
        /// </summary>
        public long needDownloadFilesSize
        {
            get
            {
                long size = 0;
                lock (this)//就是拿来锁字典
                {
                    foreach (var item in dict)
                    {
                        if (item.Value.isNeedDownload)
                            size += item.Value.fileSize;
                    }
                }
                return size;
            }
        }


        /// <summary>
        /// 所有文件完成的总大小
        /// </summary>
        public long filesDoneSize
        {
            get
            {
                //这里遍历会和下面的AddOneFile冲突
                long size = 0;
                lock (this)//就是拿来锁字典
                {
                    foreach (var item in dict)
                    {
                        size += item.Value.fileDoneSize;
                    }
                }
                return size;
            }
        }

        /// <summary>
        /// 向记录里添加一项下载文件项
        /// </summary>
        /// <param name="df">一项下载文件项</param>
        /// <returns></returns>
        public bool AddOneFile(DownloadFile df)
        {
            lock (this)//就是拿来锁字典
            {
                if (!string.IsNullOrEmpty(df.relativePath) && !dict.ContainsKey(df.relativePath))
                {
                    dict.Add(df.relativePath, df);
                    return true;
                }
                else
                {
                    Log.Warning("DownloadFiles.AddOneFile()::添加了一个重复的key值.key=" + df.fileName);
                    return false;
                }
            }
        }

        /// <summary>
        /// 是否所有下载都全部完成了
        /// </summary>
        /// <returns></returns>
        public bool isAllDone()
        {
            lock (this)//可以不加应该
            {
                foreach (var item in dict)
                {
                    if (item.Value.isDone == false)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 是否全都尝试下载过了
        /// </summary>
        /// <returns></returns>
        public bool isAllTry()
        {
            lock (this)
            {
                foreach (var item in dict)
                {
                    if (item.Value.isNeedDownload && !item.Value.isTry)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        #region xml <-> obj

        /// <summary>
        /// 从一个xml节点对自己的字段赋值
        /// </summary>
        /// <param name="element">输入的用来读取的xml节点</param>
        public void FromXml(XmlElement element)
        {
            if (dict == null)
            {
                dict = new Dictionary<string, DownloadFile>();
            }
            this.dict.Clear();

            XmlNodeList nodes = element.SelectNodes("./DownloadFile");
            for (int i = 0; i < nodes.Count; i++)
            {
                XmlElement onefile = (XmlElement)nodes[i];
                DownloadFile onefileObj = new DownloadFile();
                onefileObj.FromXml(onefile);
                AddOneFile(onefileObj);
            }
        }

        /// <summary>
        /// 把自己序列化成xml节点，然后挂在某个父节点上
        /// </summary>
        /// <param name="parent">要挂上的父节点</param>
        /// <returns>自己的xml节点</returns>
        public XmlElement ToXml(XmlElement parent)
        {
            XmlDocument xmldoc = parent.OwnerDocument;
            XmlElement node = xmldoc.CreateElement(this.GetType().Name);//官方文档提到不要对XmlElement直接实例化，应该使用Create
            parent.AppendChild(node);//node节点是自己这个类对象

            lock (this)//就是拿来锁字典
            {
                if (dict != null)
                    foreach (var kvp in dict)
                    {
                        DownloadFile onefileObj = kvp.Value;
                        onefileObj.ToXml(node);//挂在自己的node上
                    }
            }
            return node;
        }

        #endregion xml <-> obj
    }
}