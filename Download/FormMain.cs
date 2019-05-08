using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Linq;

namespace Download
{
    public partial class FormMain : Form
    {
        bool isAutoRun = false;

        public FormMain(bool pIsAutoRun)
        {
            InitializeComponent();
            isAutoRun = pIsAutoRun;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                string temp = ConfigManagement.IniReadValue(this.GetType().FullName, "Port");
                if (!string.IsNullOrEmpty(temp))
                {
                    textBox1.Text = temp;
                }

                //this.checkReg();
            }
            catch
            { }

            if (isAutoRun)
            {
                button1_Click(null, null);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                button1.Enabled = false;
                textBox1.ReadOnly = true;
                int port = int.Parse(textBox1.Text);
                int tryCount = 0;
                Begin:
                linkLabel1.Text = "http://localhost:" + port + "/";
                HttpListener httpListener = new HttpListener();
                try
                {
                    httpListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                    httpListener.Prefixes.Add("http://*:" + port + "/");
                    httpListener.Start();
                }
                catch (Exception ex2)
                {
                    string msg = ex2.Message;
                    if (msg.StartsWith("拒绝访问"))
                    {
                        throw new Exception("请尝试:右键->以管理员身份运行");
                    }
                    else if (msg.Contains("进程无法访问"))
                    {
                        tryCount++;
                        if (tryCount < 50)
                        {
                            port++;
                            goto Begin;
                        }
                        else
                        {
                            throw new Exception("换个端口");
                        }
                    }
                    throw ex2;
                }
                new Thread(new ThreadStart(delegate
                {
                    while (true)
                    {
                        try
                        {
                            HttpListenerContext httpListenerContext = httpListener.GetContext();
                            new Thread(new ParameterizedThreadStart((input) =>
                                {
                                    HttpListenerContext ctx = (HttpListenerContext)input;
                                    HttpListenerRequest request = ctx.Request;
                                    string pram = null;
                                    string result = null;
                                    bool isDesc = true;
                                    bool sortByName = false;
                                    if (request.HttpMethod == "GET")
                                    {
                                        pram = System.Web.HttpUtility.UrlDecode(request.Url.LocalPath);
                                        //路径:request.Url.AbsolutePath;
                                        //请求参数:request.QueryString;
                                        if (request.QueryString["sort"] != null && !request.QueryString["sort"].ToUpper().Equals("DESC"))
                                        {
                                            isDesc = false;
                                        }
                                        if (request.QueryString["sorttype"] != null && request.QueryString["sorttype"].ToUpper().Equals("NAME"))
                                        {
                                            sortByName = true;
                                        }
                                        string dirFullPath = Application.StartupPath + pram;
                                        bool isLink = false;
                                        if (File.Exists(dirFullPath + ".lnk"))
                                        {
                                            isLink = true;
                                        }
                                        bool isRedirect = false;
                                        if (!isLink && !Directory.Exists(dirFullPath))
                                        {
                                            string[] partPath = pram.Split(new string[] { "/", "\\" }, StringSplitOptions.RemoveEmptyEntries);
                                            if (partPath != null && partPath.Length > 0)
                                            {
                                                string tempPath = Application.StartupPath;
                                                foreach (string item in partPath)
                                                {
                                                    tempPath += "/" + item;
                                                    if (File.Exists(tempPath + ".lnk"))
                                                    {
                                                        IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                                                        IWshRuntimeLibrary.IWshShortcut iws = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(tempPath + ".lnk");
                                                        //文件夹
                                                        tempPath = iws.TargetPath;
                                                        isRedirect = true;
                                                    }
                                                }
                                                if (isRedirect)
                                                {
                                                    dirFullPath = tempPath;
                                                }
                                            }
                                        }
                                        bool isBigFile = false;
                                        bool isDonload = false;
                                        if (System.IO.File.Exists(dirFullPath))
                                        {
                                            isDonload = true;
                                            #region 文件下载
                                            //文件输出
                                            FileStream fileStream = new System.IO.FileStream(dirFullPath, System.IO.FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
                                            if (fileStream != null && fileStream.Length <= int.MaxValue)
                                            {
                                                int byteLength = (int)fileStream.Length;
                                                byte[] fileBytes = new byte[byteLength];
                                                fileStream.Read(fileBytes, 0, byteLength);
                                                fileStream.Close();
                                                fileStream.Dispose();
                                                //数据流模式
                                                httpListenerContext.Response.ContentType = "application/octet-stream ";
                                                //文件大小
                                                httpListenerContext.Response.ContentLength64 = byteLength;
                                                //返回状态
                                                httpListenerContext.Response.StatusCode = 200;
                                                try
                                                {
                                                    //下载
                                                    httpListenerContext.Response.OutputStream.Write(fileBytes, 0, byteLength);
                                                }
                                                catch
                                                {
                                                    //取消下载异常，不做处理
                                                }
                                                httpListenerContext.Response.OutputStream.Close();
                                            }
                                            else
                                            {
                                                isBigFile = true;
                                            }
                                            #endregion
                                        }
                                        if (!isDonload || isBigFile)
                                        {
                                            //HTML结构
                                            result = "<HTML><HEAD><TITLE>HTTP下载 - v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version + "</TITLE></HEAD><BODY>";
                                            result += "当前【按" + (sortByName ? "名称" : "时间") + (isDesc ? "倒序" : "升序") + "】&nbsp;";
                                            //排序类型：按名称/按时间
                                            result += "<a href=\"http://" + request.UserHostName + (pram.Equals("/") ? "" : pram) + "?sorttype=" + (sortByName ? "time" : "name") + "&sort=" + (isDesc ? "desc" : "asc") + "\"><font color=\"Red\">" + (sortByName ? "按时间" : "按名称") + "</font></a>";
                                            //排序方式：升序/降序
                                            result += "&nbsp;&nbsp;<a href=\"http://" + request.UserHostName + (pram.Equals("/") ? "" : pram) + "?sorttype=" + (sortByName ? "name" : "time") + "&sort=" + (!isDesc ? "desc" : "asc") + "\"><font color=\"Red\">" + (isDesc ? "升序" : "降序") + "</font></a>";
                                            //首页
                                            result += (pram.Length > 1 ? ("&nbsp;&nbsp;<a href=\"http://" + request.UserHostName + "?sorttype=" + (sortByName ? "name" : "time") + "&sort=" + (isDesc ? "desc" : "asc") + "\">首页</a>") : "");
                                            if (!isBigFile)
                                            {
                                                if (isLink || Directory.Exists(dirFullPath))
                                                {
                                                    bool hasContent = false;
                                                    if (isLink)
                                                    {
                                                        #region 快捷方式
                                                        IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                                                        IWshRuntimeLibrary.IWshShortcut iws = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(dirFullPath + ".lnk");
                                                        //文件夹
                                                        dirFullPath = iws.TargetPath;
                                                        #endregion
                                                    }
                                                    #region 文件夹
                                                    //返回上级
                                                    result += (pram.Length > 1 && pram.LastIndexOf("/") != pram.IndexOf("/") ? ("&nbsp;&nbsp;<a href=\"http://" + request.UserHostName + pram.Remove(pram.LastIndexOf("/")) + "?sorttype=" + (sortByName ? "name" : "time") + "&sort=" + (isDesc ? "desc" : "asc") + "\">返回上级</a>") : "") + "<br /><br />"; ;
                                                    //文件夹列表
                                                    Dictionary<string, DateTime> itemList = new Dictionary<string, DateTime>();
                                                    foreach (DirectoryInfo item in new DirectoryInfo(dirFullPath).GetDirectories())
                                                    {
                                                        itemList.Add(item.Name, item.LastWriteTime);
                                                    }
                                                    //下级快捷方式
                                                    List<string> dicLink = new List<string>();
                                                    FileInfo[] filsLink = new DirectoryInfo(dirFullPath).GetFiles("*.lnk");
                                                    if (filsLink != null && filsLink.Length > 0)
                                                    {
                                                        foreach (FileInfo item in filsLink)
                                                        {
                                                            IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                                                            IWshRuntimeLibrary.IWshShortcut iws = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(item.FullName);
                                                            //文件夹
                                                            if (Directory.Exists(iws.TargetPath))
                                                            {
                                                                dicLink.Add(item.Name);
                                                                itemList.Add(item.Name.Remove(item.Name.LastIndexOf(".")), item.LastWriteTime);
                                                            }
                                                            else
                                                            {
                                                                //文件快捷方式不支持
                                                            }
                                                        }
                                                    }
                                                    //文件夹排序
                                                    Dictionary<string, DateTime> dis = null;
                                                    if (sortByName)
                                                    {
                                                        if (isDesc)
                                                        {
                                                            dis = itemList.OrderByDescending(p => p.Key).ToDictionary(p => p.Key, o => o.Value);
                                                        }
                                                        else
                                                        {
                                                            dis = itemList.OrderBy(p => p.Key).ToDictionary(p => p.Key, o => o.Value);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (isDesc)
                                                        {
                                                            dis = itemList.OrderByDescending(p => p.Value).ToDictionary(p => p.Key, o => o.Value);
                                                        }
                                                        else
                                                        {
                                                            dis = itemList.OrderBy(p => p.Value).ToDictionary(p => p.Key, o => o.Value);
                                                        }
                                                    }
                                                    //生成界面代码
                                                    if (dis != null && dis.Count > 0)
                                                    {
                                                        hasContent = true;
                                                        result += "<span style=\"font-size:25px\">文件夹</span><br />";
                                                        foreach (string item in dis.Keys)
                                                        {
                                                            result += "<a href=\"http://" + request.UserHostName + (pram.Equals("/") ? "" : pram) + "/" + item + "?sorttype=" + (sortByName ? "name" : "time") + "&sort=" + (isDesc ? "desc" : "asc") + "\">" + item + "</a><br />";
                                                        }
                                                        result += "<br />";
                                                    }
                                                    #endregion
                                                    #region 文件
                                                    itemList.Clear();
                                                    //文件列表
                                                    Dictionary<string, string> fileDate = new Dictionary<string, string>();
                                                    decimal fileSize = 0;
                                                    string unit = "Byte";
                                                    foreach (FileInfo item in new DirectoryInfo(dirFullPath).GetFiles())
                                                    {
                                                        //排出本程序自生相关文件
                                                        if (!item.Name.Equals(new FileInfo(Application.ExecutablePath).Name)
                                                        && !item.Name.Equals("Config.ini") && !item.Name.Equals("favicon.ico") && !dicLink.Contains(item.Name))
                                                        {
                                                            //文件大小、单位转换
                                                            fileSize = 0;
                                                            unit = "Byte";
                                                            fileSize = (decimal)item.Length;
                                                            if (fileSize > 1024)
                                                            {
                                                                fileSize = decimal.Round(fileSize / 1024, 2);
                                                                unit = "KB";
                                                                if (fileSize > 1024)
                                                                {
                                                                    fileSize = decimal.Round(fileSize / 1024, 2);
                                                                    unit = "MB";
                                                                    if (fileSize > 1024)
                                                                    {
                                                                        fileSize = decimal.Round(fileSize / 1024, 2);
                                                                        unit = "GB";
                                                                    }
                                                                }
                                                            }
                                                            //记录文件名用于排序
                                                            itemList.Add(item.Name, item.LastWriteTime);
                                                            //记录其他信息：时间、大小
                                                            fileDate.Add(item.Name, "<font color=\"#CC6633\">" + item.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") + "</font>&nbsp;&nbsp;" + fileSize + "&nbsp;" + unit);
                                                        }
                                                    }
                                                    //文件排序
                                                    //文件夹排序
                                                    Dictionary<string, DateTime> fis = null;
                                                    if (sortByName)
                                                    {
                                                        if (isDesc)
                                                        {
                                                            fis = itemList.OrderByDescending(p => p.Key).ToDictionary(p => p.Key, o => o.Value);
                                                        }
                                                        else
                                                        {
                                                            fis = itemList.OrderBy(p => p.Key).ToDictionary(p => p.Key, o => o.Value);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (isDesc)
                                                        {
                                                            fis = itemList.OrderByDescending(p => p.Value).ToDictionary(p => p.Key, o => o.Value);
                                                        }
                                                        else
                                                        {
                                                            fis = itemList.OrderBy(p => p.Value).ToDictionary(p => p.Key, o => o.Value);
                                                        }
                                                    }
                                                    //生成界面代码
                                                    if (fis != null && fis.Count > 0)
                                                    {
                                                        hasContent = true;
                                                        result += "<span style=\"font-size:25px\">文件</span><br />";
                                                        foreach (string item in fis.Keys)
                                                        {
                                                            //文件和网页的快捷方式暂不支持下载
                                                            if (item.ToLower().EndsWith(".lnk") || item.ToLower().EndsWith(".url"))
                                                            {
                                                                result += item + "&nbsp;&nbsp;" + fileDate[item] + "<br />";
                                                            }
                                                            else
                                                            {
                                                                //文件路径连接-点击下载
                                                                result += "<a href=\"http://" + request.UserHostName + (pram.Equals("/") ? "" : pram) + "/" + item + "\">" + item + "</a>" + "&nbsp;&nbsp;" + fileDate[item] + "<br />";
                                                            }
                                                        }
                                                    }
                                                    #endregion
                                                    if (!hasContent)
                                                    {
                                                        result += "<H1>空文件夹</H1>";
                                                    }
                                                }
                                                else
                                                {
                                                    result += "<H1>404 此目录或文件不存在</H1>";
                                                }
                                            }
                                            else
                                            {
                                                result += "<H1>文件太大，不支持下载</H1>";
                                            }
                                            //HTML结尾
                                            result += "</BODY></HTML>";
                                            //输出类型
                                            httpListenerContext.Response.ContentType = "text/html; charset=UTF-8";
                                            //返回状态
                                            httpListenerContext.Response.StatusCode = 200;
                                            using (StreamWriter writer = new StreamWriter(httpListenerContext.Response.OutputStream))
                                            {
                                                try
                                                {
                                                    //输出界面内容
                                                    writer.Write(result);
                                                }
                                                catch
                                                {
                                                    //刷新太快异常，不做处理
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //本程序支持 GET
                                        result = "不支持的请求类型";
                                        httpListenerContext.Response.StatusCode = 200;
                                        using (StreamWriter writer = new StreamWriter(httpListenerContext.Response.OutputStream))
                                        {
                                            writer.Write(result);
                                        }
                                    }
                                })).Start(httpListenerContext);
                        }
                        catch
                        { }
                    }
                })).Start();
                ConfigManagement.IniWriteValue(this.GetType().FullName, "Port", textBox1.Text);
                ConfigManagement.IniWriteValue(this.GetType().FullName, "RealPort", port.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "异常", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                button1.Enabled = true;
                textBox1.ReadOnly = false;
                linkLabel1.Text = "";
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(linkLabel1.Text);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!button1.Enabled)
            {
                this.Hide();
                e.Cancel = true;
            }
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                string path = Application.ExecutablePath;
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey rk2 = rk.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                string type = "";
                if (linkLabel2.Text.Equals("开机启动"))
                {
                    rk2.SetValue("HTTP Download", path + " -a");
                    type = "设置";
                }
                else
                {
                    rk2.DeleteValue("HTTP Download", false);
                    type = "取消设置";
                }
                rk2.Close();
                rk.Close();
                checkReg();
                MessageBox.Show(type + "成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "设置失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void checkReg()
        {
            RegistryKey rk = Registry.LocalMachine;
            RegistryKey rk2 = rk.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            if (rk2 != null)
            {
                object obj = rk2.GetValue("HTTP Download");
                if (obj != null)
                {
                    linkLabel2.Text = "取消开机启动";
                }
                else
                {
                    linkLabel2.Text = "开机启动";
                }
            }
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
            catch
            { }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
        }
    }
}
