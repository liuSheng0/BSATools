const vscode = require('vscode');
const fs = require('fs');
const child_process = require('child_process');
const path = require('path');

const DIRNAME =  __dirname + '/../../';

const method_info_path = DIRNAME + 'data/methodinfo.txt';
const class_info_path = DIRNAME + 'data/classinfo.txt';
const dis_path = DIRNAME + 'data/dis.txt';
const out_path = DIRNAME + 'res/predictresult.txt';
const model_path = DIRNAME + 'pymodel/model.pb'
const info_path = DIRNAME + 'data/info.txt';

module.exports = function(context) {
    context.subscriptions.push(vscode.commands.registerCommand('extension.test', run));
};

function run() {
    let argvstr = method_info_path+' '+class_info_path+' '+dis_path+' '+out_path+' '+model_path+' '+info_path;
    var workerProcess = child_process.exec('python '+ DIRNAME +'pymodel/predict.py '+argvstr, function (error, stdout, stderr) {
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
            vscode.window.showErrorMessage('特征依恋预测失败');
        }
    });
}