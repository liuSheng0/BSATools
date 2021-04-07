const vscode = require('vscode');
const path = require('path');

module.exports = function(context) {
    context.subscriptions.push(vscode.commands.registerCommand('extension.start', run));
};

function run(uri) {
    vscode.commands.executeCommand('extension.getFeature', uri).then(result => {
        console.log('命令结果', result);
    });
    runCommand('extension.test');
}

function runCommand(command) {
    vscode.commands.executeCommand(command).then(result => {
        console.log('命令结果', result);
    });
}