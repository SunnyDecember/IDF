using System;
using System.Xml;

/*  Author      :   Runing
 *  Time        :   18.7.3
 *  Description :   增量下载文件的入口
 */

namespace Runing.Increment
{
    public class IDFClient
    {
        static IDFClient _instance = new IDFClient();

        public static IDFClient Instance
        {
            get
            {
                return _instance;
            }
        }

        public IDFClient()
        {

        }


        public void ReadINI(string iniFilePath)
        {
            //设置 xml 文件 url
            
            //设置本地下载缓存路径
        }

        public void Go( )
        {
            UpdateTask updateTask = new UpdateTask();
         
        }
    }
}

