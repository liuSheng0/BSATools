//自定包：用于判断类，方法等
const stringRule = {
    //驼峰命名或帕斯卡命名转下划线命名法
    ConvertTF : function(str) {
        var res = ""
        for (var i = 0; i < str.length; i++) {
            var temp = str.charAt(i);
            if (temp >= 'A' && temp <= 'Z' && i != 0) {
                res += "_";
            }
            res += temp;
        }
        res = res.toLowerCase();
        return res.trim();
    },

    //将类名转化为五词一组的分词形式
    AdjustStr : function(str) {
        str = stringRule.ConvertTF(str);
        var res = "";
        var str_arr = str.split('_');
        for(var i = 0; i < 5 - str_arr.length; i++){
            res += "* ";
        }
        str_arr.forEach(temp => {
            res = res + temp + " ";
        });
        return res.trim();
    },

    calculateTC: function(arr1, arr2){
        //计算T-C距离
        var res = 1.0;
        // 交集
        let intersection = arr1.filter(function (val) { return arr2.indexOf(val) > -1 });
        // 并集
        let union = arr1.concat(arr2.filter(function (val) { return !(arr1.indexOf(val) > -1) }));
        res = 1.0 - intersection.length / union.length;
        return res;
    },
};

module.exports = stringRule;