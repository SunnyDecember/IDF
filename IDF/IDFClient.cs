using System.IO;

/*  Author      :   Runing
 *  Time        :   18.7.3
 *  Description :   增量下载文件的入口
 */

namespace Runing.Increment
{
    /// <summary>
    /// 增量更新
    /// </summary>
    public class IDFClient
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static IDFClient Instance { get; } = new IDFClient();

        public IDFClient()
        {
        }

        /// <summary>
        /// 开始异步下载资源
        /// </summary>
        /// <param name="xmlServerURL">服务器的XML路径</param>
        /// <param name="tempFolderPath">临时保存的路径</param>
        /// <param name="targetFolderPath">需要被替换的文件路径</param>
        /// <param name="backupFolderPath">备份的文件夹路径</param>
        public UpdateTask Go(string xmlServerURL, string tempFolderPath, string targetFolderPath, string backupFolderPath)
        {
            var ls = new LocalSetting()
            {
                tempFolderPath = tempFolderPath,
                targetFolderPath = targetFolderPath,
                xmlUrl = xmlServerURL,
                backupFolderPath = backupFolderPath
            };

            DirectoryInfo dif = new DirectoryInfo(ls.targetFolderPath);
            ls.targetFolderPath = dif.FullName;//确保转换成完整路径
            dif = new DirectoryInfo(ls.tempFolderPath);
            ls.tempFolderPath = dif.FullName;//确保转换成完整路径
            dif = new DirectoryInfo(ls.backupFolderPath);
            ls.backupFolderPath = dif.FullName;//确保转换成完整路径

            UpdateTask updateTask = new UpdateTask(ls);
            updateTask.DownLoad();

            return updateTask;
        }
    }
}