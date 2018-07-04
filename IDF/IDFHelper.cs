using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using xuexue.file;
using Ionic.Zip;

namespace Runing.Increment
{
    /// <summary>
    /// 增量更新下载的辅助方法
    /// </summary>
    public static class IDFHelper
    {
        /// <summary>
        /// 根据一个文件夹生成xml的配置文件(或zip文件)
        /// </summary>
        /// <param name="dirPath">要计算的文件夹</param>
        /// <param name="urlDom">url文件服务器上的路径,形如http://mr.xuexuesoft.com:8010/UpdateFile/</param>
        /// <param name="xmlPath">最后的结果xml文件路径(支持.xml和.zip)</param>
        public static void CreatConfigFileWithXml(string dirPath, string urlDom, string xmlPath)
        {
            OriginFolder folder = new OriginFolder();

            DirectoryInfo di = new DirectoryInfo(dirPath);
            FileInfo[] fis = di.GetFiles("*", SearchOption.AllDirectories);
            for (int i = 0; i < fis.Length; i++)
            {
                //遍历每一个文件
                FileInfo fi = fis[i];
                FileItem fileItem = new FileItem() { size = fi.Length };

                fileItem.relativePath = FileHelper.Relative(fi.FullName, dirPath);//求这个文件相对路径
                fileItem.url = urlDom + fileItem.relativePath;
                fileItem.MD5 = MD5Helper.FileMD5(fi.FullName);//计算md5

                //写到文件夹记录
                folder.Add(fileItem);
            }

            var xml = XmlHelper.CreatXml();
            folder.ToXml(xml.DocumentElement);//文件夹内容挂到xml文件的根节点

            FileInfo xmlFileInfo = new FileInfo(xmlPath);//要保存的目录
            if (!xmlFileInfo.Directory.Exists)
            {
                Directory.CreateDirectory(xmlFileInfo.Directory.FullName);
            }

            if (xmlFileInfo.Extension == ".zip")//如果是zip那就保存zip文件
            {
                MemoryStream ms = new MemoryStream();
                xml.Save(ms);

                using (ZipFile zip = new ZipFile(Encoding.Default))
                {
                    ZipEntry zipEntry = new ZipEntry();
                    zip.AddEntry(xmlFileInfo.Name.Replace(".zip", ".xml"), ms.ToArray());//把xml的内存流写到zip文件
                    zip.Save(xmlFileInfo.FullName);
                }
            }
            else
            {
                xml.Save(xmlFileInfo.FullName);//保存.xml文件
            }
        }
    }
}
