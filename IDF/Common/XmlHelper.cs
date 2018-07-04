using System;
using System.IO;
using System.Xml;

namespace Runing.Increment
{
    /// <summary>
    /// 一个简单的自己定义的xml序列化反序列化接口
    /// </summary>
    internal interface IXMLSerialize
    {
        /// <summary>
        /// 从一个xml节点对自己的字段赋值
        /// </summary>
        /// <param name="node">输入的用来读取的xml节点</param>
        void FromXml(XmlNode node);

        /// <summary>
        /// 把自己序列化成xml节点，然后挂在某个父节点上
        /// </summary>
        /// <param name="parent">要挂上的父节点</param>
        /// <returns>自己的xml节点</returns>
        XmlElement ToXml(XmlElement parent);
    }

    /// <summary>
    /// 一些xml相关的公用方法
    /// </summary>
    public static class XmlHelper
    {
        #region 字段方法

        public static string SelectToString(this XmlNode element, string xpath)
        {
            XmlNode child = element.SelectSingleNode(xpath);
            if (child != null)
            {
                return child.InnerText;
            }
            else
            {
                return default(string);
            }
        }

        public static bool SelectToBool(this XmlNode element, string xpath)
        {
            XmlNode child = element.SelectSingleNode(xpath);
            if (child != null)
            {
                return Convert.ToBoolean(child.InnerText);
            }
            else
            {
                return default(bool);
            }
        }

        public static int SelectToInt32(this XmlNode element, string xpath)
        {
            XmlNode child = element.SelectSingleNode(xpath);
            if (child != null)
            {
                return Convert.ToInt32(child.InnerText);
            }
            else
            {
                return default(int);
            }
        }

        public static float SelectToFloat(this XmlNode element, string xpath)
        {
            XmlNode child = element.SelectSingleNode(xpath);
            if (child != null)
            {
                return Convert.ToSingle(child.InnerText);
            }
            else
            {
                return default(float);
            }
        }

        public static long SelectToLong(this XmlNode element, string xpath)
        {
            XmlNode child = element.SelectSingleNode(xpath);
            if (child != null)
            {
                return Convert.ToInt64(child.InnerText);
            }
            else
            {
                return default(long);
            }
        }

        public static XmlElement AppendChild(this XmlElement node, string childName, string data)
        {
            XmlDocument xmldoc = node.OwnerDocument;
            XmlElement child = xmldoc.CreateElement(childName);

            child.InnerText = data;
            node.AppendChild(child);
            return child;
        }

        public static XmlElement AppendChild(this XmlElement node, string childName, bool data)
        {
            XmlDocument xmldoc = node.OwnerDocument;
            XmlElement child = xmldoc.CreateElement(childName);

            child.InnerText = Convert.ToString(data);
            node.AppendChild(child);
            return child;
        }

        public static XmlElement AppendChild(this XmlElement node, string childName, int data)
        {
            XmlDocument xmldoc = node.OwnerDocument;
            XmlElement child = xmldoc.CreateElement(childName);

            child.InnerText = Convert.ToString(data);
            node.AppendChild(child);
            return child;
        }

        public static XmlElement AppendChild(this XmlElement node, string childName, float data)
        {
            XmlDocument xmldoc = node.OwnerDocument;
            XmlElement child = xmldoc.CreateElement(childName);

            child.InnerText = Convert.ToString(data);
            node.AppendChild(child);
            return child;
        }

        public static XmlElement AppendChild(this XmlElement node, string childName, long data)
        {
            XmlDocument xmldoc = node.OwnerDocument;
            XmlElement child = xmldoc.CreateElement(childName);

            child.InnerText = Convert.ToString(data);
            node.AppendChild(child);
            return child;
        }

        #endregion 字段方法

        /// <summary>
        /// 给一个节点加上注释并且插入到它前面
        /// </summary>
        /// <param name="tn">要注释的节点</param>
        /// <param name="strComment">注内容</param>
        public static void AddComment(this XmlNode tn, string strComment)
        {
            XmlDocument xmldoc = tn.OwnerDocument;
            XmlComment comment = xmldoc.CreateComment(strComment);
            tn.ParentNode.InsertBefore(comment, tn);//必须由它的紧接一级的上一级来插入
        }

        /// <summary>
        /// 把XmlDocument输出成string
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <returns></returns>
        public static string WriteXmlToString(this XmlDocument xmlDoc)
        {
            MemoryStream stream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(stream, null);
            writer.Formatting = Formatting.Indented;
            xmlDoc.Save(writer);
            StreamReader sr = new StreamReader(stream, System.Text.Encoding.UTF8);
            stream.Position = 0;
            string xmlString = sr.ReadToEnd();
            sr.Close();
            stream.Close();
            return xmlString;
        }

        /// <summary>
        /// 创建一个xml文档，默认添加一个root节点
        /// </summary>
        /// <returns></returns>
        public static XmlDocument CreatXml()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement root = xmlDoc.CreateElement("root");
            xmlDoc.AppendChild(root);
            return xmlDoc;
        }
    }
}