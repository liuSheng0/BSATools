//自定包：用于判断类，方法等
const judgeJava = {
    //判断此行是否为类，如果是，返回类名；如果不是，返回null
    judgeClass: function(str) {
        var className = null;
        var rgclass = new RegExp(/(([^/]*\s)|^)(class|interface)\s+/);
        var rgMid = new RegExp(/\<[^\<\>]*?\>/);

        while(rgMid.test(str)){
            str = str.replace(rgMid, "");
        }
        if(rgclass.test(str)) {
            className = str.replace(rgclass, "");
            className = className.replace(/\s*extends.*/, "");
            className = className.replace(/\s*implements.*/, "");
            className = className.replace(/\(.*?\)/);
            className = className.replace(/<.*>/);
            className = className.replace(/[^0-9a-zA-Z_]/g, "");
        }
        return className;
    },

    //判断此行是否为方法，如果是，返回method；如果不是，返回method[0] = null
    judgeMethod: function(str) {
        const modifierwords = ["private", "public", "protected", "static", "final", "abstract", "native", "strictfp", "synchronized", "volatile", "transient"];
        const keywords = ["if", "else", "while", "for", "foreach", "try", "catch"];
        var method = [null, "", ""];//返回值method[0]为方法名，method[1]为方法参数名, method[2]为参数类名，以空格隔开
        var rg = new RegExp(/\(.*?\)/);
        var rgMid = new RegExp(/\<[^\<\>]*?\>/);

        while(rgMid.test(str)){
            str = str.replace(rgMid, "");
        }
        var strName = str.replace(rg, " ");
        var strsParameter = rg.exec(str);
        var strParameter = "";
        if (strsParameter != null) {
            strParameter = strsParameter[0];
        }
        var strSplit = strName.trim().replace(/\,\s/, "").split(' ');
        let i = 0;
        while (i < strSplit.length && modifierwords.indexOf(strSplit[i]) != -1) {
            i++;
        }
        i--;
        if (i+2 < strSplit.length && strSplit[i+1] != "class" && rg.test(str) && str.indexOf(';') == -1 && keywords.indexOf(strSplit[i+2]) == -1) {
            if(strSplit[i+2] != '{')
                method[0] = strSplit[i+2];
            else
                method[0] = strSplit[i+1];
            strParameter = strParameter.replace(/\(|\)/g, "");
            var splitParameter = strParameter.split(',');
            for (let i in splitParameter) {
                let sp = splitParameter[i];
                let splitSp = sp.trim().split(' ');
                if (splitSp.length > 1) {
                    method[1] += splitSp[splitSp.length - 1] + " ";
                    method[2] += splitSp[splitSp.length - 2] + " ";
                }
            }
        }
        method[1] = method[1].trim();
        method[2] = method[2].trim();
        return method;
    },

    judgeProperty: function(str) {
        const modifierwords = ["private", "public", "protected", "static", "final", "abstract", "native", "strictfp", "synchronized", "volatile", "transient"];
        var property = [ null, "" ];//property[0]为属性名，property[1]为属性类
        var strSplit = str.trim().split(' ');
        let i = 0;
        while (i < strSplit.length && modifierwords.indexOf(strSplit[i]) != -1) {
            i++;
        }
        i--;
        if (i+2 < strSplit.length && strSplit[i+1] != "class" && strSplit[i+1] != "package" && strSplit[i+1] != "import" && str.indexOf(';') != -1) {
            property[0] = strSplit[i + 2].replace(";","");
            property[1] = strSplit[i + 1];
        }
        return property;
    },

    //判断此行是否为包，如果是，返回包名；如果不是，返回null
    judgePackage: function(str) {
        var packageName = null;
        var rgpackage = new RegExp(/(^|\s+)package\s*/);
        if(rgpackage.test(str)) {
            packageName = str.replace(rgpackage, "");
            packageName = packageName.replace(";","");
        }
        return packageName;
    },

    //根据包名判断此行是否为引用，如果是，返回引用名；如果不是，返回null
    judgeImport: function(str, packageName) {
        var importName = null;
        var rgimport = new RegExp(/(^|\s+)(\s+static\s+|\s*)import\s*/);
        var rgpackage = new RegExp(packageName+'.');
        if(rgimport.test(str)) {
            importName = str.replace(rgimport, "").replace(";", "");
        }
        return importName;
    },
};

module.exports = judgeJava;