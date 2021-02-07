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
using System.Linq;

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
            if (dte.ActiveDocument != null && dte.ActiveDocument.Type == "Text")
            {
                string activeDocumentPath = dte.ActiveDocument.FullName;
                Stack<string> braceStack = new Stack<string>();//栈，用于判断当前类或方法
                string nowClassOrMethod = "*";//当前类或方法的迭代，用于判断目前为类还是方法
                string[] nowClassMethod = new String[] { "*", "*" };//判断当前类和方法内的迭代，0为类，1为方法，只在flag的值位置有效
                int flagClassOrMethod = 0;//判断目前为类内还是方法内，0为类，1为方法
                braceStack.Push(nowClassOrMethod);
                List<string> classList = new List<string>();//列表用于存储全部类
                Dictionary<string, List<string>> classMethods = new Dictionary<string, List<string>>();//字典用于存储类的全部方法
                List<string> propertyList = new List<string>();//列表用于存储全部类内属性
                Dictionary<string, string> propertyClass = new Dictionary<string, string>();//字典用于存储类内属性的类
                methodTagNowClass[] methodTagNowClasses = new methodTagNowClass[100];
                int methodCount = 0;
                string myprint = "";
                using (StreamReader sr = new StreamReader(activeDocumentPath))
                {
                    string line;//每行代码
                    while ((line = sr.ReadLine()) != null)
                    {
                        //逐行读取文档
                        line = new Regex("//.*").Replace(line, "");//去除注释
                        //判断是否为类
                        string className = lang.judgeClass(line);
                        if (className != null)
                        {
                            classList.Add(className);
                            try
                            {
                                classMethods.Add(className, new List<string>());
                            }
                            catch (ArgumentException)
                            {
                                Console.WriteLine("Class \"" + className + "\" already exists.");
                            }
                            flagClassOrMethod = 0;
                            nowClassOrMethod = className;
                            nowClassMethod[flagClassOrMethod] = className;
                            className = ConvertTF(className);
                            myprint += '\n';
                            myprint += AdjustStr(className);
                        }
                    }
                }
                using (StreamReader sr = new StreamReader(activeDocumentPath))
                {
                    string line;//每行代码
                    while ((line = sr.ReadLine()) != null)
                    {
                        //逐行读取文档
                        line = new Regex("//.*").Replace(line, "");//去除注释
                        //判断是否为类
                        string className = lang.judgeClass(line);
                        if(className != null)
                        {
                            flagClassOrMethod = 0;
                            nowClassOrMethod = className;
                            nowClassMethod[flagClassOrMethod] = className;
                        }
                        else
                        {
                            string[] method = lang.judgeMethod(line);
                            //判断是否为方法
                            if (method[0] != null)
                            {
                                string nowClass = nowClassMethod[0];
                                string methodName = method[0];
                                string methodParameter = method[1];
                                methodTagNowClasses[methodCount++] = new methodTagNowClass(method[0], methodParameter, nowClass, new List<string>());
                                methodTagNowClasses[methodCount - 1].disTagClass.Add(nowClass);
                                foreach (string item in method[2].Split(' '))
                                {
                                    if (classList.Contains(item))
                                    {
                                        methodTagNowClasses[methodCount - 1].disTagClass.Add(item);
                                    }
                                }
                                if (classList.Contains(nowClass))
                                {
                                    classMethods[nowClass].Add(methodName);
                                }
                                flagClassOrMethod = 1;
                                nowClassOrMethod = method[0];
                                nowClassMethod[flagClassOrMethod] = method[0];
                                methodName = ConvertTF(methodName);
                                myprint = myprint + '\n' + AdjustStr(methodName) + " " + methodParameter + " " + AdjustStr(ConvertTF(nowClass));
                            }
                            //获取类内方法外声明的属性
                            else if (flagClassOrMethod == 0)
                            {
                                string[] property = lang.judgeProperty(line);
                                if(property[0] != null)
                                {
                                    propertyList.Add(property[0]);
                                    try
                                    {
                                        propertyClass.Add(property[0], property[1]);
                                    }
                                    catch (ArgumentException)
                                    {
                                        Console.WriteLine("Property \"" + property[0] + "\" already exists.");
                                    }
                                    myprint += '\n';
                                    myprint += property[0] + " " + property[1];
                                }
                            }
                        }
                        if (flagClassOrMethod == 1)
                        {
                            line = new Regex("\".*?\"").Replace(line, " ");
                            string[] splitLine = line.Split(new char[] { '.', ' ', '(', ')' ,';' });
                            foreach (string words in splitLine)
                            {
                                if (classList.Contains(words))
                                {
                                    methodTagNowClasses[methodCount - 1].disTagClass.Add(words);
                                }
                                else if (propertyClass.ContainsKey(words))
                                {
                                    methodTagNowClasses[methodCount - 1].disTagClass.Add(propertyClass[words]);
                                }
                            }
                        }
                        //判断类或方法情况
                        foreach (char c in line)
                        {
                            if (c == '{')
                            {
                                braceStack.Push(nowClassOrMethod);
                            }
                            else if (c == '}')
                            {
                                if (braceStack.Peek() == nowClassOrMethod)
                                {
                                    braceStack.Pop();
                                    nowClassOrMethod = braceStack.Peek();
                                    if (classList.Contains(nowClassOrMethod))
                                    {
                                        flagClassOrMethod = 0;
                                    }
                                    else
                                    {
                                        flagClassOrMethod = 1;
                                    }
                                    nowClassMethod[flagClassOrMethod] = nowClassOrMethod;
                                }
                            }
                        }
                    }
                    StreamWriter sw_test_class_name = new StreamWriter("..//data//test_class_name_" + dte.ActiveDocument.Name + ".txt");
                    StreamWriter sw_test_method_name = new StreamWriter("..//data//test_method_name_" + dte.ActiveDocument.Name + ".txt");
                    foreach (methodTagNowClass method in methodTagNowClasses)
                    {
                        if (method != null)
                        {
                            string[] disTagClassArr = method.disTagClass.ToArray();
                            string[] classArr = classList.ToArray();
                            string[] TagClasses = classArr.Where(c => !disTagClassArr.Contains(c)).ToArray();
                            foreach (string tagClass in TagClasses)
                            {
                                string tagClassMethodStr = "";
                                if (classMethods.ContainsKey(tagClass))
                                {
                                    foreach (string item in classMethods[tagClass])
                                    {
                                        tagClassMethodStr += AdjustStr(ConvertTF(item)) + ' ';
                                    }
                                }
                                string writeline_class_name = (AdjustStr(ConvertTF(tagClass)) + " " + AdjustStr(ConvertTF(method.nowClass)) + " " + tagClassMethodStr).Trim();
                                string writeline_method_name = (AdjustStr(ConvertTF(method.methodName)) + " " + method.methodParameters).Trim();
                                sw_test_class_name.WriteLine(writeline_class_name);
                                sw_test_method_name.WriteLine(writeline_method_name);
                            }
                        }
                    }
                    sw.WriteLine(myprint.Trim());
                    sw.Close();
                    sw_test_class_name.Close();
                    sw_test_method_name.Close();
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
            return res.Trim();
        }

        static public string AdjustStr(string str)
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
            return res.Trim();
        }

        class methodTagNowClass
        {
            public string methodName;
            public string nowClass;
            public List<string> disTagClass;
            public string methodParameters;

            public methodTagNowClass(string mName, string mParameters ,string nClass, List<string> tClass)
            {
                methodName = mName;
                nowClass = nClass;
                disTagClass = tClass;
                methodParameters = mParameters;
            }
        }

        class langKeywords
        {
            //用于使用不同代码类型保留字
            public string type;
            public string split;
            public string split01;
            public string classwords;
            public string[] typewords;
            public string[] modifierwords;

            public langKeywords(string filename)
            {
                //判断文件类型
                if(filename.Contains(".cs"))
                {
                    type = "cs";
                    split = ":";
                    split01 = ":";
                    classwords = "class";
                    typewords = new string[] { "void", "int", "string" };
                    modifierwords = new string[] { "private", "public", "protected", "static", "final" };
                }
                else if (filename.Contains(".java") || filename.Contains(".class"))
                {
                    type = "java";
                    split = "extends";
                    split01 = "implements";
                    classwords = "class";
                    typewords = new string[] { "void", "int", "string" };
                    modifierwords = new string[] { "private", "public", "protected", "static", "final" };
                }
                else
                {
                    type = "null";
                    split = "null";
                    split01 = "null";
                    classwords = "null";
                    typewords = null;
                    modifierwords = null;
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
                    if (new Regex(split01).IsMatch(className))
                    {
                        className = new Regex("\\s*" + split01 + ".*$").Replace(className, "");
                    }
                    className = new Regex("[^0-9a-zA-Z_]*").Replace(className, "");
                }
                return className;
            }

            public string[] judgeMethod(string str)
            {
                //返回值method[0]为方法名，method[1]为方法参数名, method[2]为参数类名，以空格隔开
                //判断此行是否为方法，如果是，返回method；如果不是，返回method[0] = null
                string[] method = new string[3] {null, "", ""};
                Regex rg = new Regex(@"\(.*?\)");
                string strName = rg.Replace(str, "");
                string strParameter = rg.Match(str).Value;
                string[] strSplit = strName.Trim().Split(' ');
                int i = 0;
                while (i < strSplit.Length && Array.IndexOf(modifierwords, strSplit[i]) == -1)
                {
                    i++;
                }
                while (i < strSplit.Length && Array.IndexOf(modifierwords, strSplit[i]) != -1)
                {
                    i++;
                }
                i--;
                if (i + 2 < strSplit.Length && strSplit[i + 1] != classwords && rg.IsMatch(str) && !str.Contains(";"))
                {
                    method[0] = strSplit[i + 2];
                    strParameter = strParameter.Replace("(", "");
                    strParameter = strParameter.Replace(")", "");
                    string[] splitParameter = strParameter.Split(',');
                    foreach (string sp in splitParameter)
                    {
                        string[] splitSp = sp.Trim().Split(' ');
                        if (splitSp.Length > 1)
                        {
                            method[1] += splitSp[splitSp.Length - 1] + " ";
                            method[2] += splitSp[splitSp.Length - 2] + " ";
                        }
                    }
                }
                method[1] = method[1].Trim();
                method[2] = method[2].Trim();
                return method;
            }

            public string[] judgeProperty(string str)
            {
                //property[0]为属性名，property[1]为属性类
                string[] property = new string[] { null, "" };
                string[] strSplit = str.Trim().Split(' ');
                int i = 0;
                while (i < strSplit.Length && Array.IndexOf(modifierwords, strSplit[i]) == -1)
                {
                    i++;
                }
                while (i < strSplit.Length && Array.IndexOf(modifierwords, strSplit[i]) != -1)
                {
                    i++;
                }
                i--;
                if (i + 2 < strSplit.Length && strSplit[i + 1] != classwords && str.Contains(";"))
                {
                    property[0] = strSplit[i + 2].Replace(";","");
                    property[1] = strSplit[i + 1];
                }
                return property;
            }
        }
    }
}
