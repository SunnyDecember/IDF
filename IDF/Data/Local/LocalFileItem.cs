namespace Runing.Increment
{
    /// <summary>
    /// 本地文件项
    /// </summary>
    internal class LocalFileItem
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

        //其他客户端使用的进度状态记录
    }
}