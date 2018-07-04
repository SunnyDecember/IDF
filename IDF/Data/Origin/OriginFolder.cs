using System.Collections.Concurrent;
using System.Xml;

namespace Runing.Increment
{
    /// <summary>
    /// 原始文件夹内容
    /// </summary>
    internal class OriginFolder : IXMLSerialize
    {
        /// <summary>
        /// key:文件的相对路径
        /// </summary>
        public ConcurrentDictionary<string, FileItem> fileItemDict = new ConcurrentDictionary<string, FileItem>();

        /// <summary>
        /// 增加FileItem到字典中
        /// </summary>
        /// <param name="fileItem"></param>
        public void Add(FileItem fileItem)
        {
            if (string.IsNullOrEmpty(fileItem.relativePath) || !fileItemDict.TryAdd(fileItem.relativePath, fileItem))
            {
                Log.Warning("OriginFolder.Add():存在一样的路径名字, " + fileItem.relativePath);
            }
        }

        #region xml <-> obj

        /// <summary>
        /// 读取xml节点并储存在字典中。
        /// </summary>
        /// <param name="node"></param>
        public void FromXml(XmlNode node)
        {
            fileItemDict.Clear();
            XmlNodeList childNodes = node.SelectNodes("./FileItem");
            for (int i = 0; i < childNodes.Count; i++)
            {
                FileItem fileItem = new FileItem();
                fileItem.FromXml(childNodes[i]);
                Add(fileItem);
            }
        }

        /// <summary>
        /// 从字典中读取节点，并添加到parent下面。
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public XmlElement ToXml(XmlElement parent)
        {
            XmlDocument xml = parent.OwnerDocument;
            XmlElement element = xml.CreateElement(this.GetType().Name);
            parent.AppendChild(element);

            foreach (var kv in fileItemDict)
            {
                FileItem item = kv.Value;
                item.ToXml(element);
            }
            return element;
        }

        #endregion xml <-> obj
    }
}