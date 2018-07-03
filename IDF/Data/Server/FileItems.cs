using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Runing.Increment
{
    class FileItems : IXMLSerialize
    {
        public Dictionary<string, FileItem> _fileItemDict = new Dictionary<string, FileItem>();

        public void Add(FileItem fileItem)
        {
            if (_fileItemDict.ContainsKey(fileItem.relativePath))
            {
                Log.Warning("FileItems.Add()-> 存在一样的路径名字 " + fileItem.relativePath);
            }
        }

        public void FromXml(XmlElement element)
        {
            
        }

        public XmlElement ToXml(XmlElement parent)
        {
            return null;
        }
    }
}
