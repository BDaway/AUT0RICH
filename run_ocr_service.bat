@echo off
setlocal EnableDelayedExpansion

:: 设置脚本工作目录
set "WORK_DIR=%~dp0"
set "VENV_DIR=%WORK_DIR%python_env\venv"
set "PY_FILE=%WORK_DIR%paddleocr_server.py"

:: 清理屏幕
cls
echo ==================================================
echo      一键运行 OCR 服务
echo ==================================================
echo.

:: 检查 Python 文件是否存在
if not exist "%PY_FILE%" (
    echo [错误] 未找到 Python 文件：%PY_FILE%
    echo 请确保 ocr_service.py 存在于脚本目录！
    pause
    exit /b 1
)

:: 检查虚拟环境是否存在
if not exist "%VENV_DIR%\Scripts\activate.bat" (
    echo [错误] 未找到虚拟环境：%VENV_DIR%
    echo 请先运行 setup_python_env.bat 创建环境！
    pause
    exit /b 1
)

:: 激活虚拟环境
echo 正在激活虚拟环境 ...
call "%VENV_DIR%\Scripts\activate.bat"
if %ERRORLEVEL% neq 0 (
    echo [错误] 激活虚拟环境失败！
    pause
    exit /b 1
)

:: 显示 Python 版本
echo.
echo 当前环境 Python 版本：
python --version
echo.

:: 检查必要包是否安装
echo 正在检查必要包 ...
set "MISSING_PACKAGES="
for %%p in (flask opencv-python paddlepaddle-gpu paddleocr) do (
    pip show %%p >nul 2>&1
    if !ERRORLEVEL! neq 0 (
        set "MISSING_PACKAGES=!MISSING_PACKAGES! %%p"
    )
)
if defined MISSING_PACKAGES (
    echo [错误] 缺少以下包：%MISSING_PACKAGES%
    echo 请运行 install_packages.bat 安装必要包！
    pause
    deactivate
    exit /b 1
)

:: 检查 GPU 可用性（仅提示，非强制）
echo 检查 GPU 可用性（仅供参考）：
python -c "import paddle; print('Compiled with CUDA:', paddle.device.is_compiled_with_cuda()); print('Current Device:', paddle.device.get_device())" 2>nul
if %ERRORLEVEL% neq 0 (
    echo [警告] 无法检查 GPU 可用性，可能缺少 paddlepaddle-gpu 或配置问题。
    echo 服务可能仍可运行（使用 CPU 或 GPU）。
)
echo.

:: 运行 Python 程序
echo 正在启动 OCR 服务 ...
echo 服务将运行在 http://localhost:8000/recognize
echo.

:: 提示测试方法
echo 测试方法：
echo 1. 确保服务启动（看到 Flask 日志）。
echo 2. 使用 curl 或 Postman 发送 POST 请求：
echo    curl -F "image=@test.jpg" -F "numbersOnly=true" http://localhost:8000/recognize
echo 3. 按 Ctrl+C 停止服务。
echo.

:: 执行 Python 文件
python "%PY_FILE%"
if %ERRORLEVEL% neq 0 (
    echo [错误] 服务启动失败！
    echo - 如果提示端口占用（Address already in use），请检查 8000 端口。
    echo - 如果提示模块缺失，请运行 install_packages.bat。
    echo - 如果提示 GPU 错误，请确认 CUDA 11.8 和 cuDNN 配置。
    pause
)

:: 退出虚拟环境
deactivate
echo.
echo 服务已停止。
echo 按任意键退出...
pause >nul
exit /b 0