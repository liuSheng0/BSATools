using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace BSAToolsforVS
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TextCommand_01
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 256;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("f7cbf1ae-9889-403f-99c9-10327a1de290");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextCommand_01"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private TextCommand_01(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TextCommand_01 Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in TextCommand_01's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new TextCommand_01(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            /*
             * 自定义功能
             * 逐行读取当前代码，提取代码中的类名，并以标准格式输出文件
            */
            DTE dte = ServiceProvider.GetServiceAsync(typeof(DTE)).Result as DTE;
            langKeywords lang = new langKeywords(dte.ActiveDocument.Name);
            string savePath = "..//data//copy_" + dte.ActiveDocument.Name + ".txt";
            StreamWriter sw = new StreamWriter(savePath);
            List<string> classList = new List<string>(); 
            string myprint = "";
            if (dte.ActiveDocument != null && dte.ActiveDocument.Type == "Text")
            {
                string activeDocumentPath = dte.ActiveDocument.FullName;
                using (StreamReader sr = new StreamReader(activeDocumentPath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        //逐行读取文档
                        line = new Regex("//.*").Replace(line, "");//去除注释
                        string className = lang.judgeClass(line);
                        if(className != null)
                        {
                            classList.Add(className);
                            className = ConvertTF(className);
                            myprint += SplitStr(className);
                        }
                    }
                    sw.WriteLine(myprint.Trim());
                    sw.Close();
                }
                ShowMsgBox("写入成功！");
            }
            else
            {
                ShowMsgBox("ERROR！");
            }
        }

        public void ShowMsgBox(string msg)
        {
            //弹出窗口
            VsShellUtilities.ShowMessageBox(this.package, msg, "", OLEMSGICON.OLEMSGICON_NOICON, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        static public string ConvertTF(string str)
        {
            //驼峰命名法或帕斯卡命名法转下划线命名法
            string res = "";
            for (int i = 0; i < str.Length; i++)
            {
                string temp = str[i].ToString();
                if (Regex.IsMatch(temp, "[A-Z]") && i != 0)
                {
                    temp = "_" + temp.ToLower();
                }
                res += temp;
            }
            res = res.ToLower();
            return res;
        }

        static public string SplitStr(string str)
        {
            //将下划线命名格式转化为五词一组的分词形式
            string res = "";
            string[] wline_arr = str.Split('_');
            for (int i = 0; i < 5 - wline_arr.Length; i++)
            {
                res += "* ";
            }
            foreach (string temp in wline_arr)
            {
                res = res + temp + ' ';
            }
            return res;
        }

        class langKeywords
        {
            //用于使用不同代码类型保留字
            public string type;
            public string split;
            public string classwords;
            public string[] keywords;

            public langKeywords(string filename)
            {
                //判断文件类型
                if(filename.Contains(".cs"))
                {
                    type = "cs";
                    split = ":";
                    classwords = "class";
                    keywords = new string[] { "void", "int", "string" };
                }
                else if (filename.Contains(".java") || filename.Contains(".class"))
                {
                    type = "java";
                    split = "extend";
                    classwords = "class";
                    keywords = new string[] { "void", "int", "string" };
                }
                else
                {
                    type = "null";
                    split = null;
                    classwords = null;
                    keywords = null;
                }
            }

            public string judgeClass(string str)
            {
                //判断此行是否为类，如果是，返回类名；如果不是，返回null
                string className = null;
                Regex ifclass = new Regex("((^)|(^[^/]*\\s))" + classwords + "\\s*");
                if (ifclass.IsMatch(str))
                {
                    className = ifclass.Replace(str, "");
                    if (new Regex(split).IsMatch(className))
                    {
                        className = new Regex("\\s*" + split + ".*$").Replace(className, "");
                    }
                    className = new Regex("[^0-9a-zA-Z_]*").Replace(className, "");
                }
                return className;
            }
        }
    }
}
