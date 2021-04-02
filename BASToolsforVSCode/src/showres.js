const vscode = require('vscode');

module.exports = function(context) {
    context.subscriptions.push(vscode.commands.registerCommand('extension.showres', run));
};

const DIRNAME =  __dirname + '/../';
const out_path = DIRNAME + 'res/predictresult.txt';

const options = {
	// 是否预览，默认true，预览的意思是下次再打开文件是否会替换当前文件
	preview: false,
	// 显示在第二个编辑器
	viewColumn: vscode.ViewColumn.Active
};

const panel = vscode.window.createWebviewPanel(
    'testWebview', // viewType
    "WebView演示", // 视图标题
    vscode.ViewColumn.One, // 显示在编辑器的哪个部位
    {
        enableScripts: true, // 启用JS，默认禁用
        retainContextWhenHidden: true, // webview被隐藏时保持状态，避免被重置
    }
);

function run() {
    vscode.window.showTextDocument(vscode.Uri.file(out_path), options);
    panel.webview.html = `<html><body>你好，我是Webview</body></html>`
}