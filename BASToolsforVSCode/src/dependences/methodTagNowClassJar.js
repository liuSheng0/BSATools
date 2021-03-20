//自定类
const methodTagNowClassJar = {
    methodTagNowClass: function(mName, mParameters, nClass, tClass) {
        this.methodName = mName;
        this.methodParameters = mParameters;
        this.disTagClass = tClass;
        this.nowClass = nClass;
        this.getMethodName = function() {
            return this.methodName;
        }
        this.getMethodParameters = function() {
            return this.methodParameters;
        }
        this.getDisTagClass = function() {
            return this.disTagClass;
        }
        this.getNowClass = function() {
            return this.nowClass;
        }
    }

};

module.exports = methodTagNowClassJar;