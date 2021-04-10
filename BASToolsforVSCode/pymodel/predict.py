from __future__ import print_function

import os
import sys

from keras.preprocessing.text import Tokenizer
from keras.preprocessing.sequence import pad_sequences
import tensorflow as tf
#from kegra.utils import *
import numpy as np

HEAD = '<head><style>table tr#b{ background:#333} table tr#w{ background:#555} table td{ text-align: center; padding-top: 5px; padding-bottom: 5px;}</style></head>'
SCRIPT = "<script>const testMode = false;const vscode = testMode ? {} : acquireVsCodeApi();const callbacks = {};function openFileInVscode(path,words) {vscode.postMessage({command: 'openFileInVscode',text: path,words: words})}</script>"
JUMP = 'href="可信度：{}" onclick="openFileInVscode(\'{}\',\'{}\')"'

def putTxtInfo(test_y_out, out_path, info_path) :
    # test_y_out为数组（为0 的概率，为1的概率）
    TARGETPATH = out_path

    if (os.path.exists(TARGETPATH)):
        os.remove(TARGETPATH)
    resultWriter = open(TARGETPATH, 'w', encoding = "utf-8")

    resultWriter.write('<html>{}<body style="width:100%"><table width=100%><tr id="b"><th>方法名</th><th>自身类名</th><th>检测结果</th><th>重构推荐</th></tr>'.format(HEAD))

    f = open(info_path, 'r')

    lines = f.readlines()
    linenum = 0

    tagClassesRes = {"tagClasses" : [], "predictRes" : [], "res" : 1 , "nowMethod": ""}
    aflag = False
    lowRes = []
    modifyClasses = []
    tagClassPredictRes = {}
    methodNowClass = {}
    classPathInfo = {}
    nowMethod = ""
    nowNowClass = ""
    nowMethodRes = 1
    idStr = 'id="b"'
    idStrW = 'id="w"'


    for result in test_y_out:
        line = lines[linenum].replace('\n','')
        re = 1
        if(result[1] > 0.5):
            re = 0
        else:
            re = 1
        lineName = line.split(" ")
        methodName = lineName[0]
        tagClassName = lineName[1]
        nowClassName = lineName[2]
        tagClassPath = lineName[3]
        nowClassPath = lineName[4]

        classPathInfo[nowClassName] = nowClassPath
        classPathInfo[tagClassName] = tagClassPath
        tagClassPredictRes[tagClassName] = result[1]
        if(nowClassPath[0] == '/'):
            classPathInfo[nowClassName] = nowClassPath[1:]
        if(tagClassName[0] == '/'):
            classPathInfo[tagClassName] = tagClassPath[1:]
        

        if (methodName == nowMethod and nowClassName == nowNowClass) :
            nowMethodRes *= re
            if(re == 0):
                modifyClasses.append(tagClassName)
        else:
            if aflag and nowMethod != "":
                if nowMethodRes == 0 :
                    restr = "Featury Envy"
                else :
                    restr = "Featury Envy"

                if(nowMethodRes == 0) :
                    resultWriter.write("<tr {}><td {}>{}</td><td><a {}>{}</a></td><td>{}</td>".format(idStr ,JUMP.format("", classPathInfo[nowNowClass], nowMethod), nowMethod, JUMP.format("", classPathInfo[nowNowClass], nowNowClass), nowNowClass, restr))
                    resultWriter.write("<td>Advise Move To:<br>")
                    for modifyClass in modifyClasses:
                        resultWriter.write("<a {}>{}<br></a>".format(JUMP.format(str(tagClassPredictRes[modifyClass]), classPathInfo[modifyClass], modifyClass), modifyClass))
                    resultWriter.write("</td></tr>")
                else:
                    lowRes.append("<tr {}><td {}>{}</td><td><a {}>{}</a></td><td>{}</td><td></td></tr>".format(idStrW ,JUMP.format("", classPathInfo[nowNowClass], nowMethod), nowMethod, JUMP.format("", classPathInfo[nowNowClass], nowNowClass), nowNowClass, restr))

            nowMethod = methodName
            nowNowClass = nowClassName
            nowMethodRes = re
            methodNowClass[methodName] = nowClassName
            
            modifyClasses = []
            if(re == 0):
                modifyClasses.append(tagClassName)
            aflag = True
        linenum+=1

    if nowMethod != "" and not aflag:
        if nowMethodRes == 0 :
            restr = "可能存在特征依恋！"
        else :
            restr = "不存在特征依恋"
        
        if(nowMethodRes == 0) :
            resultWriter.write("<tr {}><td {}>{}</td><td><a {}>{}</a></td><td>{}</td>".format(idStr ,JUMP.format("", classPathInfo[nowNowClass], nowMethod), nowMethod, JUMP.format("", classPathInfo[nowNowClass], nowNowClass), nowNowClass, restr))
            resultWriter.write("<td>Advise Move To:<br>")
            for modifyClass in modifyClasses:
                resultWriter.write("<a {}>{}<br></a>".format(JUMP.format(str(tagClassPredictRes[modifyClass]), classPathInfo[modifyClass], modifyClass), modifyClass))
            resultWriter.write("</td></tr>")
        else:
            lowRes.append("<tr {}><td {}>{}</td><td><a {}>{}</a></td><td>{}</td><td></td></tr>".format(idStrW ,JUMP.format("", classPathInfo[nowNowClass], nowMethod), nowMethod, JUMP.format("", classPathInfo[nowNowClass], nowNowClass), nowNowClass, restr))
    
    for alowRes in lowRes:
        resultWriter.write(alowRes)
    resultWriter.write("</table>{}</body></html>".format(SCRIPT))
    resultWriter.close()

