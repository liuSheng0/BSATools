const vscode = require('vscode');
const fs = require('fs');
const child_process = require('child_process');
const path = require('path');

module.exports = function(context) {
    context.subscriptions.push(vscode.commands.registerCommand('extension.test', (uri) => {
        vscode.window.showInformationMessage('开始进行代码特征依恋预测');
        let method_info_path = __dirname + '/../data/methodinfo.txt';
        let class_info_path = __dirname + '/../data/classinfo.txt';
        let dis_path = __dirname + '/../data/dis.txt';
        let out_path = __dirname + '/../pymodel/res/predictres.txt';
        let model_path = __dirname + '/../pymodel/model.pb'
        let argvstr = method_info_path+' '+class_info_path+' '+dis_path+' '+out_path+' '+model_path;
        var workerProcess = child_process.exec('python '+ __dirname +'/../pymodel/predict.py '+argvstr, function (error, stdout, stderr) {
            if (error) {
                console.log(error.stack);
                console.log('Error code: '+error.code);
                console.log('Signal received: '+error.signal);
            }
            console.log('stdout: ' + stdout);
            console.log('stderr: ' + stderr);
        });
        
        workerProcess.on('exit', function (code) {
            console.log('子进程已退出，退出码 '+code);
            if(code==0) {
                vscode.window.showInformationMessage('特征依恋预测成功');
            }
            else {
                vscode.window.showInformationMessage('特征依恋预测失败');
            }
        });
    }));
};