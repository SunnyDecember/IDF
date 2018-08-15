using Ionic.Zip;
using JumpKick.HttpLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using xuexue.file;

namespace Runing.Increment
{
    public class MoveFile
    {
        internal MoveFile(LocalSetting fcc)
        {
            localFolder = new LocalFolder(fcc);

            this.fcc = fcc;
        }

        /// <summary>
        /// 本地设置的一个记录，它记录了用户设置的临时文件夹位置，目标文件夹位置，备份文件夹位置等等。
        /// </summary>
        public LocalSetting fcc;

        /// <summary>
        /// 备份的文件
        /// </summary>
        private List<LocalFileItem> backupFileList = new List<LocalFileItem>();

        /// <summary>
        /// 当需要移动临时文件到目标文件夹，那么这里记录这些文件
        /// 主要是为了一旦移动失败，需要把这些文件撤出来的。
        /// </summary>
        private List<LocalFileItem> hasMoveFileList = new List<LocalFileItem>();

        /// <summary>
        /// 本地文件夹数据，它对应一个OriginFolder数据
        /// </summary>
        private LocalFolder localFolder;

        private event Action<Exception> EventError = null;

        private event Action<MoveFile, bool> EventMoveDone = null;

        /// <summary>
        /// 增量更新过程中出现了错误,参数是传出来一个Exception。
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public MoveFile OnError(Action<Exception> action)
        {
            EventError += action;
            return this;
        }

        /// <summary>
        /// 设置移动文件结束的回调函数，移动结束可能仍然是失败的。
        ///  其中Action的第二个参数bool，如果移动成功返回true，发生错误返回false。
        /// </summary>
        /// <param name="action">回调委托，bool表示是否发生错误</param>
        /// <returns></returns>
        public MoveFile OnMoveFileDone(Action<MoveFile, bool> action)
        {
            EventMoveDone += action;
            return this;
        }
        
        /// <summary>
        /// 加载XML
        /// </summary>
        public XmlDocument LoadXML()
        {
            var xml = XmlHelper.CreatXml();

            try
            {
                if (fcc.xmlUrl.EndsWith(".zip"))
                {
                    ZipFile zip = ZipFile.Read(fcc.xmlUrl);
                    ZipEntry ze = zip.Entries.First();//第一个实体
                    MemoryStream xmlms = new MemoryStream();
                    ze.Extract(xmlms);
                    xmlms.Position = 0;
                    xml.Load(xmlms);
                }
                else
                {
                    xml.Load(fcc.xmlUrl);
                }
            }
            catch (Exception e)
            {
                xml = null;
                EventError?.Invoke(e);
                Log.Error($"MoveFile.LoadXML(): 加载XML异常 path:" + fcc.xmlUrl + "  exception:" + e);
            }
            
            return xml;
        }

