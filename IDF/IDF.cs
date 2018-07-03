using System;
using System.Xml;

/*  Author      :   Runing
 *  Time        :   18.7.3
 *  Description :   增量下载文件的入口
 */

namespace Runing.Increment
{
    public class IDF
    {
        static IDF _instance;

        public static IDF Instance
        {
            get
            {
                if (null != _instance)
                {
                    _instance = new IDF();
                }
                return _instance;
            }
        }

        public IDF()
        {
            
        }

        /// <summary>
        /// 在localDirt中生成XML文件
        /// </summary>
        /// <param name="localDirt">本地的目录,在此目录会生成一个XML</param>
        /// <param name="serverDirt">服务器的目录</param>
        public void GenerateXMLWith(string localDirt, string serverDirt)
        {
            XmlDocument xmlDocument = XmlHelper.CreatXml();
            
        }
    }
}

