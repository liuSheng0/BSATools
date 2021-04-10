const vscode = require('vscode');
const path = require('path');
const webfunc = require('./dependences/webviewFunc');
const fs = require('fs');
const util = require('./dependences/util');
const jj = require('./dependences/judgeJava');

const DIRNAME =  __dirname + '/../';
const out_path = DIRNAME + 'res/predictresult.txt';
const html_path = 'res/predictresult.html'

const options = {
	// 是否预览，默认true，预览的意思是下次再打开文件是否会替换当前文件
	preview: false,
	// 显示在第二个编辑器
	viewColumn: vscode.ViewColumn.Active
};

module.exports = function(context) {
    context.subscriptions.push(vscode.commands.registerCommand('extension.showres', function () {
        const panel = vscode.window.createWebviewPanel(
            'testWebview', // viewType
            "代码坏味检测结果", // 视图标题
            vscode.ViewColumn.Active, // 显示在编辑器的哪个部位
            {
                enableScripts: true, // 启用JS，默认禁用
                retainContextWhenHidden: true, // webview被隐藏时保持状态，避免被重置
            }
        );
        const projectPath = vscode.Uri.file;
        console.log(projectPath);
        let html = getWebViewContent(html_path);
        panel.webview.html = html;
        panel.webview.onDidReceiveMessage(message => {
            switch (message.command) {
                case 'openFileInVscode' :
                    vscode.window.showTextDocument(vscode.Uri.file(message.text), options).then(editor => {
                        let decorationType = vscode.window.createTextEditorDecorationType({
                            backgroundColor: "#FF000055"
                        })
                        let pos = getPosition(editor, message.words);
                        editor.setDecorations(decorationType, pos);
                    });
					break;
                default:

            }
        }, undefined, context.subscriptions);
    }));
};

/**
 * 从某个HTML文件读取能被Webview加载的HTML内容
 * @param {*} templatePath 相对于插件根目录的html文件相对路径
 */
function getWebViewContent(templatePath) {
    const resourcePath = __dirname + '/../' + templatePath;
    const dirPath = path.dirname(resourcePath);
    let html = fs.readFileSync(resourcePath, 'utf-8');
    // vscode不支持直接加载本地资源，需要替换成其专有路径格式，这里只是简单的将样式和JS的路径替换
    html = html.replace(/(<link.+?href="|<script.+?src="|<img.+?src=")(.+?)"/g, (m, $1, $2) => {
        return $1 + vscode.Uri.file(path.resolve(dirPath, $2)).with({ scheme: 'vscode-resource' }).toString() + '"';
    });
    html = html.replace('%3A',':');
    return html;
}


//获取高亮位置
function getPosition(editor, word) {
    if(!editor) {
        return;
    }
    let range = [];
    const text = editor.document.getText();
    const lines = text.split(/\r?\n/);
    let lineflag = 0; 
    lines.forEach(line => {
        let index = line.indexOf(word);
        if(index != -1 && jj.judgeClass(line)) {
            range.push(new vscode.Range(lineflag, 0, lineflag, line.length));
        } else if(index != -1 && jj.judgeMethod(line)[0]) {
            range.push(new vscode.Range(lineflag, index, lineflag, index + line.length));
        }
        lineflag += 1;
    });
    return range;
}