        /// <summary>
        /// 从临时文件移动到目标文件
        /// </summary>
        /// <returns></returns>
        public void Go()
        {
            //先读取XML
            XmlDocument xml = LoadXML();

            //读取xml失败，结束移动文件流程。
            if (null == xml)
                return;

            OriginFolder fis = new OriginFolder();
            var node = xml.DocumentElement.SelectSingleNode("./" + typeof(OriginFolder).Name);
            fis.FromXml(node);//从xml文件根节点反序列化

            //初始化 LocalFolder 里面的 fileItemClientDict字典
            localFolder.InitWithFolderConfig(fis.fileItemDict);
            
            Log.Info($"MoveFile.Go(): 开始移动文件！！！");

            //清空备份列表的记录
            backupFileList.Clear();
            hasMoveFileList.Clear();

            try
            {
                foreach (var kv in localFolder.fileItemClientDict)
                {
                    LocalFileItem localFileItem = kv.Value;

                    //判断目标文件是否已经存在，目标文件和临时文件的md5是否一致。都不成立就备份目标文件到备份文件夹下。
                    if (File.Exists(localFileItem.targetFilePath) &&
                          //localFileItem.fileItem.MD5 ==  MD5Helper.FileMD5(localFileItem.targetFilePath))//<-----这里重新计算了一次MD5
                          localFileItem.fileItem.MD5 == localFileItem.lastTargetMD5)//暂时不再重新计算一次了
                    {
                        Log.Info($"MoveFile.Go(): 不需要移动文件 {localFileItem.targetFilePath}");
                        continue;
                    }
                    else
                    {
                        //尝试把目标文件剪切到备份文件夹下,如果目标文件存在才会移动备份
                        if (File.Exists(localFileItem.targetFilePath))
                        {
                            TryBackupOneFile(localFileItem);
                        }
                    }

                    //检查创建目标文件的所在目录。
                    FileHelper.CheckCreateParentDir(localFileItem.targetFilePath);

                    //从临时文件移动到目标文件
                    if (File.Exists(localFileItem.tempFilePath))
                    {
                        Log.Info($"MoveFile.Go(): 移动临时文件到目标文件夹下-> {localFileItem.fileItem.relativePath}");
                        File.Move(localFileItem.tempFilePath, localFileItem.targetFilePath);
                        hasMoveFileList.Add(localFileItem);
                    }
                    else
                    {
                        Log.Error($"MoveFile.Go(): 源路径不存在 : {localFileItem.tempFilePath}, 无法剪切。");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"MoveFile.Go(): 移动文件异常 {e}");
                OnException();
            }

            //移动结束后，匹配xml和目标文件夹，找到哪些目标文件夹中的文件不存在xml中，那么删除这些多余的文件。
            RemoveUnuseFile();

            //移动结束后，比对目标文件和xml的md5是否一致
            if (CheckTargetFileMD5WithXML())
            {
                Log.Info("MoveFile.Go(): 移动文件成功");

                if (null != EventMoveDone)
                    EventMoveDone(this, true);
            }
            else
            {
                if (null != EventError)
                    EventError(new Exception());
                Log.Info("MoveFile.Go(): 移动文件出错，移动后文件和服务器的不一致");
            }
        }


        private void OnException()
        {
            //操作目标文件发生异常，把备份文件恢复到目标文件。
            RecoverFile();
            if (null != EventMoveDone)
                EventMoveDone(this, false);

            if (CheckTargetFileMD5BeforeAfter())
            {
                Log.Info("MoveFile.OnException(): 备份前后的文件MD5一致");
            }
        }

        /// <summary>
        /// 用整个更新操作前后的目标文件md5比较,确保整个操作前后的文件一致。
        /// </summary>
        /// <returns></returns>
        bool CheckTargetFileMD5BeforeAfter()
        {
            bool isCorrect = true;
            foreach (var item in localFolder.fileItemClientDict.Values)
            {
                if (!File.Exists(item.targetFilePath))
                {
                    continue;
                }

                if (item.lastTargetMD5 != MD5Helper.FileMD5(item.targetFilePath))
                {
                    isCorrect = false;
                    Log.Error("MoveFile.CheckTargetFileMD5BeforeAfter(): 操作前目标文件和恢复后的MD5不一致 " + item.targetFilePath);
                }
            }

            return isCorrect;
        }

        /// <summary>
        /// 用目标文件和服务器下载下来的xml比较md5
        /// </summary>
        bool CheckTargetFileMD5WithXML()
        {
            bool isCorrect = true;
            foreach (var item in localFolder.fileItemClientDict.Values)
            {
                if (!File.Exists(item.targetFilePath))
                {
                    Log.Warning("MoveFile.CheckTargetFileMD5WithXML(): 目标文件不存在 " + item.targetFilePath);
                    continue;
                }

                if (!MD5Helper.Compare(item.targetFilePath, item.fileItem.MD5, item.fileItem.size))
                {
                    isCorrect = false;
                    Log.Warning("MoveFile.CheckTargetFileMD5WithXML(): 目标文件和XML不一致 " + item.targetFilePath);
                }
            }

            return isCorrect;
        }

        /// <summary>
        /// 尝试把目标文件剪切到备份文件夹下,如果目标文件存在才会移动备份
        /// </summary>
        /// <param name="localFileItem"></param>
        private void TryBackupOneFile(LocalFileItem localFileItem)
        {
            try
            {
                //检查创建备份文件的所在目录
                FileHelper.CheckCreateParentDir(localFileItem.backupFilePath);

                if (File.Exists(localFileItem.targetFilePath))
                {
                    //尝试先删除备份文件
                    if (File.Exists(localFileItem.backupFilePath))
                        File.Delete(localFileItem.backupFilePath);

                    //把需要备份的文件移动到备份文件夹。
                    File.Move(localFileItem.targetFilePath, localFileItem.backupFilePath);
                    Log.Info($"MoveFile.TryBackupOneFile(): 移动目标文件到备份文件夹 {localFileItem.fileItem.relativePath}");

                    //记录备份文件
                    backupFileList.Add(localFileItem);
                }
            }
            catch (Exception e)
            {
                Log.Error($"MoveFile.TryBackupOneFile(): 移动目标文件{localFileItem.fileItem.relativePath}到备份文件夹时异常:" + e.Message);
            }
        }

        /// <summary>
        /// 把备份文件恢复到目标文件。
        /// </summary>
        void RecoverFile()
        {
            //先把改动过的文件移动回临时文件夹
            for (int i = 0; i < hasMoveFileList.Count; i++)
            {
                LocalFileItem localFileItem = hasMoveFileList[i];

                if (!File.Exists(localFileItem.targetFilePath))
                    continue;

                if (File.Exists(localFileItem.tempFilePath))
                    File.Delete(localFileItem.tempFilePath);

                //把目标文件移动到临时文件
                File.Move(localFileItem.targetFilePath, localFileItem.tempFilePath);
                Log.Info($"MoveFile.RecoverFile(): 把目标文件移动到临时文件 {localFileItem.fileItem.relativePath}");
            }

            //遍历备份文件列表
            for (int i = 0; i < backupFileList.Count; i++)
            {
                LocalFileItem localFileItem = backupFileList[i];

                if (!File.Exists(localFileItem.backupFilePath))
                    continue;

                if (File.Exists(localFileItem.targetFilePath))
                    File.Delete(localFileItem.targetFilePath);

                //剪切备份文件到目标文件
                File.Move(localFileItem.backupFilePath, localFileItem.targetFilePath);
                Log.Info($"MoveFile.RecoverFile(): 把备份文件恢复到目标文件 {localFileItem.fileItem.relativePath}");
            }

            Log.Info($"MoveFile.RecoverFile(): 备份文件恢复到目标文件结束");
            hasMoveFileList.Clear();
            backupFileList.Clear();
        }


        #region 移除目标文件夹中的多余文件
        private List<string> removeUnuseFileList = new List<string>();

        /// <summary>
        /// 移动结束后，匹配xml和目标文件夹，找到哪些目标文件夹中的文件不存在xml中，那么删除这些多余的文件。
        /// </summary>
        void RemoveUnuseFile()
        {
            string targetFolder = localFolder.localSetting.targetFolderPath.Replace("\\", "/");
            Dictionary<string, LocalFileItem> fileItemClientDict = localFolder.fileItemClientDict;

            //如果目标路径不存在
            if (!Directory.Exists(targetFolder))
                return;

            //获取目标路径下所有的文件
            DirectoryInfo dir = new DirectoryInfo(targetFolder);
            FileInfo[] fileInfoArray = dir.GetFiles("*", SearchOption.AllDirectories);

            //在xml中找路径
            for (int i = 0; i < fileInfoArray.Length; i++)
            {
                //目标文件的相对路径(把前段路径去掉，只留相对路径，这样可以直接去字典匹配，提高查找效率)
                string targetFileRelativePath = fileInfoArray[i].FullName.Replace("\\", "/").Replace(targetFolder + "/", "");
                string fullName = fileInfoArray[i].FullName.Replace("\\", "/");

                if (!fileItemClientDict.ContainsKey(targetFileRelativePath) && File.Exists(fullName))
                {
                    removeUnuseFileList.Add(fullName);
                    Log.Info($"MoveFile.RemoveUnuseFile(): 移除目标文件夹中的多余文件, path: {fullName}");
                    File.Delete(fullName);
                }
            }
        }
        #endregion
    }
}
