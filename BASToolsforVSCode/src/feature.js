const vscode = require('vscode');
const jj = require('./dependences/judgeJava');
const sr = require('./dependences/stringRule');
const mc = require('./dependences/methodTagNowClassJar');
const fs = require('fs');
const path = require('path');
const { methodTagNowClass } = require('./dependences/methodTagNowClassJar');

const methodinfoPath = __dirname + '/../data/methodinfo.txt';
const classinfoPath = __dirname + '/../data/classinfo.txt';
const disPath = __dirname + '/../data/dis.txt';
const resPath = __dirname + '/../data/info.txt';
const dirPath = __dirname + '/../data/fileinfo.txt';

module.exports = function(context) {
    context.subscriptions.push(vscode.commands.registerCommand('extension.getFeature', run));
};

function run() {
    let classList = [];//列表用于存储全部类
    let methodList = [];//列表用于存储全部方法
    let classMethods = new Array();//字典用于存储类的全部方法
    let propertyClass = new Array();//字典用于存储在类内方法外创建的类的实例
    let methodUsese = new Array();//字典用于存储使用方法的全部实例
    let methodTagNowClasses = [];//用于存储方法的目标类和自身类

    try { fs.unlinkSync(methodinfoPath) } catch {;}
    try { fs.unlinkSync(classinfoPath) } catch {;}
    try { fs.unlinkSync(disPath) } catch {;}
    try { fs.unlinkSync(resPath) } catch {;}



    let braceStack = [];//括号匹配栈
    let nowClassOrMethod = "*";//当前类或者方法
    let nowClassMethod = ["*", "*"];//当前类，方法
    let flagClassMethod = 0;//当前状态，0为类，1为方法
    let methodCount = 0;
    braceStack.push(nowClassOrMethod);
    if(fs.existsSync(dirPath)){
        let filedirs = readfile(dirPath).toString().split(/\r?\n/);
        filedirs.forEach(filedir => {
            console.log(filedir);
            if(fs.existsSync(filedir)){
                let everfilelines = readfile(filedir).toString().split(/\r?\n/);
                braceStack = [];//括号匹配栈
                nowClassOrMethod = "*";//当前类或者方法
                nowClassMethod = ["*", "*"];//当前类，方法
                flagClassMethod = 0;//当前状态，0为类，1为方法
                methodCount = 0;
                everfilelines.forEach((line) => {
                    line = line.replace(/\/\/.*/g, "");//去除注释
                    let className = jj.judgeClass(line);
                    if(className != null) {
                        classList.push(className);
                        if(!classMethods.hasOwnProperty(className)) {
                            classMethods[className] = [];
                        }
                        flagClassMethod = 0;
                        nowClassOrMethod = className;
                        nowClassMethod[flagClassMethod] = className;
                        console.log(sr.ConvertTF(className));
                    }
                    else {
                        let method = jj.judgeMethod(line);
                        if (method[0] != null) {
                            let nowClass = nowClassMethod[0];
                            let methodName = method[0];
                            let methodParameter = method[1];
                            methodList.push(methodName);
                            if (classList.indexOf(nowClass) != -1 && classMethods[nowClass].indexOf(methodName) == -1) {
                                classMethods[nowClass].push(methodName);
                            }
                            methodCount++;
                            flagClassMethod = 1;
                            nowClassOrMethod = method[0];
                            nowClassMethod[flagClassMethod] = method[0];
                            console.log(sr.AdjustStr(methodName) + " " + methodParameter + " " + sr.AdjustStr(nowClass));
                        }
                    }

                    //括号匹配
                    for(let i = 0; i < line.length; i++) {
                        let c = line.charAt(i);
                        if(c == '{') {
                            braceStack.push(nowClassOrMethod);
                        }
                        else if(c == '}') {
                            if (braceStack[braceStack.length - 1] == nowClassOrMethod) {
                                braceStack.pop();
                                nowClassOrMethod = braceStack[braceStack.length - 1];
                                if (classList.indexOf(nowClassOrMethod) != -1) {
                                    flagClassMethod = 0;
                                }
                                else {
                                    flagClassMethod = 1;
                                }
                                nowClassMethod[flagClassMethod] = nowClassOrMethod;
                            }
                        }
                    }
                });
            }
        });
    }




    console.log("----------------------");
    let editor = vscode.window.activeTextEditor;
    if(!editor) {
        return;
    }
    const text = editor.document.getText();
    const lines = text.split(/\r?\n/);
    braceStack = [];//括号匹配栈
    nowClassOrMethod = "*";//当前类或者方法
    nowClassMethod = ["*", "*"];//当前类，方法
    flagClassMethod = 0;//当前状态，0为类，1为方法
    methodCount = 0;
    //第一遍循环，读取文档类名信息，类的方法信息，实例信息
    lines.forEach((line) => {
        line = line.replace(/\/\/.*/g, "");//去除注释
        let className = jj.judgeClass(line);
        if(className != null) {
            classList.push(className);
            if(!classMethods.hasOwnProperty(className)) {
                classMethods[className] = [];
            }
            flagClassMethod = 0;
            nowClassOrMethod = className;
            nowClassMethod[flagClassMethod] = className;
            console.log(sr.ConvertTF(className));
        }
        else {
            let method = jj.judgeMethod(line);
            if (method[0] != null) {
                let nowClass = nowClassMethod[0];
                let methodName = method[0];
                let methodParameter = method[1];
                methodList.push(methodName);
                methodTagNowClasses[methodCount] = new methodTagNowClass(method[0], methodParameter, nowClass, []);
                methodTagNowClasses[methodCount].disTagClass.push(nowClass);
                let m2spl = method[2].split(' ');
                for (let i in m2spl) {
                    let item = m2spl[i];
                    if (classList.indexOf(item) != -1) {
                        methodTagNowClasses[methodCount].disTagClass.push(item);
                    }
                }
                if (classList.indexOf(nowClass) != -1 && classMethods[nowClass].indexOf(methodName) == -1) {
                    classMethods[nowClass].push(methodName);
                }
                methodCount++;
                flagClassMethod = 1;
                nowClassOrMethod = method[0];
                nowClassMethod[flagClassMethod] = method[0];
                console.log(sr.AdjustStr(methodName) + " " + methodParameter + " " + sr.AdjustStr(nowClass));
            }
            //获取类内方法外声明的类的实例，生成propertyClass
            else if (flagClassMethod == 0) {
                let property = jj.judgeProperty(line);
                if (property[0] != null) {
                    try
                    {
                        propertyClass[property[0]] = property[1];
                    }
                    catch (ArgumentException)
                    {
                        console.log("Property \"" + property[0] + "\" already exists.");
                    }
                    console.log(property[0] + " " + property[1]);
                }
            }
        }

        //括号匹配
        for(let i = 0; i < line.length; i++) {
            let c = line.charAt(i);
            if(c == '{') {
                braceStack.push(nowClassOrMethod);
            }
            else if(c == '}') {
                if (braceStack[braceStack.length - 1] == nowClassOrMethod) {
                    braceStack.pop();
                    nowClassOrMethod = braceStack[braceStack.length - 1];
                    if (classList.indexOf(nowClassOrMethod) != -1) {
                        flagClassMethod = 0;
                    }
                    else {
                        flagClassMethod = 1;
                    }
                    nowClassMethod[flagClassMethod] = nowClassOrMethod;
                }
            }
        }
    });

    braceStack = [];//括号匹配栈
    nowClassOrMethod = "*";//当前类或者方法
    nowClassMethod = ["*", "*"];//当前类，方法
    flagClassMethod = 0;//当前状态，0为类，1为方法
    methodCount = 0;
    //第二遍读取文档，生成methodTagNowClasses的tagClass
    lines.forEach((line) => {
        line = line.replace(/\/\/.*/g, "");//去除注释
        let className = jj.judgeClass(line);
        //标记为类内
        if (className != null) {
            flagClassMethod = 0;
            nowClassOrMethod = className;
            nowClassMethod[flagClassMethod] = className;
        }
        else {
            let method = jj.judgeMethod(line);
            //标记为方法内
            if (method[0] != null) {
                methodCount++;
                flagClassMethod = 1;
                nowClassOrMethod = method[0];
                nowClassMethod[flagClassMethod] = method[0];
            }
        }
        if(flagClassMethod == 1) {
            line = line.replace(/\".*?\"/, " ");
            let splitLine = line.split(/\.|\s|\(|\)|;|{|}|\[|\]/g);
            splitLine.forEach(words => {
                if (classList.indexOf(words) != -1) {//判断是否声明了其他类，若声明，则将此类加入distagclass
                    methodTagNowClasses[methodCount - 1].disTagClass.push(words);
                }
                else if (propertyClass.hasOwnProperty(words)) {//判断是否使用了自身类声明的其他类的实例，若使用，则将此实例对应的类加入distagClass
                    methodTagNowClasses[methodCount - 1].disTagClass.push(propertyClass[words]);
                }
                else if (methodList.indexOf(words) != -1) {//判断方法是否使用了其他实例，生成methodUsese
                    if (!methodUsese.hasOwnProperty(methodTagNowClasses[methodCount - 1].methodName)) {
                        methodUsese[methodTagNowClasses[methodCount - 1].methodName] = [];
                    }
                    if (methodUsese[methodTagNowClasses[methodCount - 1].methodName].indexOf(words) == -1) {
                        methodUsese[methodTagNowClasses[methodCount - 1].methodName].push(words);
                    }
                }
            });

            //括号匹配
            for(let i = 0; i < line.length; i++) {
                let c = line.charAt(i);
                if(c == '{') {
                    braceStack.push(nowClassOrMethod);
                }
                else if(c == '}') {
                    if (braceStack[braceStack.length - 1] == nowClassOrMethod) {
                        braceStack.pop();
                        nowClassOrMethod = braceStack[braceStack.length - 1];
                        if (classList.indexOf(nowClassOrMethod) != -1) {
                            flagClassMethod = 0;
                        }
                        else {
                            flagClassMethod = 1;
                        }
                        nowClassMethod[flagClassMethod] = nowClassOrMethod;
                    }
                }
            }
        }
    });
    console.log("----------------------");

    //开始生成数据集
    methodTagNowClasses.forEach(method => {
        let disTagClass = unique(method.disTagClass);
        let tagClasses = classList.filter(function (val) { return !(disTagClass.indexOf(val) > -1) });
        tagClasses.forEach(tagClass => {
            let tagClassMethodStr = "";
            if (classMethods.hasOwnProperty(tagClass)) {
                classMethods[tagClass].forEach(item => {
                    tagClassMethodStr += sr.AdjustStr(item) + " ";
                });
            }
            let TCTagCount = sr.calculateTC(methodUsese[method.methodName], classMethods[tagClass]);
            classMethods[method.nowClass] = removeByValue(classMethods[method.nowClass], method.methodName);
            let TCNowCount = sr.calculateTC(methodUsese[method.methodName], classMethods[method.nowClass]);
            classMethods[method.nowClass].push(method.methodName);
            let writeline_dis = TCTagCount.toFixed(20) + " " + TCNowCount.toFixed(20) + '\n';
            let writeline_class_name = (sr.AdjustStr(tagClass) + " " + sr.AdjustStr(method.nowClass) + " " + tagClassMethodStr).trim() + '\n';
            let writeline_method_name = (sr.AdjustStr(method.methodName) + " " + method.methodParameters).trim() + '\n';
            let writeline_res = method.methodName + " " + tagClass + '\n';
            writefile(methodinfoPath, writeline_method_name);
            writefile(classinfoPath, writeline_class_name);
            writefile(disPath, writeline_dis);
            writefile(resPath, writeline_res);
        });
    });
    showInfMessage(`特征提取成功`);
}

function writefile(path, msg) {
    fs.writeFile(path, msg,{'flag':'a'}, function (error) {
        if (error) {
          console.log('写入'+path+'失败')
          return false
        } else {
          console.log('写入'+path+'成功')
          return true
        }
    });
}

function showInfMessage(msg) {
	vscode.window.showInformationMessage(msg);
}

function unique(arr) {
    return Array.from(new Set(arr));
}

function removeByValue(arr, value) { 
    var index = arr.indexOf(value); 
    if (index > -1) { 
        arr.splice(index, 1); 
    }
    return arr;
}

function readfile(path) {
    var data = fs.readFileSync(path);
    return data.toString();
}