@echo off
setlocal EnableDelayedExpansion

:: 设置脚本工作目录
set "WORK_DIR=%~dp0"
set "VENV_DIR=%WORK_DIR%python_env\venv"
set "TSINGHUA_MIRROR=https://pypi.tuna.tsinghua.edu.cn/simple"
set "PADDLE_MIRROR=https://www.paddlepaddle.org.cn/packages/stable/cu118/"

:: 清理屏幕
cls
echo ==================================================
echo      一键安装 Python 包（清华镜像 + Paddle 官方源）
echo ==================================================
echo.

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

:: 显示当前 Python 版本
echo.
echo 当前环境 Python 版本：
python --version
echo.

:: 提示 GPU 环境要求
echo [注意] 安装 paddlepaddle-gpu 需要：
echo - NVIDIA GPU
echo - CUDA 11.8 和 cuDNN 已正确配置
echo 如果未满足要求，可能导致安装或运行失败。
echo.

:: 升级 pip（使用清华源）
echo 正在升级 pip ...
pip install --upgrade pip -i %TSINGHUA_MIRROR%
if %ERRORLEVEL% neq 0 (
    echo [警告] 升级 pip 失败，但继续尝试安装包...
)

:: 安装 paddlepaddle-gpu
echo 正在安装 paddlepaddle-gpu==3.0.0 ...
pip install paddlepaddle-gpu==3.0.0 -i %PADDLE_MIRROR%
if %ERRORLEVEL% neq 0 (
    echo [错误] 安装 paddlepaddle-gpu 失败！请检查 GPU 环境和网络。
    set "INSTALL_FAILED=1"
) else (
    echo [成功] paddlepaddle-gpu 安装完成。
)

:: 安装 flask
echo 正在安装 flask ...
pip install flask -i %TSINGHUA_MIRROR%
if %ERRORLEVEL% neq 0 (
    echo [错误] 安装 flask 失败！
    set "INSTALL_FAILED=1"
) else (
    echo [成功] flask 安装完成。
)

:: 安装 opencv-python
echo 正在安装 opencv-python ...
pip install opencv-python -i %TSINGHUA_MIRROR%
if %ERRORLEVEL% neq 0 (
    echo [错误] 安装 opencv-python 失败！
    set "INSTALL_FAILED=1"
) else (
    echo [成功] opencv-python 安装完成。
)

:: 安装 paddleocr
echo 正在安装 paddleocr ...
pip install paddleocr -i %TSINGHUA_MIRROR%
if %ERRORLEVEL% neq 0 (
    echo [错误] 安装 paddleocr 失败！
    set "INSTALL_FAILED=1"
) else (
    echo [成功] paddleocr 安装完成。
)

:: 检查安装结果
echo.
echo ==================================================
if defined INSTALL_FAILED (
    echo      安装过程中出现错误！
    echo 请检查网络、GPU 环境或镜像源是否可用。
) else (
    echo      所有包安装成功！
)
echo ==================================================
echo.

:: 显示已安装包
echo 当前环境已安装的包：
pip list
echo.

:: 提示使用方法
echo 使用方法：
echo 1. 双击 "%WORK_DIR%python_env\activate_env.bat" 激活环境。
echo 2. 在命令行中即可使用 Python 和已安装的包（如 flask、opencv-python、paddleocr）。
echo 3. 输入 "exit" 退出环境。
echo.
echo 注意：运行 paddlepaddle-gpu 和 paddleocr 需要兼容的 GPU 环境！
echo.
echo 按任意键退出...
pause >nul

:: 退出虚拟环境
deactivate
exit /b 0