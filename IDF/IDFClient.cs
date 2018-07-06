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
    }
}