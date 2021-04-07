const vscode = require('vscode');
const path = require('path');
const fs = require('fs');
const webviewFunc = {
    /**
     * 获取某个扩展文件相对于webview需要的一种特殊路径格式
     * 形如：vscode-resource:/Users/toonces/projects/vscode-cat-coding/media/cat.gif
     * @param context 上下文
     * @param relativePath 扩展中某个文件相对于根目录的路径，如 images/test.jpg
     */
    getExtensionFileVscodeResource: function(context, relativePath) {
        const diskPath = vscode.Uri.file(path.join(context.extensionPath, relativePath));
        return diskPath.with({ scheme: 'vscode-resource' }).toString();
    }
};

module.exports = webviewFunc;