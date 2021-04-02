const vscode = require('vscode');

/**
 * @param {vscode.ExtensionContext} context
 */
function activate(context) {

	console.log('恭喜，您的扩展“BASToolsforVSCode”已被激活！');
	console.log(vscode);
	require('./findfile')(context);// 预处理，将java依赖包存入data文件夹中
    require('./feature')(context); // 特征提取，将三类特征存入data文件夹中
	require('./test')(context); // 预测，调用pymodel中的predict子程序
	require('./start')(context);// 主入口，运行其他三个文件
}

function deactivate() {
	console.log('您的扩展“BASToolsforVSCode”已被释放！')
}

module.exports = {
	activate,
	deactivate
}

