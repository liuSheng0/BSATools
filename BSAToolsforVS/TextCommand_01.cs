using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Collections;
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
             * 特征提取
            */
            DTE dte = ServiceProvider.GetServiceAsync(typeof(DTE)).Result as DTE;
            langKeywords lang = new langKeywords(dte.ActiveDocument.Name);
            string savePath = "..//data//inf_" + dte.ActiveDocument.Name + ".txt";
            StreamWriter sw = new StreamWriter(savePath);
            if (dte.ActiveDocument != null && dte.ActiveDocument.Type == "Text")
            {
                string activeDocumentPath = dte.ActiveDocument.FullName;
                List<string> classList = new List<string>();//列表用于存储全部类
                List<string> methodList = new List<string>();//列表用于存储全部方法
                Dictionary<string, List<string>> classMethods = new Dictionary<string, List<string>>();//字典用于存储类的全部方法
                Dictionary<string, string> propertyClass = new Dictionary<string, string>();//字典用于存储在类内方法外创建的类的实例
                methodTagNowClass[] methodTagNowClasses = new methodTagNowClass[100];//用于存储方法的目标类和自身类
                Dictionary<string, List<string>> methodUsese = new Dictionary<string, List<string>>();//字典用于存储使用方法的全部实例
                string myprint = "";
                using (StreamReader sr = new StreamReader(activeDocumentPath))
                {
                    Stack<string> braceStack = new Stack<string>();//栈，用于判断当前类或方法
                    string nowClassOrMethod = "*";//当前类或方法的迭代，用于判断目前为类还是方法
                    string[] nowClassMethod = new String[] { "*", "*" };//判断当前类和方法内的迭代，0为类，1为方法，只在flag的值位置有效
                    int flagClassOrMethod = 0;//判断目前为类内还是方法内，0为类，1为方法
                    braceStack.Push(nowClassOrMethod);
                    int methodCount = 0;
                    string line;//每行代码
                    while ((line = sr.ReadLine()) != null)
                    {
                        //第一遍循环，读取文档类名信息，类的方法信息，实例信息
                        line = new Regex("//.*").Replace(line, "");//去除注释
                        //读取代码中的类名生成classList和classMethods-key
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
                        else
                        {
                            string[] method = lang.judgeMethod(line);
                            //读取代码中的方法名生成methodTagNowClasses-key和classMethods-value
                            if (method[0] != null)
                            {
                                string nowClass = nowClassMethod[0];
                                string methodName = method[0];
                                string methodParameter = method[1];
                                methodList.Add(methodName);
                                methodTagNowClasses[methodCount] = new methodTagNowClass(method[0], methodParameter, nowClass, new List<string>());
                                methodTagNowClasses[methodCount].disTagClass.Add(nowClass);
                                foreach (string item in method[2].Split(' '))
                                {
                                    if (classList.Contains(item))
                                    {
                                        methodTagNowClasses[methodCount].disTagClass.Add(item);
                                    }
                                }
                                if (classList.Contains(nowClass) && !classMethods[nowClass].Contains(methodName))
                                {
                                    classMethods[nowClass].Add(methodName);
                                }
                                methodCount++;
                                flagClassOrMethod = 1;
                                nowClassOrMethod = method[0];
                                nowClassMethod[flagClassOrMethod] = method[0];
                                methodName = ConvertTF(methodName);
                                myprint = myprint + '\n' + AdjustStr(methodName) + " " + methodParameter + " " + AdjustStr(ConvertTF(nowClass));
                            }
                            //获取类内方法外声明的类的实例，生成propertyClass
                            else if (flagClassOrMethod == 0)
                            {
                                string[] property = lang.judgeProperty(line);
                                if (property[0] != null)
                                {
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
                        //用栈的方式匹配当前类或者方法
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
                }

                using (StreamReader sr = new StreamReader(activeDocumentPath))
                {
                    Stack<string> braceStack = new Stack<string>();//栈，用于判断当前类或方法
                    string nowClassOrMethod = "*";//当前类或方法的迭代，用于判断目前为类还是方法
                    string[] nowClassMethod = new String[] { "*", "*" };//判断当前类和方法内的迭代，0为类，1为方法，只在flag的值位置有效
                    int flagClassOrMethod = 0;//判断目前为类内还是方法内，0为类，1为方法
                    braceStack.Push(nowClassOrMethod);
                    int methodCount = 0;
                    string line;//每行代码
                    while ((line = sr.ReadLine()) != null)
                    {
                        //第二遍读取文档，生成methodTagNowClasses的tagClass
                        line = new Regex("//.*").Replace(line, "");//去除注释
                        //标记为类内
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
                            //标记为方法内
                            if (method[0] != null)
                            {
                                methodCount++;
                                flagClassOrMethod = 1;
                                nowClassOrMethod = method[0];
                                nowClassMethod[flagClassOrMethod] = method[0];
                            }
                        }
                        if (flagClassOrMethod == 1)//当前方法内
                        {
                            line = new Regex("\".*?\"").Replace(line, " ");
                            string[] splitLine = line.Split(new char[] { '.', ' ', '(', ')' ,';' });
                            foreach (string words in splitLine)
                            {
                                //判断是否声明了其他类，若声明，则将此类加入distagclass
                                if (classList.Contains(words))
                                {
                                    methodTagNowClasses[methodCount - 1].disTagClass.Add(words);
                                }
                                //判断是否使用了自身类声明的其他类的实例，若使用，则将此实例对应的类加入distagClass
                                else if (propertyClass.ContainsKey(words))
                                {
                                    methodTagNowClasses[methodCount - 1].disTagClass.Add(propertyClass[words]);
                                }
                                //判断方法是否使用了其他实例，生成methodUsese
                                else if (methodList.Contains(words))
                                {
                                    if (!methodUsese.ContainsKey(methodTagNowClasses[methodCount - 1].methodName))
                                    {
                                        methodUsese.Add(methodTagNowClasses[methodCount - 1].methodName, new List<string>());
                                    }
                                    if (!methodUsese[methodTagNowClasses[methodCount - 1].methodName].Contains(words))
                                    {
                                        methodUsese[methodTagNowClasses[methodCount - 1].methodName].Add(words);
                                    }
                                }
                            }
                        }
                        //用栈的方式匹配当前类或者方法
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
                }

                //开始生成数据集
                StreamWriter sw_test_class_name = new StreamWriter("..//data//class_name.txt");
                StreamWriter sw_test_method_name = new StreamWriter("..//data//method_name.txt");
                StreamWriter sw_test_dis = new StreamWriter("..//data//dis.txt");
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
                            
                            double TCTagCount = calculateTC(methodUsese[method.methodName], classMethods[tagClass]);
                            classMethods[method.nowClass].Remove(method.methodName);
                            double TCNowCount = calculateTC(methodUsese[method.methodName], classMethods[method.nowClass]);
                            classMethods[method.nowClass].Add(method.methodName);
                            string writeline_dis = doubleToStandardString(TCTagCount) + " " + doubleToStandardString(TCNowCount);
                            string writeline_class_name = (AdjustStr(ConvertTF(tagClass)) + " " + AdjustStr(ConvertTF(method.nowClass)) + " " + tagClassMethodStr).Trim();
                            string writeline_method_name = (AdjustStr(ConvertTF(method.methodName)) + " " + method.methodParameters).Trim();
                            sw_test_class_name.WriteLine(writeline_class_name);
                            sw_test_method_name.WriteLine(writeline_method_name);
                            sw_test_dis.WriteLine(writeline_dis);
                        }
                    }
                }
                sw.WriteLine(myprint.Trim());
                sw.Close();
                sw_test_class_name.Close();
                sw_test_method_name.Close();
                sw_test_dis.Close();
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

        static public double calculateTC(List<string> smlist, List<string> sclist)
        {
            //计算T-C距离
            double res = 1.0;
            string[] sm = smlist.ToArray();
            string[] sc = sclist.ToArray();
            var smjsc = sm.Intersect(sc).ToList().ToArray();
            var smbsc = sm.Union(sc).ToList().ToArray();
            res = 1.0 - (double)smjsc.Length / (double)smbsc.Length;
            return res;
        }

        static public string doubleToStandardString(double d)
        {
            string res = d.ToString();
            if(!res.Contains("."))
            {
                res += ".";
            }
            if(res.Length < 22)
            {
                for(int i = res.Length; i < 22; i++)
                {
                    res += "0";
                }
            }
            return res;
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
            //用于存储方法的方法名，方法的当前类，方法的非目标类，方法的参数
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
