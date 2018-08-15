using System.IO;

/*  Author      :   Runing
 *  Time        :   18.7.3
 *  Description :   增量下载文件的入口
 */

namespace Runing.Increment
{
    /// <summary>
    /// 增量更新类
    /// </summary>
    public class IDF
    {
        /// <summary>
        /// 开始异步更新操作,用法IDF.Update(url,path,path,path).Go();
        /// </summary>
        /// <param name="xmlServerURL">服务器的XML路径</param>
        /// <param name="tempFolderPath">临时保存的路径</param>
        /// <param name="targetFolderPath">需要被替换的文件路径</param>
        /// <param name="backupFolderPath">备份的文件夹路径</param>
        /// <returns></returns>
        public static UpdateTask Update(string xmlServerURL, string tempFolderPath, string targetFolderPath, string backupFolderPath)
        {
            LocalSetting ls = new LocalSetting()
            {
                xmlUrl = xmlServerURL,
                targetFolderPath = new DirectoryInfo(targetFolderPath).FullName,//确保转换成完整路径
                tempFolderPath = new DirectoryInfo(tempFolderPath).FullName,//确保转换成完整路径
                backupFolderPath = new DirectoryInfo(backupFolderPath).FullName//确保转换成完整路径
            };

            UpdateTask updateTask = new UpdateTask(ls);
            return updateTask;
        }

        /// <summary>
        /// 只做下载，包括下载xml和对应的资源.
        /// 用法IDF.Download(url,path,path,path).Go();
        /// </summary>
        /// <param name="xmlServerURL">服务器的XML路径</param>
        /// <param name="xmlLocalURL">把XML保存在本地的哪条路径</param>
        /// <param name="tempFolderPath">临时保存的路径</param>
        /// <param name="targetFolderPath">需要被替换的文件路径</param>
        /// <param name="backupFolderPath">备份的文件夹路径</param>
        /// <returns></returns>
        public static DownloadTask Download(string xmlServerURL, string xmlLocalURL, string tempFolderPath, string targetFolderPath)//, string backupFolderPath)
        {
            LocalSetting ls = new LocalSetting()
            {
                xmlUrl = xmlServerURL,
                targetFolderPath = new DirectoryInfo(targetFolderPath).FullName,//确保转换成完整路径
                tempFolderPath = new DirectoryInfo(tempFolderPath).FullName,//确保转换成完整路径
                //backupFolderPath = new DirectoryInfo(backupFolderPath).FullName//确保转换成完整路径
                backupFolderPath = ""
            };

            xmlLocalURL = new DirectoryInfo(xmlLocalURL).FullName;//确保转换成完整路径
            DownloadTask downloadTask = new DownloadTask(ls, xmlLocalURL);
            return downloadTask;
        }

        /// <summary>
        /// 移动文件，包括从临时文件夹到目标文件夹，目标文件夹到备份文件夹。如果移动失败，会恢复目标文件夹。
        /// IDF.Move(url,path,path,path).Go();
        /// </summary>
        /// <param name="xmlLocalURL">本地的XML路径</param>
        /// <param name="tempFolderPath">临时保存的路径</param>
        /// <param name="targetFolderPath">需要被替换的文件路径</param>
        /// <param name="backupFolderPath">备份的文件夹路径</param>
        /// <returns></returns>
        public static MoveFile Move(string xmlLocalURL, string tempFolderPath, string targetFolderPath, string backupFolderPath)
        {
            LocalSetting ls = new LocalSetting()
            {
                xmlUrl = new DirectoryInfo(xmlLocalURL).FullName,
                targetFolderPath = new DirectoryInfo(targetFolderPath).FullName,//确保转换成完整路径
                tempFolderPath = new DirectoryInfo(tempFolderPath).FullName,//确保转换成完整路径
                backupFolderPath = new DirectoryInfo(backupFolderPath).FullName//确保转换成完整路径
            };

            MoveFile moveFile = new MoveFile(ls);
            return moveFile;
        }
    }
}