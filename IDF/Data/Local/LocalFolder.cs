using System.Collections.Generic;
using System.IO;
using xuexue.flie;

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
        /// <param name="folderConfig"></param>
        public void InitWithFolderConfig(OriginFolder folderConfig)
        {
            //先clear
            fileItemClientDict.Clear();

            foreach (var fileItem in folderConfig.fileItemDict.Values)
            {
                LocalFileItem fic = new LocalFileItem();
                fic.fileItem = fileItem;
                fic.tempFilePath = Path.Combine(localSetting.tempFolderPath, fic.fileItem.relativePath);
                fic.targetFilePath = Path.Combine(localSetting.targetFolderPath, fic.fileItem.relativePath);

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
                if (!File.Exists(item.targetFilePath) || MD5Helper.FileMD5(item.targetFilePath) != item.fileItem.MD5)
                {
                    item.IsNeedDownload = true;//标记它需要下载
                }
                else
                {
                    item.IsNeedDownload = false;
                }
            }
        }
    }
}