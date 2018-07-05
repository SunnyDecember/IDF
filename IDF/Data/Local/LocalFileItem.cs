namespace Runing.Increment
{
    /// <summary>
    /// 本地文件项
    /// </summary>
    public class LocalFileItem
    {
        /// <summary>
        /// 原始的文件项
        /// </summary>
        public FileItem fileItem;

        /// <summary>
        /// 这一项文件是否需要下载，MD5比对之后标记.
        /// 在FolderConfigClient类的CheekNeedDownload()函数中被标记。
        /// </summary>
        public bool IsNeedDownload;

        /// <summary>
        /// 临时下载到本地的文件路径,是完整路径.
        /// </summary>
        public string tempFilePath;

        /// <summary>
        /// 本地目标文件路径
        /// </summary>
        public string targetFilePath;

        /// <summary>
        /// 备份文件夹
        /// </summary>
        public string backupFilePath;

        /// <summary>
        /// 更新前的目标文件md5（如果更新失败，检查还原后所有的文件当前MD5值等于它）
        /// </summary>
        public string lastTargetMD5;

        //其他客户端使用的进度状态记录
    }
}