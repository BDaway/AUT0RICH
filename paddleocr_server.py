from flask import Flask, request, jsonify
import cv2
import numpy as np
from paddleocr import PaddleOCR
import threading

app = Flask(__name__)

# 初始化 PaddleOCR，设置语言为中文（chi_sim），禁用日志输出
try:
    ocr = PaddleOCR(
        use_angle_cls=False,  # 禁用角度分类，加速推理
        lang="ch",
        show_log=False,
        use_gpu=True  # 如果有 GPU，可以设置为 True
    )
except Exception as e:
    print(f"Failed to initialize PaddleOCR: {str(e)}")
    raise

# 添加锁，确保 PaddleOCR 的推理过程是线程安全的
ocr_lock = threading.Lock()

@app.route('/recognize', methods=['POST'])
def recognize_text():
    try:
        # 从请求中获取图像文件
        if 'image' not in request.files:
            return jsonify({'error': 'No image provided'}), 400
        
        image_file = request.files['image']
        # 将图像文件转换为 numpy 数组
        image_buffer = image_file.read()
        print(f"Received image size: {len(image_buffer)} bytes")
        image_data = cv2.imdecode(np.frombuffer(image_buffer, np.uint8), cv2.IMREAD_COLOR)
        if image_data is None:
            return jsonify({'error': 'Failed to decode image'}), 400

        # 放大图像 4 倍
        scale_factor = 4.0
        resized_image = cv2.resize(image_data, None, fx=scale_factor, fy=scale_factor, interpolation=cv2.INTER_LINEAR)

        # 使用锁保护 PaddleOCR 推理过程
        with ocr_lock:
            # 调用 PaddleOCR 进行识别
            result = ocr.ocr(resized_image, cls=False)

        # 提取识别结果
        if result is None or len(result) == 0:
            return jsonify({'text': ''}), 200

        # 合并所有识别的文本
        recognized_text = ''
        for line in result:
            if line:  # 确保 line 不为空
                if isinstance(line, list) and len(line) > 0:
                    if isinstance(line[0], list):  # 处理嵌套列表 [[box, (text, conf)], ...]
                        for sub_line in line:
                            if sub_line and len(sub_line) > 1:
                                text = sub_line[1][0]  # 提取文本内容
                                if isinstance(text, str):
                                    recognized_text += text
                    else:  # 处理 [box, (text, conf)]
                        if len(line) > 1:
                            text = line[1][0]  # 提取文本内容
                            if isinstance(text, str):
                                recognized_text += text

        # 根据 numbersOnly 参数决定是否只提取数字
        numbers_only = request.form.get('numbersOnly', 'true').lower() == 'true'
        if numbers_only:
            import re
            recognized_text = ''.join(re.findall(r'\d+', recognized_text))

        return jsonify({'text': recognized_text}), 200

    except Exception as e:
        # 打印详细错误信息到控制台，便于调试
        print(f"Error in /recognize endpoint: {str(e)}")
        return jsonify({'error': f'Internal server error: {str(e)}'}), 500

if __name__ == '__main__':
    # 启用多线程支持
    app.run(host='0.0.0.0', port=8000, debug=False, threaded=True)