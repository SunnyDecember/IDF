using System;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Runing.Increment;

namespace UnitTest
{
    [TestClass]
    public class UnitTestXML
    {
        [TestMethod]
        public void TestMethod1()
        {
            DownloadFile downloadFile = new DownloadFile();
            downloadFile.fileDoneSize = 1024;
            downloadFile.fileName = "abc";
            downloadFile.relativePath = "/abc";

            var xml = XmlHelper.CreatXml();

            //把自己写到一个xml节点（序列化）
            XmlElement node = downloadFile.ToXml(xml.DocumentElement);

            DownloadFile downloadFile2 = new DownloadFile();

            //从node节点反序列化
            downloadFile2.FromXml(node);
            
            Assert.IsTrue(downloadFile2.fileDoneSize == downloadFile.fileDoneSize);
            Assert.IsTrue(downloadFile2.fileName == downloadFile.fileName);
            Assert.IsTrue(downloadFile2.relativePath == downloadFile.relativePath);

        }
    }
}
