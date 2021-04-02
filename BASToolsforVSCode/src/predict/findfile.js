const vscode = require('vscode');
const fs = require('fs');
const path = require('path');
const jj = require('../dependences/judgeJava');

const DIRNAME =  __dirname + '/../../';
const fileinfoPath = DIRNAME + 'data/fileinfo.txt';

module.exports = function(context) {
    context.subscriptions.push(vscode.commands.registerCommand('extension.findfile', run));
};

function run(uri) {
    try { fs.unlinkSync(fileinfoPath) } catch {;}
    let rootPath = uri.path.replace(/java\/.*/, "");
    rootPath = rootPath.replace(/\/c:\/|\/C:\//,"C:/");
    console.log(rootPath);
    let editor = vscode.window.activeTextEditor;
    if(!editor) {
        return;
    }
    const text = editor.document.getText();
    const lines = text.split(/\r?\n/);
    let packageName = null;
    lines.forEach(line => {
        line = line.replace(/\/\/.*/g, "");//去除注释
        let packageNameJudge = jj.judgePackage(line);
        if(packageNameJudge) {
            packageName = packageNameJudge;
            console.log(packageName);
        }
        if(packageName) {
            let importName = jj.judgeImport(line, packageName);
            if(importName) {
                console.log(importName);
                fileDisplay(rootPath, packageName.replace(/\./g,"\\"), function(data) {
                    let importPath = data + '\\' + importName.replace(/\./g, "\\");
                    console.log(importPath);
                    writefile(fileinfoPath, (importPath+'.java\n'));
                });
            }
        }
    });
    showInfMessage("预处理成功");
}

function showInfMessage(msg) {
	vscode.window.showInformationMessage(msg);
}

var flag = true;
/**
 * 文件遍历方法
 * @param filePath 需要遍历的文件路径
 * @param findFileName 需要查找的文件名
 */
function fileDisplay(filePath, findFileName, callback) {
    //根据文件路径读取文件，返回文件列表
    fs.readdir(filePath,function(err,files) {
        if(err) {
            console.warn(err)
        } else {
        //遍历读取到的文件列表
            files.forEach(function(filename){
                //获取当前文件的绝对路径
                var filedir = path.join(filePath,filename);
                //根据文件路径获取文件信息，返回一个fs.Stats对象
                fs.stat(filedir,function(eror,stats) {
                    if(eror) {
                        console.warn('获取文件stats失败');
                    } else {
                        var isFile = stats.isFile();//是文件
                        var isDir = stats.isDirectory();//是文件夹
                        if(isFile) {
                            ;
                        }
                        if(isDir) {
                            if(filedir.indexOf(findFileName) != -1 && flag){
                                flag = false;
                                callback(filedir);
                                flag = true;
                                return;
                            }
                            return fileDisplay(filedir, findFileName, callback);//递归，如果是文件夹，就继续遍历该文件夹下面的文件
                        }
                    }
                })
            });
        }
    });
}

function writefile(path, msg) {
    fs.writeFile(path, msg, {'flag':'a'}, function (error) {
        if (error) {
          console.log('写入'+path+'失败')
          return false
        } else {
          console.log('写入'+path+'成功')
          return true
        }
    });
}

function readfile(path) {
    console.log(path);
    let data = fs.readFileSync(path);
    console.log(data);
    return data.toString();
}