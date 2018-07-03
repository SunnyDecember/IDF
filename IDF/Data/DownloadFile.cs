using System.Xml;

namespace Runing.Increment
{
    /// <summary>
    /// 下载一个文件的记录
    /// </summary>
    public class DownloadFile : IXMLSerialize
    {
        /// <summary>
        /// url
        /// </summary>
        public string url;

        /// <summary>
        ///  下载的保存位置（应该尽量统一成绝对路径）
        /// </summary>
        public string fileName;

        /// <summary>
        /// 相对路径
        /// </summary>
        public string relativePath;

        /// <summary>
        /// 这个目标文件的hash
        /// </summary>
        public string hashSHA1;

        /// <summary>
        /// 文件的实际SHA1
        /// </summary>
        public string fileSHA1;

        /// <summary>
        /// 文件大小,Byte为单位
        /// </summary>
        public long fileSize;

        /// <summary>
        /// 文件下载下来的大小
        /// </summary>
        public long fileDoneSize;

        /// <summary>
        /// 是否尝试过下载
        /// </summary>
        public bool isTry;

        /// <summary>
        /// 这个文件是否需要下载，不需要下载即为不需要更新
        /// </summary>
        public bool isNeedDownload;

        /// <summary>
        /// 是否下载已经完成
        /// </summary>
        public bool isDone;

        #region xml <-> obj

        /// <summary>
        /// 从一个xml节点对自己的字段赋值
        /// </summary>
        /// <param name="element">输入的用来读取的xml节点</param>
        public void FromXml(XmlElement element)
        {
            this.url = element.SelectToString("./url");
            this.fileName = element.SelectToString("./fileName");
            this.relativePath = element.SelectToString("./relativePath");
            this.hashSHA1 = element.SelectToString("./hashSHA1");
            this.fileSHA1 = element.SelectToString("./fileSHA1");
            this.fileSize = element.SelectToLong("./fileSize");
            this.fileDoneSize = element.SelectToLong("./fileDoneSize");
            this.isTry = element.SelectToBool("./isTry");
            this.isDone = element.SelectToBool("./isDone");
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

            XmlElement child = node.AppendChild("url", this.url);
            child.AddComment("服务器的url地址");

            child = node.AppendChild("fileName", this.fileName);
            child.AddComment("下载的保存位置");

            child = node.AppendChild("relativePath", this.relativePath);
            child.AddComment("相对路径");

            child = node.AppendChild("hashSHA1", this.hashSHA1);
            child.AddComment("哈希");

            child = node.AppendChild("fileSHA1", this.fileSHA1);
            child.AddComment("文件实际hash");

            child = node.AppendChild("fileSize", this.fileSize);
            child.AddComment("文件大小Byte");

            child = node.AppendChild("fileDoneSize", this.fileDoneSize);
            child.AddComment("文件完成Byte");

            child = node.AppendChild("isTry", this.isTry);
            child.AddComment("是否尝试下载过");

            child = node.AppendChild("isDone", this.isDone);
            child.AddComment("是否下载完成");

            return node;
        }

        #endregion xml <-> obj
    }
}