if __name__ == '__main__':

    # 利用测试集测试模型效果
    TP = 0
    FN = 0
    FP = 0
    TN = 0
    NUM_CORRECT = 0
    TOTAL = 0
    MAX_SEQUENCE_LENGTH = 55

    #读取路径，[1]为method，[2]为class，[3]为dis，[4]为输出，[5]为模型位置
    msg = sys.argv
    try:
        method_info_path = msg[1]
        class_info_path = msg[2]
        dis_path = msg[3]
        out_path = msg[4]
        model_path = msg[5]
        info_path = msg[6]
    except:
        print("参数不足，应为method_path class_path dis_path out_path")
        exit(1)

    # model = model_from_json(open('model.json').read())
    # model.load_weights('model_weights.h5')
    # 这里是将模型读取过来了

    test_distances = []
    test_labels = []
    test_class_info = []
    test_method_info = []

    try:
        with open(method_info_path, 'r') as file_to_read:
            for line in file_to_read.readlines():
                test_method_info.append(line)

        with open(class_info_path, 'r') as file_to_read:
            for line in file_to_read.readlines():
                test_class_info.append(line)

        with open(dis_path, 'r') as file_to_read:
            for line in file_to_read.readlines():  # 读取一行数据
                values = line.split()  # 将这一行数据按照空格分割开
                distance = values[:2]  # 取values前2个数据
                test_distances.append(distance)
                label = values[2:]  # 取values最后一个数据
                test_labels.append(label)
    except:
        exit(2)

    test_tokenizer_class = Tokenizer(num_words=None)
    test_tokenizer_method = Tokenizer(num_words=None)

    test_tokenizer_class.fit_on_texts(test_class_info)
    test_class_sequences = test_tokenizer_class.texts_to_sequences(test_class_info)
    test_word_index_class = test_tokenizer_class.word_index
    test_class_data = pad_sequences(test_class_sequences, maxlen=MAX_SEQUENCE_LENGTH)

    test_tokenizer_method.fit_on_texts(test_method_info)
    test_method_sequences = test_tokenizer_method.texts_to_sequences(test_method_info)
    test_word_index_method = test_tokenizer_method.word_index
    test_method_data = pad_sequences(test_method_sequences, maxlen=MAX_SEQUENCE_LENGTH)

    test_distances = np.asarray(test_distances)

    test_labels = np.asarray(test_labels)

    x_val = []

    x_val_dis = np.expand_dims(test_distances, axis=2)
    x_val.append(test_class_data)
    x_val.append(test_method_data)
    x_val.append(np.array(x_val_dis))
    y_val = np.array(test_labels)

    # 测试tensorflow模型

    with tf.Graph().as_default():
        output_graph_def = tf.compat.v1.GraphDef()

        with open(model_path, "rb") as f:#主要步骤即为以下标出的几步,1、2步即为读取图
            output_graph_def.ParseFromString(f.read())# 1.将模型文件解析为二进制放进graph_def对象
            _ = tf.import_graph_def(output_graph_def, name="")# 2.import到当前图

        with tf.compat.v1.Session() as sess:
            init = tf.compat.v1.global_variables_initializer()
            sess.run(init)

            graph = tf.compat.v1.get_default_graph()# 3.获得当前图

            # 4.get_tensor_by_name获取需要的节点
            class_info_input = sess.graph.get_tensor_by_name("input_1_1:0")
            method_name_input = sess.graph.get_tensor_by_name("input_2_1:0")
            Dis_in = sess.graph.get_tensor_by_name("input_3:0")
            Y = graph.get_tensor_by_name("dense_3/Sigmoid:0")

            #执行
            test_y_out = sess.run(Y, feed_dict={class_info_input:test_class_data,method_name_input:test_method_data,Dis_in:np.array(x_val_dis)})
            # print("test_y_out:{}".format(test_y_out)
            print(test_y_out)
            
            putTxtInfo(test_y_out, out_path, info_path)

    exit(0)