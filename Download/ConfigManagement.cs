using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace Download
{
    /// <summary>
    /// Config 管理
    /// </summary>
    public class ConfigManagement
    {
        private static string path = "";     //INI文件名

        public static string Path
        {
            get { return path; }
            set { path = value; }
        }

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        //类的构造函数，传递INI文件名
        public static void IniWriteValue(string Section, string Key, string Value)
        {
            if (!File.Exists(path))
            {
                StreamWriter sw = new StreamWriter(path);
                sw.Flush();
                sw.Close();
            }
            WritePrivateProfileString(Section, Key, Value, path);
        }

        //读INI文件         
        public static string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(10000);
            int i = GetPrivateProfileString(Section, Key, "", temp, 10000, path);
            return temp.ToString();
        }
    }
}