const vscode = require('vscode');

/**
 * @param {vscode.ExtensionContext} context
 */
function activate(context) {

	console.log('恭喜，您的扩展“BASToolsforVSCode”已被激活！');
	console.log(vscode);
    require('./feature')(context); // helloworld
}

function deactivate() {
	console.log('您的扩展“BASToolsforVSCode”已被释放！')
}

module.exports = {
	activate,
	deactivate
}

