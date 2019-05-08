using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace Download
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Process current = Process.GetCurrentProcess();
                bool newinstance = false;
                while (!newinstance)
                {
                    newinstance = true;
                    Process[] processes = Process.GetProcessesByName(current.ProcessName);
                    //遍历正在有相同名字运行的例程  
                    foreach (Process process in processes)
                    {
                        //忽略现有的例程  
                        if (process.Id != current.Id)
                        {
                            //确保例程从EXE文件运行  
                            if (System.Reflection.Assembly.GetExecutingAssembly().Location.Replace("/", "\\") == current.MainModule.FileName)
                            {
                                //返回另一个例程实例  
                                current = process;
                                newinstance = false;
                                break;
                            }
                        }
                    }
                }
                if (newinstance)
                {
                    bool isAutoRun = false;
                    if (args != null && args.Length > 0 && args[0].Equals("-a"))
                    {
                        isAutoRun = true;
                    }
                    ConfigManagement.Path = Application.StartupPath + "\\Config.ini";
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new FormMain(isAutoRun));
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                {
                    msg = ex.InnerException.Message;
                }
                //if (MessageBox.Show(msg + "\r\n\r\n是否重启程序？", "异常", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                //{
                ConfigManagement.IniWriteValue("Log", "Error_" + DateTime.Now.ToString("yyyyMMdd"), msg);
                Application.Restart();
                //}
            }
        }
    }
}
