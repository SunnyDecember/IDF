using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using xuexue.file;

namespace Runing.Increment
{
    /// <summary>
    /// 本地文件夹
    /// </summary>
    internal class LocalFolder
    {
        public LocalFolder(LocalSetting localSetting)
        {
            //设置本地设置
            this.localSetting = localSetting;
        }

        /// <summary>
        /// 相对路径作为key值
        /// </summary>
        public Dictionary<string, LocalFileItem> fileItemClientDict = new Dictionary<string, LocalFileItem>();

        /// <summary>
        /// 本地设置
        /// </summary>
        public LocalSetting localSetting;

        /// <summary>
        /// 使用一个服务器上下下来的FolderConfig,加上本地配置LocalSetting。
        /// 初始化自己的整个内容
        /// </summary>
        /// <param name="fileItemDict"></param>
        public void InitWithFolderConfig(ConcurrentDictionary<string, FileItem> fileItemDict)
        {
            //先clear
            fileItemClientDict.Clear();

            foreach (var fileItem in fileItemDict.Values)
            {
                LocalFileItem fic = new LocalFileItem();
                fic.fileItem = fileItem;
                fic.tempFilePath = Path.Combine(localSetting.tempFolderPath, fic.fileItem.relativePath);
                fic.targetFilePath = Path.Combine(localSetting.targetFolderPath, fic.fileItem.relativePath);
                fic.backupFilePath = Path.Combine(localSetting.backupFolderPath, fic.fileItem.relativePath);

                fileItemClientDict.Add(fic.fileItem.relativePath, fic);
            }
        }

        /// <summary>
        /// 计算本地文件的MD5，标记哪些文件需要下载
        /// </summary>
        public void CheekNeedDownload()
        {
            foreach (LocalFileItem item in fileItemClientDict.Values)
            {
                item.IsNeedDownload = true;//默认标记它需要下载
                if (File.Exists(item.targetFilePath))
                {
                    item.lastTargetMD5 = MD5Helper.FileMD5(item.targetFilePath);//记录一下更新前的这个文件的md5
                    if (item.lastTargetMD5 == item.fileItem.MD5)
                    {
                        item.IsNeedDownload = false;//只有这一种情况不下载
                    }
                }
                //就算是空文件也是要下载的
            }
        }
    }
}