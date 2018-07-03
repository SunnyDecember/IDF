using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Runing.Increment
{
    public class FileItem
    {
        /// <summary>
        /// 服务器的目录
        /// </summary>
        public string url;

        /// <summary>
        /// 相对路径
        /// </summary>
        public string relativePath;

        /// <summary>
        /// 这个目标文件的hash
        /// </summary>
        public string MD5;

        /// <summary>
        /// 文件大小,Byte为单位
        /// </summary>
        public long size;


        /// <summary>
        /// 从一个xml节点对自己的字段赋值
        /// </summary>
        /// <param name="element">输入的用来读取的xml节点</param>
        public void FromXml(XmlElement element)
        {
            this.url = element.SelectToString("./url");
            this.relativePath = element.SelectToString("./relativePath");
            this.MD5 = element.SelectToString("./MD5");
            this.size = element.SelectToLong("./size");
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

            child = node.AppendChild("relativePath", this.relativePath);
            child.AddComment("相对路径");

            child = node.AppendChild("MD5", this.MD5);
            child.AddComment("MD5值");

            child = node.AppendChild("size", this.size);
            child.AddComment("文件大小Byte");

            return node;
        }
    }
}
