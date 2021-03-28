const vscode = require('vscode');

/**
 * @param {vscode.ExtensionContext} context
 */
function activate(context) {

	console.log('恭喜，您的扩展“BASToolsforVSCode”已被激活！');
	console.log(vscode);
    require('./feature')(context); // 特征提取，将三类特征存入data文件夹中
	require('./test')(context); // 预测，调用pymodel中的predict子程序
	require('./findfile')(context);
}

function deactivate() {
	console.log('您的扩展“BASToolsforVSCode”已被释放！')
}

module.exports = {
	activate,
	deactivate
}

