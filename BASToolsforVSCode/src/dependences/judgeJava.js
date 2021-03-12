//自定包：用于判断类，方法等
const judgeJava = {
    //判断此行是否为类，如果是，返回类名；如果不是，返回null
    judgeClass: function(str) {
        var className = null;
        var rgclass = new RegExp(/((^)|(^[^/]*\s))class\s*/);
        if(rgclass.test(str)) {
            className = str.replace(rgclass, "");
            className = className.replace(/\s*extends.*/, "");
            className = className.replace(/\s*implements.*/, "");
            className = className.replace(/[^0-9a-zA-Z_]/g, "");
        }
        return className;
    },

    //判断此行是否为方法，如果是，返回method；如果不是，返回method[0] = null
    judgeMethod: function(str) {
        const modifierwords = ["private", "public", "protected", "static", "final"];
        var method = [null, "", ""];//返回值method[0]为方法名，method[1]为方法参数名, method[2]为参数类名，以空格隔开
        var rg = new RegExp(/\(.*?\)/);
        var strName = str.replace(rg, "");
        var strsParameter = rg.exec(str);
        var strParameter = "";
        if (strsParameter != null) {
            strParameter = strsParameter[0];
        }
        var strSplit = strName.trim().split(' ');
        let i = 0;
        while (i < strSplit.length && modifierwords.indexOf(strSplit[i]) == -1) {
            i++;
        }
        while (i < strSplit.length && modifierwords.indexOf(strSplit[i]) != -1) {
            i++;
        }
        i--;
        if (i+2 < strSplit.length && strSplit[i+1] != "class" && rg.test(str) && str.indexOf(';') == -1) {
            method[0] = strSplit[i+2];
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
        const modifierwords = ["private", "public", "protected", "static", "final"];
        var property = [ null, "" ];//property[0]为属性名，property[1]为属性类
        var strSplit = str.trim().split(' ');
        let i = 0;
        while (i < strSplit.length && modifierwords.indexOf(strSplit[i]) == -1) {
            i++;
        }
        while (i < strSplit.length && modifierwords.indexOf(strSplit[i]) != -1) {
            i++;
        }
        i--;
        if (i+2 < strSplit.length && strSplit[i+1] != "class" && str.indexOf(';') != -1) {
            property[0] = strSplit[i + 2].replace(";","");
            property[1] = strSplit[i + 1];
        }
        return property;
    },
};

module.exports = judgeJava;